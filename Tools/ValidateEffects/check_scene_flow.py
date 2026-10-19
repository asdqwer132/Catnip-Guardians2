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

    # Content scenes use local freeform lights with separate sorting-layer targets.
    def local_light_targets(blocks):
        result = set()
        for _, body in blocks.values():
            if 'guid: 073797afb82c5a1438f328866b10b3f0' not in body:
                continue
            assert '  m_LightType: 1\n' in body, 'Content light must be local Freeform, not Global'
            blend = int(re.search(r'^  m_BlendStyleIndex: (\d+)', body, re.M)[1])
            raw = bytes.fromhex(re.search(r'^  m_ApplyToSortingLayers: (\w+)', body, re.M)[1])
            for index in range(0, len(raw), 4):
                key = (int.from_bytes(raw[index:index + 4], 'little', signed=True), blend)
                assert key not in result, ('unexpected overlapping base light target', key)
                result.add(key)
        return result

    item_targets = local_light_targets(scenes['GameItemScene'][1])
    battle_targets = local_light_targets(scenes['GameBattleScene'][1])
    assert item_targets and battle_targets and item_targets.isdisjoint(battle_targets), 'Battle/Item local lights must target different sorting layers'
    tags = (root / 'ProjectSettings/TagManager.asset').read_text()
    item_sorting_id = int(re.search(r'- name: GameItemWorld\n    uniqueID: (\d+)', tags)[1])
    assert tags.index('- name: GameItemWorld') < tags.index('- name: Default'), 'Shared Core UI must draw above the item world'
    assert item_targets == {(item_sorting_id, 0)}
    # Match Canvases, SortingGroups and SpriteMask ranges as well as sprite renderers.
    paths = [assets / 'Scenes/GameItemScene.unity'] + [assets / 'Prefab/Merge' / name for name in
             ('MergeItemObject.prefab', 'BottomWall.prefab', 'LeftWall.prefab', 'RightWall.prefab')]
    for path in paths:
        layer_ids = re.findall(r'^  m_(?:Front|Back)?SortingLayerID: (-?\d+)$', path.read_text(), re.M)
        assert layer_ids and all(int(value) == item_sorting_id for value in layer_ids), (path, 'Item visuals/masks must match the item global light')
    print('PASS local lighting: Freeform Battle/Item lights, separate targets, pooled visuals/masks and Canvas ordering.')

    def guid(path):
        return re.search(r'^guid: (\w+)', Path(str(assets / path) + '.meta').read_text(), re.M)[1]

    core, general = scenes['CoreScene'][0], scenes['GameGeneralScene'][0]
    # Cross-scene buttons serialize local relays; the Battle keeps all result/manager references local.
    battle_blocks = scenes['GameBattleScene'][1]
    general_blocks = scenes['GameGeneralScene'][1]
    context_id, context = next((key, body) for key, (_, body) in battle_blocks.items()
                              if guid('Script/Manager/Utility/Scene/BattleSceneContext.cs') in body)
    relay_id, relay = next((key, body) for key, (_, body) in general_blocks.items()
                          if guid('Script/Manager/Utility/Scene/BattleSceneButtonRelay.cs') in body)
    manager_id = re.search(r'^  gameManager: \{fileID: (\d+)\}', context, re.M)[1]
    manager = battle_blocks[manager_id][1]
    assert 'Assembly-CSharp::GameManager\n' in manager
    end_id = re.search(r'^  endManager: \{fileID: (\d+)\}', manager, re.M)[1]
    result_id = re.search(r'^  resultPanel: \{fileID: (\d+)\}', battle_blocks[end_id][1], re.M)[1]
    assert result_id != '0' and result_id in battle_blocks
    controller_id = re.search(r'^  sceneController: \{fileID: (\d+)\}', relay, re.M)[1]
    assert guid('Script/Manager/Utility/Scene/GameGeneralSceneController.cs') in general_blocks[controller_id][1]
    assert '  m_Enabled: 1\n' in context and '  m_Enabled: 1\n' in relay
    assert any(f'm_Target: {{fileID: {relay_id}}}\n' in body and
               'm_TargetAssemblyTypeName: BattleSceneButtonRelay, Assembly-CSharp\n' in body and
               'm_MethodName: RequestDefeat\n' in body for _, body in general_blocks.values())
    print('PASS cross-scene defeat wiring: General button -> local relay/controller -> Battle Context/GameManager/result panel.')
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
    # Every authored camera is registered; test roles rather than fixed camera IDs.
    camera_guid = guid('Script/Manager/Utility/Scene/SceneCameraBinding.cs')
    profiles = {}
    def mask(body, field):
        return int(re.search(r'^  ' + field + r':\n    serializedVersion: \d+\n    m_Bits: (\d+)', body, re.M)[1])
    for name, (_, blocks) in scenes.items():
        cameras = [(identifier, body) for identifier, (kind, body) in blocks.items() if kind == '20']
        bindings = [body for _, body in blocks.values() if 'guid: ' + camera_guid in body]
        assert len(cameras) == len(bindings) == 1, (name, 'each camera needs one explicit SceneCameraBinding')
        profile = bindings[0]; profiles[name] = profile
        camera_id, camera = cameras[0]
        go = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', camera, re.M)[1]
        assert f'  m_GameObject: {{fileID: {go}}}\n' in profile
        assert '  m_Enabled: 1\n' in profile
        if name == 'CoreScene':
            assert '  role: 0\n' in profile
            assert re.search(r'backgroundViewport:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 1\n    height: 1\n',profile), 'Core background must cover padding and gaps across the full output'
            # Core may display its shared UI layer. World content must still be excluded.
            assert all(mask(body, field) & ~(1 << 5) == 0 for body, field in
                       ((profile, 'standaloneLayers'), (profile, 'composedLayers'), (camera, 'm_CullingMask')))
            assert '  receiveVisibilitySettings: 0\n' in profile
            assert '  standaloneTag: Untagged\n' in profile and '  composedTag: Untagged\n' in profile
            assert 'm_TagString: Untagged' in blocks[go][1]
        elif name == 'GameGeneralScene':
            nav_layer = re.search(r'^  layers:\n([\s\S]*?)  m_SortingLayers:', tags, re.M)[1].splitlines().index('  - GeneralNavigation')
            assert '  role: 2\n' in profile
            assert mask(profile, 'standaloneLayers') == mask(profile, 'composedLayers') == mask(camera, 'm_CullingMask') == 1 << nav_layer
            assert '  receiveVisibilitySettings: 0\n' in profile
            assert 'm_TagString: Untagged' in blocks[go][1]
            assert mask(profiles['CoreScene'], 'standaloneLayers') & ~(1 << 5) == 0
        else:
            assert '  role: 1\n' in profile
    assert int(re.search(r'^  priority: (-?\d+)', profiles['GameGeneralScene'], re.M)[1]) > int(re.search(r'^  priority: (-?\d+)', profiles['CoreScene'], re.M)[1])
    item_bit = 1 << int(re.search(r'^  itemLayer: (\d+)', general, re.M)[1])
    assert mask(profiles['GameItemScene'], 'composedLayers') == item_bit
    assert mask(profiles['GameBattleScene'], 'composedLayers') & item_bit == 0
    assert mask(profiles['GameBattleScene'], 'composedLayers') & mask(profiles['GameGeneralScene'], 'composedLayers') == 0
    assert '  receiveVisibilitySettings: 1\n' in profiles['GameBattleScene']
    assert '  receiveVisibilitySettings: 0\n' in profiles['GameItemScene']
    for name in ('GameBattleScene', 'GameItemScene'):
        _, blocks = scenes[name]
        camera = next(body for kind, body in blocks.values() if kind == '20')
        camera_go = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', camera, re.M)[1]
        camera_transform_id = next(i for i, (kind, body) in blocks.items() if kind == '4' and f'  m_GameObject: {{fileID: {camera_go}}}\n' in body)
        light = next(body for _, body in blocks.values() if 'guid: 073797afb82c5a1438f328866b10b3f0' in body)
        light_go = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', light, re.M)[1]
        light_transform = next(body for kind, body in blocks.values() if kind == '4' and f'  m_GameObject: {{fileID: {light_go}}}\n' in body)
        assert f'  m_Father: {{fileID: {camera_transform_id}}}\n' in light_transform, (name, 'local base light must follow its content camera')
    # Core's UI must remain visible without relying on world geometry culling.
    for prefab in ('Prefab/UI/Setting/Canvas/SettingCanvas.prefab', 'Prefab/UI/Setting/Canvas/SceneMoveCanvas.prefab'):
        path = assets / prefab
        assert '  m_RenderMode: 0\n' in path.read_text(), (path, 'shared Core UI must remain Screen Space Overlay')
    loading = (assets / 'Prefab/UI/Setting/Canvas/SceneMoveCanvas.prefab').read_text()
    settings_ui = (assets / 'Prefab/UI/Setting/Canvas/SettingCanvas.prefab').read_text()
    assert '  loadingCanvas: {fileID: 453714196291250765}\n' in loading
    assert float(re.search(r'^  minimumLoadingSeconds: (.*)', loading, re.M)[1]) > 0
    assert '  m_SortingLayerID: 1581199633\n' in loading and '  m_SortingOrder: 300\n' in loading
    assert '  m_SortingLayerID: 1581199633\n' in settings_ui and '  m_SortingOrder: 200\n' in settings_ui
    assert 'propertyPath: m_SortingOrder\n      value: 300\n' in core
    assert '  navigationSortingOrder: 100\n' in general
    assert '  navigationArea: {fileID: 1510854061}\n' in general
    assert '  navigationSizeMode: 0\n' in general
    assert float(re.search(r'^  navigationHeightRatio: (.*)', general, re.M)[1]) == .1
    assert ratio == .6 and float(re.search(r'^  gap: (.*)', general, re.M)[1]) == 0
    item_text, item_blocks = scenes['GameItemScene']
    layout_guid = guid('Script/Manager/Utility/Scene/ItemSceneLayoutController.cs')
    fitter_guid = guid('Script/Manager/Utility/Scene/UIWorldBoundsFitter.cs')
    scaler_guid = guid('Script/Manager/Utility/Scene/ViewportCanvasScaler.cs')
    item_layout = next(body for _, body in item_blocks.values() if layout_guid in body)
    fitter = next(body for _, body in item_blocks.values() if fitter_guid in body)
    assert '  gameAreaShare: {x: 0.8, y: 0.8}\n' in item_layout
    assert all(f'  {field}: {{fileID: {value}}}\n' in item_layout for field, value in
               [('gameArea',604680137),('seriesArea',962900167),('materialsArea',563686106),('recipeArea',766217317)])
    assert '  frame: {fileID: 1599007221}\n' in fitter and '  worldRoot: {fileID: 269577203}\n' in fitter
    assert '  worldCamera: {fileID: 1889333136}\n' in fitter and '  - {fileID: 985560683}\n' in fitter
    assert '  autoFit: 1\n' in fitter
    assert '  frameOccupancy:\n    serializedVersion: 2\n' in fitter
    occupancy = re.search(r'frameOccupancy:\n    serializedVersion: 2\n    x: (.*)\n    y: (.*)\n    width: (.*)\n    height: (.*)',fitter)
    ox,oy,ow,oh = map(float,occupancy.groups())
    assert ow>0 and oh>0 and ox>=0 and oy>=0 and ox+ow<=1 and oy+oh<=1
    assert '  boards:\n  - {fileID: 1595348419}\n  - {fileID: 86893377}\n' in fitter
    assert '  selections:\n  - {fileID: 1595348420}\n' in fitter
    assert '  boards:' not in item_layout and '  selections:' not in item_layout
    assert item_text.count('guid: ' + scaler_guid) == 1
    scale_asset_guid = guid('Data/SceneUIScaleProfile.asset')
    profile_asset = (assets / 'Data/SceneUIScaleProfile.asset').read_text()
    assert guid('Script/Manager/Utility/Scene/SceneUIScaleProfile.cs') in profile_asset
    assert '  referencePixelsPerUnit: 100\n' in profile_asset
    for scene_name, expected in [('GameGeneralScene',2),('GameBattleScene',3),('GameItemScene',1)]:
        scalers = [body for _,body in scenes[scene_name][1].values() if 'guid: '+scaler_guid in body]
        assert len(scalers) == expected
        assert all(f'  sharedScaleProfile: {{fileID: 11400000, guid: {scale_asset_guid}, type: 2}}\n' in body for body in scalers)
    # Preserve the user's Unity layout groups. The old ratio controller must stay disabled.
    assert '  m_Enabled: 0\n' in item_layout
    layout_group = next(body for _,body in item_blocks.values() if 'UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup' in body and 'm_GameObject: {fileID: 1230442318}' in body)
    assert '  m_Enabled: 1\n' in layout_group
    print('PASS shared UI scale: General, Battle and Item use one profile without replacing authored Layout Groups.')
    for name in ('GameBattleScene', 'GameItemScene'):
        camera = next(body for kind, body in scenes[name][1].values() if kind == '20')
        # Standalone viewport is authored by the user; General supplies the composed split.
        rect = re.search(r'm_NormalizedViewPortRect:\n    serializedVersion: \d+\n    x: (.*)\n    y: (.*)\n    width: (.*)\n    height: (.*)\n', camera)
        assert rect, (name, 'camera viewport must be serialized')
        x,y,w,h = map(float,rect.groups())
        assert x>=0 and y>=0 and w>0 and h>0 and x+w<=1.0001 and y+h<=1.0001, (name, 'viewport must remain inside its output')
    print('PASS responsive layout: 60/40 content, authored 10/90 navigation and item panels, existing world-box references.')
    presentation_guid = guid('Script/Manager/Utility/Scene/SceneCameraPresentation.cs')
    background_guid = guid('Data/GameScreenBackground.asset')
    for name in ('CoreScene','GameGeneralScene','GameBattleScene','GameItemScene'):
        _,blocks = scenes[name]
        view = next(body for _,body in blocks.values() if 'guid: '+presentation_guid in body)
        camera = next(body for kind,body in blocks.values() if kind=='20')
        host = re.search(r'^  m_GameObject: \{fileID: (\d+)\}',camera,re.M)[1]
        assert f'  m_GameObject: {{fileID: {host}}}\n' in view
        assert f'  background: {{fileID: 11400000, guid: {background_guid}, type: 2}}\n' in view
        assert int(re.search(r'^  shape: (\d+)',view,re.M)[1]) in range(5), (name,'camera shape must stay configurable')
    renderer = (assets/'Settings/Renderer2D.asset').read_text()
    assert guid('Script/Manager/Utility/Scene/SceneScreenPresentationFeature.cs') in renderer
    assert guid('Script/Manager/Utility/Scene/SceneScreenPresentation.shader') in renderer
    assert 'm_RendererFeatures:\n  - {fileID: 910000990001}' in renderer
    print('PASS common background: four existing cameras share one profile; camera shapes remain configurable; Renderer2D feature and shader are connected.')
    audio_guid = guid('Script/Manager/Utility/Scene/SceneAudioChannel.cs')
    for name in ('GameGeneralScene', 'GameBattleScene', 'GameItemScene'):
        _, blocks = scenes[name]
        channel = next(body for _, body in blocks.values() if 'guid: ' + audio_guid in body)
        sources = [re.search(r'^  ' + field + r': \{fileID: (\d+)\}', channel, re.M)[1] for field in ('effectsSource','musicSource')]
        assert len(set(sources)) == 2
        for source_id in sources:
            kind, body = blocks[source_id]
            assert kind == '82' and '  m_PlayOnAwake: 0\n' in body, (name, 'scene sources must be explicit and must not autoplay')
        assert '  Loop: 0\n' in blocks[sources[0]][1] and '  Loop: 1\n' in blocks[sources[1]][1]
    print('PASS scene presentation: loading/settings/navigation stack, connected loader Canvas, camera-following local lights and scene-owned SFX/music sources.')
    print('PASS scene camera profiles: shared Core background/UI, isolated Navigation camera, disjoint content masks and settings ownership.')
    anchor_guid = guid('Script/Items/Effect/Excuter/Data/EffectPositionAnchor.cs')
    assert anchor_guid in scenes['GameBattleScene'][0] and '  anchorKey: Player\n' in scenes['GameBattleScene'][0]
    assert 'm_TagString: MainCamera' in scenes['GameItemScene'][1]['1889333134'][1]
    print('PASS scene flow: seven registered scenes, standalone UI/input/navigation bindings, configurable Core entry, settings relay and local references.')


if __name__ == '__main__':
    check_scene_flow()
