"""Validate the merge catalog and real scene references without importing Unity."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]


def check_merge_game():
    assets = ROOT / 'Assets'
    catalog_path = assets / 'Data/MergeGame/MergeGameCatalog.asset'
    if not catalog_path.exists():
        return
    paths = {}
    for meta in assets.rglob('*.meta'):
        match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(errors='replace'), re.M)
        if match:
            paths[match[1]] = Path(str(meta)[:-5])

    def guid(path):
        return re.search(r'^guid: (\w+)', Path(str(path) + '.meta').read_text(), re.M)[1]

    def refs(text):
        return re.findall(r'guid: ([a-f0-9]{32})', text)

    def section(text, key):
        match = re.search(r'^  ' + key + r':\n([\s\S]*?)(?=^  [A-Za-z_]|\Z)', text, re.M)
        return match[1] if match else ''

    catalog = catalog_path.read_text()
    containers = [paths[g] for g in refs(section(catalog, 'containers'))]
    skills = [paths[g] for g in refs(section(catalog, 'containerSkills'))]
    items = [paths[g] for g in refs(section(catalog, 'encyclopediaItems'))]
    enemies = [paths[g] for g in refs(section(catalog, 'encyclopediaEnemies'))]
    assert len(containers) == 8 and len(skills) == 96 and items and enemies
    series_seen, materials_all = set(), set()
    for container in containers:
        text = container.read_text()
        series = re.search(r'^  series: (\d+)', text, re.M)[1]
        assert series not in series_seen
        series_seen.add(series)
        materials = refs(section(text, 'injectionMaterials'))
        materials_all.update(materials)
        entries = re.findall(r'^  - item: .*guid: (\w+).*\n    mergeResult: (.*)\n    radius: ([\d.]+)', text, re.M)
        registered = {entry[0] for entry in entries}
        assert len(entries) == len(registered) and set(materials) <= registered
        graph = {}
        for item_guid, result, radius in entries:
            assert 0.08 <= float(radius) <= 1.2
            assert re.search(r'^  series: (\d+)', paths[item_guid].read_text(), re.M)[1] == series
            target = re.search(r'guid: (\w+)', result)
            if target:
                assert target[1] in registered and target[1] != item_guid
                graph[item_guid] = target[1]
        assert graph
        for item in graph:
            visited = set()
            while item in graph:
                assert item not in visited, (container, 'cyclic merge graph')
                visited.add(item)
                item = graph[item]
    reward_script = guid(assets / 'Script/MergeGame/MergeContainerUpgradeReward.cs')
    for node in skills:
        text = node.read_text()
        skill_id = re.search(r'^  dataId: (\w+)', text, re.M)[1]
        reward = paths[refs(section(text, 'rewards'))[0]].read_text()
        assert reward_script in reward and f'  skillId: {skill_id}\n' in reward
        definition = re.search(r'^  container: .*guid: (\w+)', reward, re.M)[1]
        assert paths[definition] in containers
        assert int(re.search(r'^  upgrade: (\d+)', reward, re.M)[1]) in range(6)
        prerequisites = refs(section(text, 'requiredSkills'))
        assert len(prerequisites) == (1 if skill_id.endswith('_2') else 0)
        if prerequisites:
            assert f'  dataId: {skill_id[:-1]}1\n' in paths[prerequisites[0]].read_text()
    for path in (assets / 'Data/MergeGame').rglob('*.asset'):
        for g in refs(path.read_text()):
            assert g in paths, (path, 'missing external asset', g)
    enemy_script = guid(assets / 'Script/Entity/Enemy/EnemyStatData.cs')
    assert all(enemy_script in path.read_text() for path in enemies)
    assert (assets / 'Data/MergeGame/Fonts/NotoSansKR-Regular.otf').read_bytes()[:4] == b'OTTO'
    assert 'SIL OPEN FONT LICENSE' in (assets / 'Data/MergeGame/Fonts/OFL.txt').read_text()

    def scene(name):
        text = (assets / 'Scenes' / f'{name}.unity').read_text()
        blocks = re.findall(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)', text, re.M)
        assert len(blocks) == len({i for _, i, _ in blocks}), ('duplicate file IDs', name)
        return {i: (t, body) for t, i, body in blocks}

    game = scene('GameScene')
    controller = game['910000000003'][1]
    for field, expected in [('combatCamera', '20'), ('combatHud', '223'), ('resultCanvas', '223')]:
        identifier = re.search(r'^  ' + field + r': \{fileID: (\d+)\}', controller, re.M)[1]
        assert game[identifier][0] == expected, (field, 'wrong component type')
    for field in ['player', 'positionProvider', 'throwExecutor']:
        identifier = re.search(r'^  ' + field + r': \{fileID: (\d+)\}', controller, re.M)[1]
        assert game[identifier][0] == '114'
    assert '  mergeGame: {fileID: 910000000003}' in game['1473317464'][1]
    assert '1389380996' in section(controller, 'legacyGameplayUI')
    for identifier in re.findall(r'\{fileID: (\d+)\}', section(controller, 'legacyInputComponents')):
        assert '  m_Enabled: 0\n' in game[identifier][1]
    upgrade = scene('UpgradeScene')
    controller = upgrade['910000000003'][1]
    assert '  mergeUpgrade: {fileID: 910000000003}' in upgrade['1644459205'][1]
    for identifier in re.findall(r'\{fileID: (\d+)\}', section(controller, 'removedPanels')):
        assert '  m_IsActive: 0\n' in upgrade[identifier][1]
    for identifier in re.findall(r'\{fileID: (\d+)\}', section(controller, 'removedControllers')):
        assert '  m_Enabled: 0\n' in upgrade[identifier][1]
    box_script = guid(assets / 'Script/Items/Upgrade/Shop/ItemBoxPoolManager.cs')
    pool = next(body for kind, body in upgrade.values() if kind == '114' and box_script in body)
    boxes = refs(section(pool, 'allBoxList'))
    assert len(boxes) == len(containers)
    for box in boxes:
        rewards = refs(section(paths[box].read_text(), 'gachaItems'))
        assert rewards and set(rewards) <= materials_all, 'shop must only supply registered materials'
    for name, blocks in [('game', game), ('upgrade', upgrade)]:
        for identifier in re.findall(r'\{fileID: (\d+)\}', blocks['910000000003'][1]):
            assert identifier == '0' or identifier in blocks, (name, 'missing scene reference', identifier)
        root = next(body for kind, body in blocks.values() if kind == '1660057539')
        assert '  - {fileID: 910000000002}' in root
    print(f'PASS merge catalog: {len(containers)} series, {len(skills)} skills, {len(items)} items, {len(enemies)} enemies; graphs, material boxes and scene references valid.')


if __name__ == '__main__':
    check_merge_game()
