"""Validate scene registrations, local object references and shared UI bindings."""
from pathlib import Path
import re


def check_scene_flow():
    root = Path(__file__).resolve().parents[2]
    assets = root / 'Assets'
    # Repository policy: lifecycle registration/references instead of this global lookup API.
    ignored_text = re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|//[^\n]*|/\*[\s\S]*?\*/')
    for path in assets.rglob('*.cs'):
        code = ignored_text.sub('', path.read_text(encoding='utf-8-sig', errors='replace'))
        assert not re.search(r'\bFindObjectsByType\s*[<(]', code), (path, 'FindObjectsByType is forbidden; use references or lifecycle registration')
    print('PASS source policy: no FindObjectsByType calls.')
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
    auto_start = re.search(r'^  autoLoadStartupScene: ([01])', core, re.M)[1] == '1'
    startup = re.search(r'^  startupScene: *(.*)', core, re.M)[1].strip().strip('"')
    startup = Path(startup).stem
    startup = {'GameScene': 'GameGeneralScene', 'Main': 'MainScene'}.get(startup, startup)
    if auto_start: assert startup in scenes and startup != 'CoreScene'
    assert '  battleScene: GameBattleScene\n' in general and '  itemScene: GameItemScene\n' in general
    assert re.search(r'^  loadCoreScene: [01]$', general, re.M), 'General must expose its Core loading option'
    ratio = float(re.search(r'^  battleRatio: (.*)', general, re.M)[1])
    assert 0.1 <= ratio <= 0.9
    assert '  restrictPointerToGameplayCamera: 1\n' in scenes['GameBattleScene'][0]
    # Every scene can process standalone UI; the owner component arbitrates additive combinations.
    event_script = '76c392e42b5098c458856cdf6ecaaaa1'
    module_script = '01614664b831546d2ae94a42149d80ac'
    owner_guid = guid('Script/Manager/Utility/Scene/SceneEventSystemOwner.cs')
    audio_guid = guid('Script/Manager/Utility/Scene/SceneAudioListenerOwner.cs')
    loader_guid = guid('Script/Manager/Utility/Scene/SceneMoveManager.cs')
    for name, (text, blocks) in scenes.items():
        assert text.count('guid: ' + event_script) == 1, (name, 'one local EventSystem required')
        assert owner_guid in text, (name, 'additive EventSystem arbitration required')
        event_id, event = next((key, body) for key, (_, body) in blocks.items() if event_script in body)
        _, owner = next((key, body) for key, (_, body) in blocks.items() if owner_guid in body)
        event_host = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', event, re.M)[1]
        assert f'  m_GameObject: {{fileID: {event_host}}}\n' in owner, (name, 'owner must control its local EventSystem')
        assert '  m_Enabled: 0\n' in event, (name, event_id, 'EventSystem must start disabled to prevent automatic duplicate registration')
        assert '  m_Enabled: 1\n' in owner, (name, 'owner must enable standalone input at runtime')
        module_id, module = next((key, body) for key, (_, body) in blocks.items() if module_script in body)
        assert f'  m_GameObject: {{fileID: {event_host}}}\n' in module
        assert '  m_Enabled: 0\n' in module, (name, 'input module must start disabled')
        assert f'  inputModules:\n  - {{fileID: {module_id}}}\n' in owner, (name, 'explicit runtime input module required')
        if name != 'CoreScene': assert loader_guid in text, (name, 'standalone navigation required')
        listeners = [(key, body) for key, (kind, body) in blocks.items() if kind == '81']
        assert len(listeners) == 1, (name, 'one standalone/fallback audio listener required')
        _, listener = listeners[0]
        assert '  m_Enabled: 0\n' in listener, (name, 'listener must wait for ownership before enabling')
        audio = next(body for _, body in blocks.values() if audio_guid in body)
        listener_host = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', listener, re.M)[1]
        assert f'  m_GameObject: {{fileID: {listener_host}}}\n' in audio
        assert '  m_Enabled: 1\n' in audio
        expected_priority = 0 if name == 'CoreScene' else -50 if name == 'GameGeneralScene' else 50 if name == 'GameItemScene' else 100
        assert f'  priority: {expected_priority}\n' in audio
        assert f'  requireEnabledCamera: {0 if name in ("CoreScene", "GameGeneralScene") else 1}\n' in audio
    play_settings = (assets / 'Data/ScenePlaySettings.asset').read_text()
    assert guid('Script/Manager/Utility/Scene/ScenePlaySettings.cs') in play_settings
    assert int(re.search(r'^  playMode: (\d+)', play_settings, re.M)[1]) in range(3)
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
    for name, camera_id in [('CoreScene', '286779211'), ('GameGeneralScene', '208387200')]:
        _, blocks = scenes[name]
        if name == 'GameGeneralScene' and camera_id not in blocks: continue
        camera = blocks[camera_id][1]
        go = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', camera, re.M)[1]
        assert 'm_TagString: Untagged' in blocks[go][1], (name, 'must not replace the battle MainCamera')
    # Core's camera visibility is authored by the user; audio fallback works with either setting.
    anchor_guid = guid('Script/Items/Effect/Excuter/Data/EffectPositionAnchor.cs')
    assert anchor_guid in scenes['GameBattleScene'][0] and '  anchorKey: Player\n' in scenes['GameBattleScene'][0]
    assert 'm_TagString: MainCamera' in scenes['GameItemScene'][1]['1889333134'][1]
    print('PASS scene flow: seven registered scenes, standalone UI/input/navigation bindings, configurable Core entry, settings relay and local references.')


if __name__ == '__main__':
    check_scene_flow()
