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
    # 부모를 사용자가 Active/Inactive로 재배치할 수 있으므로 특정 fileID에 고정하지 않습니다.
    for field in ('mergeItemActiveParent', 'mergeItemPoolParent'):
        parent_id = re.search(r'^  ' + field + r': \{fileID: (\d+)\}', pool, re.M)[1]
        assert parent_id != '0' and scene[parent_id][0] == '4', field
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
        if '  mergeMaterials: []\n' in body and '  mergeRecipe: []\n' in body:
            assert '  acceptKeyboardInput: 0\n' in body, 'unconfigured board must not receive drop input'
            continue
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
    for field in ('circleCollider', 'spriteRenderer', 'visualSize', 'body'):
        reference = re.search(r'^  ' + field + r': \{fileID: (\d+)\}', piece, re.M)[1]
        assert reference in prefab and reference != '0', field
    renderer = next(body for kind, body in prefab.values() if kind == '212')
    assert '  m_GameObject: {fileID: 910000600001}' in renderer, 'only Visual may be resized'
    assert any('  poolGroup: 3' in body for _, body in prefab.values())
    selector = next(body for _, body in scene.values() if guid(merge / 'MergeItemSelection.cs') in body)
    for field, kind in (('board', '114'), ('inputCamera', '20'), ('dragVisual', '212')):
        reference = re.search(r'^  ' + field + r': \{fileID: (\d+)\}', selector, re.M)[1]
        assert reference in scene and scene[reference][0] == kind, field
        if field == 'board': assert manager_guid in scene[reference][1]
        if field == 'dragVisual':
            preview = scene[reference][1]
            # 사용자가 Drag를 Box 프리팹으로 옮기면 씬에는 stripped 참조만 남습니다.
            # 해당 프리팹의 원본 Renderer를 따라가서 실제 시작 상태를 검사합니다.
            if '  m_Enabled:' not in preview:
                source = re.search(r'm_CorrespondingSourceObject: \{fileID: (\d+), guid: (\w+)', preview)
                assert source is not None, 'missing preview source'
                source_prefab = next(path for path in (assets / 'Prefab').rglob('*.prefab') if guid(path) == source[2])
                preview = blocks(source_prefab)[source[1]][1]
            assert '  m_Enabled: 0\n' in preview, 'preview starts hidden'
    selection_source = (merge / 'MergeItemSelection.cs').read_text()
    assert '"<Keyboard>/x"' in selection_source and 'cancelInput.performed += _ => CancelSelection()' in selection_source
    assert not re.search(r'Find\w*Object\w*\s*\(|new GameObject|AddComponent<', selection_source), 'selection uses explicit existing objects'
    assert 'pixelRect.Contains' in selection_source and 'IsOverUI()' in selection_source, 'restrict pointer to item viewport and respect UI'
    for name in ('MergegameManager.cs', 'MergeItemObject.cs'):
        source = (merge / name).read_text()
        assert not re.search(r'Camera|ScreenToWorld|ViewportToWorld|FindObjects', source), name
        assert not re.search(r'new GameObject|AddComponent<.*(?:Canvas|Text|Image|Button)', source), 'user creates UI'
    assert 'GetPhysicsScene2D' in (merge / 'MergegameManager.cs').read_text()
    print('PASS merge shell: authored scene/prefab references, local pool, recipe definitions, camera-independent spawning, drag/click camera + authored preview + X cancellation bindings, and no generated UI.')
