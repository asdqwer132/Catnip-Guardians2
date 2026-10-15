"""Validate scene registrations, local object references and shared UI bindings."""
from pathlib import Path
import re


def check_scene_flow():
    root = Path(__file__).resolve().parents[2]
    assets = root / 'Assets'
    names = ('CoreScene', 'MainScene', 'StoryScene', 'GameGeneralScene', 'GameBattleScene', 'GameItemScene', 'UpgradeScene')
    settings = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text()
    registered = re.findall(r'- enabled: 1\n    path: Assets/Scenes/(\w+)\.unity\n    guid: (\w+)', settings)
    assert registered and registered[0][0] == 'CoreScene', 'Core must start first'
    assert set(names) <= {name for name, _ in registered}, 'all seven scenes must be enabled'
    scenes = {}
    for name, scene_guid in registered:
        path = assets / 'Scenes' / (name + '.unity')
        assert f'guid: {scene_guid}' in Path(str(path) + '.meta').read_text()
        text = path.read_text()
        entries = re.findall(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n([\s\S]*?)(?=^--- !u!|\Z)', text, re.M)
        blocks = {identifier: (kind, body) for kind, identifier, body in entries}
        assert len(blocks) == len(entries), (name, 'duplicate object IDs')
        for identifier, (_, body) in blocks.items():
            for line in body.splitlines():
                if 'guid:' in line:
                    continue
                for ref in re.findall(r'\{fileID: (-?\d+)\}', line):
                    assert ref == '0' or ref in blocks, (name, identifier, 'missing local reference', line)
        scenes[name] = (text, blocks)

    def guid(path):
        return re.search(r'^guid: (\w+)', Path(str(assets / path) + '.meta').read_text(), re.M)[1]

    core, general = scenes['CoreScene'][0], scenes['GameGeneralScene'][0]
    for script in ('CoreSceneBootstrap', 'GameSceneRoutes'):
        assert (assets / f'Script/Manager/Utility/Scene/{script}.cs').exists()
    assert guid('Script/Manager/Utility/Scene/CoreSceneBootstrap.cs') in core
    assert guid('Script/Manager/Utility/Scene/GameGeneralSceneController.cs') in general
    startup = re.search(r'^  startupScene: (\w+)', core, re.M)[1]
    assert startup in scenes and startup != 'CoreScene'
    assert '  battleScene: GameBattleScene\n' in general and '  itemScene: GameItemScene\n' in general
    ratio = float(re.search(r'^  battleRatio: (.*)', general, re.M)[1])
    assert 0.1 <= ratio <= 0.9
    assert '  restrictPointerToGameplayCamera: 1\n' in scenes['GameBattleScene'][0]
    # One EventSystem services both split views and the global overlay.
    event_script = '76c392e42b5098c458856cdf6ecaaaa1'
    assert sum(text.count('guid: ' + event_script) for text, _ in scenes.values()) == 1
    assert event_script in core
    prefab_guid = guid('Prefab/UI/Setting/Canvas/SettingCanvas.prefab')
    assert core.count('m_SourcePrefab: {fileID: 100100000, guid: ' + prefab_guid) == 1
    assert all(prefab_guid not in text for name, (text, _) in scenes.items() if name != 'CoreScene')
    window_guid = guid('Script/Manager/Setting/Setting/SettingWindowController.cs')
    assert window_guid in core
    button = (assets / 'Prefab/UI/Setting/SettingBtn.prefab').read_text()
    assert 'm_TargetAssemblyTypeName: SettingUIButton, Assembly-CSharp' in button
    assert 'm_Target: {fileID: 6783302575375445878}' in button
    assert 'm_MethodName: OpenSetting' in button
    # Main must inherit this local relay instead of referencing the removed settings instance.
    main, blocks = scenes['MainScene']
    instance = blocks['4691407'][1]
    assert 'm_OnClick.m_PersistentCalls' not in instance
    assert all('m_StringArgument: GameScene\n' not in text and 'gameSceneName: GameScene\n' not in text and
               'returnSceneName: GameScene\n' not in text for text, _ in scenes.values())
    for name, camera_id in [('CoreScene', '286779211'), ('GameGeneralScene', '208387200'), ('GameItemScene', '1889333136')]:
        _, blocks = scenes[name]
        camera = blocks[camera_id][1]
        go = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', camera, re.M)[1]
        assert 'm_TagString: Untagged' in blocks[go][1], (name, 'must not replace the battle MainCamera')
    assert '  m_Enabled: 0\n' in scenes['CoreScene'][1]['286779211'][1]
    print('PASS scene flow: seven registered scenes, persistent Core bindings, one EventSystem, settings relay, camera ownership and local references.')


if __name__ == '__main__':
    check_scene_flow()
