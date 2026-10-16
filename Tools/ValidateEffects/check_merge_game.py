"""Validate the user's GameItemScene shell and its explicit pooled merge connections."""
from pathlib import Path
import math
import re


def check_merge_game():
    root = Path(__file__).resolve().parents[2]
    assets = root / 'Assets'
    merge = assets / 'Script/Items/Merge'
    def guid(path):
        return re.search(r'^guid: (\w+)', Path(str(path) + '.meta').read_text(), re.M)[1]
    def blocks(path):
        text = path.read_text()
        entries = re.findall(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n([\s\S]*?)(?=^--- !u!|\Z)', text, re.M)
        result = {fid: (kind, body) for kind, fid, body in entries}
        assert len(entries) == len(result), (path, 'duplicate file IDs')
        for _, body in result.values():
            for line in body.splitlines():
                if 'guid:' in line: continue
                for ref in re.findall(r'\{fileID: (-?\d+)\}', line):
                    assert ref == '0' or ref in result, (path, 'missing reference', line)
        return result
    scene = blocks(assets / 'Scenes/GameItemScene.unity')
    prefab = blocks(merge / 'MergePrefab/MergeItemObject.prefab')
    pool_guid = guid(assets / 'Script/Entity/Pool/ObjectPoolManager.cs')
    manager_guid = guid(merge / 'MergegameManager.cs')
    pool_id, pool = next((fid, body) for fid, (_, body) in scene.items() if pool_guid in body)
    assert '  registerAsDefaultPool: 0\n' in pool, 'local pool must not replace battle effects pool'
    assert '  mergeItemActiveParent: {fileID: 98602615}' in pool
    definitions = {}
    for path in (merge / 'MergePrefab').glob('MergeItem *.asset'):
        text = path.read_text()
        item = re.search(r'^  item: \{fileID: 11400000, guid: (\w+)', text, re.M)[1]
        radius = float(re.search(r'^  radius: (.*)', text, re.M)[1])
        assert math.isfinite(radius) and radius > 0, path
        definitions[guid(path)] = item
    recipes = {guid(path): path.read_text() for path in (merge / 'MergePrefab').glob('Recipe*.asset')}
    configured = 0
    for _, body in scene.values():
        if manager_guid not in body: continue
        assert f'  objectPool: {{fileID: {pool_id}}}' in body
        assert '  spawnPoint: {fileID: 985560683}' in body
        catalog = body.split('  mergeItems:\n', 1)[1].split('  dropInterval:', 1)[0]
        catalog_guids = re.findall(r'guid: (\w+)', catalog)
        assert all(key in definitions for key in catalog_guids)
        items = {definitions[key] for key in catalog_guids}
        for key, recipe in recipes.items():
            if key not in body: continue
            configured += 1
            ingredient_guids = re.findall(r'itemData: \{fileID: 11400000, guid: (\w+)', recipe)
            result = re.search(r'resultItem: \{fileID: 11400000, guid: (\w+)', recipe)[1]
            assert result in items and all(item in items for item in ingredient_guids), 'missing merge result definition'
    assert configured > 0
    piece = next(body for _, body in prefab.values() if guid(merge / 'MergeItemObject.cs') in body)
    for field in ('circleCollider', 'spriteRenderer', 'body'):
        reference = re.search(r'^  ' + field + r': \{fileID: (\d+)\}', piece, re.M)[1]
        assert reference in prefab and reference != '0', field
    renderer = next(body for kind, body in prefab.values() if kind == '212')
    assert '  m_GameObject: {fileID: 910000600001}' in renderer, 'only Visual may be resized'
    assert any('  poolGroup: 3' in body for _, body in prefab.values())
    for name in ('MergegameManager.cs', 'MergeItemObject.cs'):
        source = (merge / name).read_text()
        assert not re.search(r'Camera|ScreenToWorld|ViewportToWorld|FindObjects', source), name
        assert not re.search(r'new GameObject|AddComponent<.*(?:Canvas|Text|Image|Button)', source), 'user creates UI'
    assert 'GetPhysicsScene2D' in (merge / 'MergegameManager.cs').read_text()
    print('PASS merge shell: authored scene/prefab references, shared local pool, registered result definitions, camera-independent spawning, scene physics/layer inheritance and no generated UI.')
