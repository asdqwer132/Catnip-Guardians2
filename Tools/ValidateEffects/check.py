#!/usr/bin/env python3
"""Compile effect/actor APIs, execute managed checks, and inspect Unity metadata.
No generated artifacts or downloaded dependencies are written into the project.
"""
from pathlib import Path
import argparse
import collections
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import xml.etree.ElementTree as ET
import zipfile
sys.dont_write_bytecode = True
from check_scene_flow import check_scene_flow
from check_merge_game import check_merge_game

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SDK_VERSION = '8.0.414'
UNITY_PACKAGE = 'rocketmodfix.unityengine.redist'
UNITY_VERSION = '2022.3.62.3'
NUNIT_VERSION = '3.14.0'


def run(command, **kwargs):
    return subprocess.run([str(x) for x in command], check=True, **kwargs)


def download(url, destination):
    if destination.exists():
        return
    temporary = destination.with_suffix(destination.suffix + '.partial')
    command = ['curl', '--fail', '--location', '--silent', '--show-error', '--max-time', '180']
    proxy = os.environ.get('HTTPS_PROXY') or os.environ.get('https_proxy')
    if proxy:
        # Some cloud runtimes inherit NO_PROXY for NuGet but require the proxy.
        command += ['--proxy', proxy, '--noproxy', '']
    run(command + [url, '-o', temporary])
    temporary.replace(destination)


def metadata():
    changed = subprocess.check_output(['git', 'diff', '--name-only', '-z'], cwd=ROOT, text=True).split('\0')
    staged = subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z'], cwd=ROOT, text=True).split('\0')
    untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z'], cwd=ROOT, text=True).split('\0')
    guids = collections.defaultdict(list)
    pattern = re.compile(r'^guid: ([0-9a-f]{32})$', re.M)
    for path in (ROOT / 'Assets').rglob('*.meta'):
        match = pattern.search(path.read_text(encoding='utf8', errors='replace'))
        if match:
            guids[match[1]].append(str(path.relative_to(ROOT)))
    missing, invalid, replaced = [], [], []
    inspected = 0
    for name in dict.fromkeys(changed + staged + untracked):
        path = ROOT / name
        if not name.startswith('Assets/') or not path.is_file() or path.suffix not in ('.cs', '.asset', '.prefab'):
            continue
        inspected += 1
        sidecar = Path(str(path) + '.meta')
        if not sidecar.exists():
            missing.append(name)
            continue
        match = pattern.search(sidecar.read_text(encoding='utf8', errors='replace'))
        if not match:
            invalid.append(str(sidecar.relative_to(ROOT)))
        old = subprocess.run(['git', 'show', 'HEAD:' + str(sidecar.relative_to(ROOT))], cwd=ROOT, capture_output=True, text=True)
        previous = pattern.search(old.stdout) if old.returncode == 0 else None
        if match and previous and match[1] != previous[1]:
            replaced.append(name)
    duplicate = {guid: paths for guid, paths in guids.items() if len(paths) > 1}
    result = {'asset_guids': len(guids), 'touched_assets_checked': inspected, 'missing_meta': missing,
              'invalid_guids': invalid, 'changed_existing_guids': replaced, 'duplicate_guids': duplicate}
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return not (missing or invalid or replaced or duplicate)


def prepare_dependencies(cache):
    cache.mkdir(parents=True, exist_ok=True)
    dotnet = shutil.which('dotnet')
    if not dotnet:
        dotnet_root = cache / 'dotnet'
        dotnet = dotnet_root / 'dotnet'
        if not dotnet.exists():
            if sys.platform != 'linux' or os.uname().machine not in ('x86_64', 'amd64'):
                raise RuntimeError('Install .NET SDK 8 or later on this platform, then rerun.')
            archive = cache / ('dotnet-sdk-' + SDK_VERSION + '-linux-x64.tar.gz')
            download('https://builds.dotnet.microsoft.com/dotnet/Sdk/' + SDK_VERSION + '/' + archive.name, archive)
            dotnet_root.mkdir(exist_ok=True)
            with tarfile.open(archive) as bundle:
                bundle.extractall(dotnet_root, filter='data')
    refs = cache / 'refs'
    refs.mkdir(exist_ok=True)
    for package, version in ((UNITY_PACKAGE, UNITY_VERSION), ('nunit', NUNIT_VERSION)):
        archive = cache / (package + '.' + version + '.nupkg')
        download('https://api.nuget.org/v3-flatcontainer/' + package + '/' + version + '/' + archive.name, archive)
        with zipfile.ZipFile(archive) as bundle:
            for name in bundle.namelist():
                if name.startswith('lib/netstandard2.0/') and name.endswith('.dll'):
                    (refs / Path(name).name).write_bytes(bundle.read(name))
    return Path(dotnet), refs


def project(path, files, refs, executable=False, defines='', additional_refs=()):
    root = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    properties = ET.SubElement(root, 'PropertyGroup')
    values = {'TargetFramework': 'net8.0', 'EnableDefaultCompileItems': 'false', 'ImplicitUsings': 'disable',
              'Nullable': 'disable', 'GenerateAssemblyInfo': 'false', 'NoWarn': 'CS0649;CS0169;CS0414;CS0436;CS0067;CS8981'}
    if executable:
        values['OutputType'] = 'Exe'
    if defines:
        values['DefineConstants'] = defines
    for key, value in values.items():
        ET.SubElement(properties, key).text = value
    items = ET.SubElement(root, 'ItemGroup')
    for filename in dict.fromkeys(files):
        ET.SubElement(items, 'Compile', Include=str(filename))
    for filename in (list(refs.glob('*.dll')) if refs is not None else []) + list(additional_refs):
        reference = ET.SubElement(items, 'Reference', Include=filename.stem)
        ET.SubElement(reference, 'HintPath').text = str(filename)
    ET.indent(root)
    ET.ElementTree(root).write(path, encoding='unicode')


def compile_files():
    files = list((ROOT / 'Assets/Script/Items/Effect').rglob('*.cs'))
    files += list((ROOT / 'Assets/Script/Items/Data').rglob('*.cs'))
    files += list((ROOT / 'Assets/Script/Entity').rglob('*.cs'))
    files += list((ROOT / 'Assets/Editor/Tests').rglob('*.cs'))
    files = [path for path in files if '/UI/' not in str(path) and path.name not in
             ('ItemUseManager.cs', 'BagSelectManager.cs', 'ItemUsePositionProvider.cs')]
    files += list((ROOT / 'Assets/Script/Items/Effect/Impliation/Buff/UI').glob('*.cs'))
    extra = ('Items/SpecialItemManager.cs', 'Items/Upgrade/Equipment/BagData.cs', 'Items/Upgrade/Inventory/InventoryItem.cs',
             'Items/Upgrade/Inventory/Search/ISearchable.cs', 'Items/Upgrade/Skill/Reward/Unlock/IUnlockable.cs',
             'Items/Upgrade/Skill/Reward/Unlock/DataType.cs')
    files += [ROOT / 'Assets/Script' / name for name in extra]
    files += list((ROOT / 'Assets/Script/Manager/Utility/Scene').glob('*.cs'))
    files += list((ROOT / 'Assets/Script/Items/Merge').glob('*.cs'))
    files += [ROOT / 'Assets/Script/Items/Upgrade/Craft/Recipe/ItemRecipeData.cs']
    files += list((ROOT / 'Assets/Script/Manager/Currency').glob('*.cs'))
    files += list((ROOT / 'Assets/Script/Manager/Game/Statistics').glob('*.cs'))
    files += list((ROOT / 'Assets/Script/Manager/Story').glob('*.cs'))
    scene_extra = ('Manager/Utility/Input/GameInputManager.cs', 'Manager/Utility/Input/GameInputBlockType.cs',
                   'Manager/Game/GameSession.cs', 'Manager/Game/GameEndManager.cs', 'Manager/Game/UpgradeManager.cs',
                   'Manager/Game/GameManager.cs',
                   'Manager/Setting/Audio/SceneBgmManager.cs',
                   'Manager/Setting/Audio/AudioLibrary.cs',
                   'Manager/Setting/Setting/SettingManager.cs', 'Manager/Setting/Setting/GameSettingData.cs',
                   'Manager/Setting/Setting/SettingChangeType.cs', 'Manager/Setting/Setting/ISettingChangeListener.cs',
                   'Manager/Setting/Setting/SettingWindowController.cs', 'Manager/Setting/Setting/SettingUIButton.cs',
                   'Manager/Setting/Setting/SettingInjecter.cs', 'Manager/Setting/Setting/CursorChanger.cs',
                   'Manager/Tutorial/Manager/TutorialEventExecutor.cs', 'Manager/Tutorial/Manager/TutorialProgress.cs', 'Manager/Tutorial/Manager/TutorialSave.cs',
                   'Manager/Tutorial/Data/TutorialSequenceStep.cs')
    files += [ROOT / 'Assets/Script' / name for name in scene_extra]
    return list(dict.fromkeys(files))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--metadata-only', action='store_true', help='Check asset sidecars/GUIDs without dependencies.')
    parser.add_argument('--cache', type=Path, default=Path(tempfile.gettempdir()) / 'catnip-validation-cache')
    args = parser.parse_args()
    metadata_ok = metadata()
    check_scene_flow()
    check_merge_game()
    if args.metadata_only:
        return 0 if metadata_ok else 1
    dotnet, refs = prepare_dependencies(args.cache.resolve())
    work = Path(tempfile.mkdtemp(prefix='catnip-validation-'))
    print('Temporary build output:', work, flush=True)
    environment = os.environ.copy()
    environment.update(DOTNET_CLI_HOME=str(work), DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1',
                       DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
    sources = compile_files()
    sources += [ROOT / 'Assets/Editor/BuffTargetGroupDiscovery.cs']
    compile_path = work / 'Compile.csproj'
    project(compile_path, sources + [HERE / 'GameApi.cs', HERE / 'SceneFlowApi.cs'], refs)
    print('Compiling', len(sources), 'production/test files against Unity 2022.3 references; engine tests compile only.', flush=True)
    run([dotnet, 'build', compile_path, '--nologo', '-v:q'], env=environment)
    editor_path = work / 'editor' / 'GroupInspector.csproj'
    editor_path.parent.mkdir()
    editor_files = ('Assets/Editor/CoreScenePlayBootstrap.cs', 'Assets/Script/Manager/Utility/Scene/CoreSceneBootstrap.cs',
                    'Assets/Script/Manager/Utility/Scene/SceneMoveManager.cs', 'Assets/Editor/BuffTargetGroupNameDrawer.cs',
                    'Assets/Editor/BuffEditor/BuffTargetResolverEditors.cs',
                    'Assets/Editor/BuffEditor/BuffModifierEditorGUI.cs',
                    'Assets/Editor/BuffEditor/FloatFieldBuffModifierEditor.cs',
                    'Assets/Editor/SummonModuleEditor.cs', 'Assets/Editor/SummonActorEditor.cs',
                    'Assets/Editor/PrefabRangeGizmoDrawer.cs', 'Assets/Editor/PrefabVisualAnimatorEditor.cs',
                    'Assets/Editor/UseItemStatusReactionModuleEditor.cs', 'Assets/Editor/ViewportCanvasScalerEditor.cs',
                    'Assets/Editor/UIObjectPlacementLayout.cs', 'Assets/Editor/UIObjectPlacementUtility.cs',
                    'Assets/Editor/UIObjectPlacementWindow.cs', 'Assets/Editor/Tests/UIObjectPlacementTests.cs',
                    'Assets/Editor/Tests/MergeItemPrefabAssetTests.cs',
                    'Assets/Editor/Tests/BuffTargetInspectorTests.cs',
                    'Assets/Editor/Tests/BuffDurationEditorTests.cs',
                    'Assets/Editor/Tests/SummonArchitectureEditorTests.cs',
                    'Assets/Editor/Tests/Food1Weapon1BuffTests.cs')
    project(editor_path, [ROOT / name for name in editor_files] + [HERE / 'EditorApi.cs'], refs,
            defines='UNITY_EDITOR', additional_refs=[work / 'bin/Debug/net8.0/Compile.dll'])
    print('Compiling group Inspector and asset-driven EditMode tests with Editor API substitutes; no native Unity/GUI tests execute.', flush=True)
    run([dotnet, 'build', editor_path, '--nologo', '-v:q'], env=environment)
    editor_metadata_path = work / 'editor-metadata' / 'EditorMetadataChecks.csproj'
    editor_metadata_path.parent.mkdir()
    project(editor_metadata_path, [HERE / 'EditorMetadataChecks.cs'], refs, executable=True,
            additional_refs=[work / 'bin/Debug/net8.0/Compile.dll',
                             editor_path.parent / 'bin/Debug/net8.0/GroupInspector.dll'])
    run([dotnet, 'run', '--project', editor_metadata_path, '--nologo'], env=environment)
    effect = ROOT / 'Assets/Script/Items/Effect'
    selected = ('Impliation/IStat.cs', 'Excuter/Data/EffectStatUtility.cs', 'Excuter/Data/EffectExecutionScale.cs',
                'Excuter/Displayer/PrefabVisualAnimationSettings.cs', 'Excuter/Displayer/PrefabVisualRange.cs',
                'Excuter/Data/ItemEffectContext.cs', 'Excuter/Data/ItemEffectPlan.cs', 'Excuter/Data/ItemEffectLifetime.cs',
                'Excuter/Excuter/BagItemCooldownController.cs', 'Excuter/Excuter/Throw/ItemEffectExecutor.cs',
                'Excuter/Excuter/ItemUseDeliverySettings.cs',
                'Excuter/Excuter/Throw/ItemThrowArrivalTiming.cs',
                'Excuter/Excuter/Throw/ItemThrowOptions.cs',
                'Impliation/Attack/DamageArea/DamageAreaAttackStat.cs',
                'Impliation/Attack/Summon/SummonSpawnStat.cs', 'Impliation/Attack/Summon/Stats/SummonModuleStats.cs',
                'Impliation/Attack/Summon/SummonDefinition.cs', 'Impliation/Attack/Summon/SummonRegistry.cs',
                'Impliation/Attack/Summon/SummonGroupDefinition.cs', 'Impliation/Attack/Summon/SummonGroupActivationGate.cs',
                'Impliation/Attack/Summon/SummonGroupRuntime.cs',
                'Impliation/Attack/Summon/SummonGroupReactionModule.cs',
                'Impliation/Attack/Summon/SummonGroupSpawnPosition.cs',
                'Impliation/Attack/Summon/SummonGroupSustainedSpawnModule.cs', 'Excuter/Data/EffectPositionAnchor.cs',
                'Impliation/Attack/Summon/SummonContactActivationGate.cs',
                'Impliation/Attack/Summon/SummonPeriodicSpawnSchedule.cs',
                'Impliation/Cooldown/ItemCooldownStat.cs', 'Impliation/Cooldown/BagCooldownStat.cs',
                'Impliation/Buff/Data/Modifier/BuffModifier.cs', 'Impliation/Buff/Data/Modifier/FloatFieldBuffModifier.cs',
                'Impliation/Buff/Data/BuffModeEnums.cs', 'Impliation/Buff/Data/Resolver/IBuffTarget.cs',
                'Impliation/Buff/Data/BuffInfo.cs', 'Impliation/Buff/Data/BuffFlagDefinition.cs',
                'Impliation/Buff/Data/BuffClassificationDefinition.cs', 'Impliation/Buff/Data/BuffClassificationUtility.cs',
                'Impliation/Buff/Manager/ActiveBuff.cs', 'Impliation/Buff/Manager/BuffStorage.cs',
                'Impliation/Buff/Manager/BuffStatCloneUtility.cs',
                'Impliation/Buff/Manager/BuffRemovalReason.cs',
                'Impliation/SpecialStatus/SpecialStatusDefinition.cs',
                'Impliation/SpecialStatus/SpecialStatusItemUseModule.cs',
                'Impliation/SpecialStatus/UseItemStatusReactionModule.cs',
                'Impliation/SpecialStatus/ExecuteEffectsStatusReactionModule.cs',
                'Impliation/SpecialStatus/SpecialStatusItemUseDispatcher.cs',
                'Excuter/Data/ItemEffectUtility.cs', 'Impliation/Buff/Data/BuffCleanseFilter.cs',
                'Impliation/Buff/Data/Resolver/BuffTargetGroupNameAttribute.cs',
                'Impliation/Buff/Data/Resolver/BuffSelectionUtility.cs',
                'Impliation/Buff/Data/Resolver/BuffTargetResolver.cs',
                'Impliation/Buff/Data/Resolver/AllTargetsBuffTargetResolver.cs',
                'Impliation/Buff/Manager/BuffRegisterContext.cs',
                'Impliation/Buff/Manager/BuffQueryContext.cs', 'Impliation/Buff/Manager/BuffTargetHandle.cs',
                'Impliation/Repeat/RepeatItemStat.cs', 'Impliation/Repeat/AttackPlacement.cs',
                'Impliation/Attack/HitEffect/HitEffectApplyMode.cs',
                'Impliation/Attack/HitEffect/HitEffectContext.cs', 'Impliation/Attack/HitEffect/HitEffectDispatcher.cs',
                'Impliation/Random/WeightedRandomEffect.cs', 'Impliation/EnemyControl/TimeStopRuntime.cs')
    pure_dir = work / 'managed'
    pure_dir.mkdir()
    managed_path = pure_dir / 'Managed.csproj'
    scene_logic = [ROOT / 'Assets/Script/Manager/Utility/Scene' / name for name in ('GameSceneRoutes.cs', 'GameViewportLayout.cs', 'SceneStartupPolicy.cs', 'ScenePlaySettings.cs', 'SceneUILayout.cs', 'SceneUIScaleProfile.cs', 'SceneScreenPresentationMath.cs', 'SceneDataCarrier.cs')]
    scene_logic += [ROOT / 'Assets/Script/Items/Merge' / name for name in ('MergeItem.cs', 'MergeTree.cs', 'MergeItemSelectionGesture.cs')]
    scene_logic += [HERE / 'ManagedMergeGameChecks.cs']
    scene_logic += [ROOT / 'Assets/Editor/UIObjectPlacementLayout.cs', HERE / 'ManagedUIPlacementChecks.cs']
    project(managed_path, [effect / name for name in selected] + scene_logic + [ROOT / 'Assets/Editor/BuffTargetGroupDiscovery.cs', HERE / 'ManagedApi.cs', HERE / 'ManagedChecks.cs', HERE / 'ManagedBuffGroupChecks.cs', HERE / 'ManagedAllTargetsChecks.cs', HERE / 'ManagedSummonChecks.cs', HERE / 'ManagedSummonModuleChecks.cs', HERE / 'ManagedPrefabVisualChecks.cs', HERE / 'ManagedStatusReactionChecks.cs', HERE / 'ManagedSummonGroupChecks.cs', HERE / 'ManagedSustainedSummonChecks.cs', HERE / 'ManagedSceneFlowChecks.cs'], refs, executable=True)
    run([dotnet, 'run', '--project', managed_path, '--nologo'], env=environment)
    # Exercise the real owner callbacks with duplicate-registration checks on every enabled write.
    input_dir = work / 'scene-input'
    input_dir.mkdir()
    input_path = input_dir / 'SceneInput.csproj'
    project(input_path, [ROOT / 'Assets/Script/Manager/Utility/Scene/SceneEventSystemOwner.cs',
                         ROOT / 'Assets/Script/Manager/Utility/Scene/GameSceneRoutes.cs',
                         ROOT / 'Assets/Script/Manager/Utility/Scene/SceneAudioListenerOwner.cs',
                         ROOT / 'Assets/Script/Manager/Utility/Scene/SceneCameraBinding.cs',
                         HERE / 'ManagedCameraChecks.cs',
                         ROOT / 'Assets/Script/Manager/Utility/Scene/SceneAudioChannel.cs',
                         HERE / 'ManagedSceneAudioApi.cs', HERE / 'ManagedSceneAudioChecks.cs',
                         HERE / 'ManagedEventSystemChecks.cs'], None, executable=True)
    run([dotnet, 'run', '--project', input_path, '--nologo'], env=environment)
    print('API compile and managed checks passed. Unity 6000.4 import/physics/play validation remains required.')
    if not metadata_ok:
        print('Metadata validation failed; see the missing/invalid GUID report above.', file=sys.stderr)
    return 0 if metadata_ok else 1


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (subprocess.CalledProcessError, RuntimeError) as error:
        if isinstance(error, subprocess.CalledProcessError):
            print('Validation command failed with exit code', error.returncode, file=sys.stderr)
        else:
            print('Validation failed:', error, file=sys.stderr)
        sys.exit(1)
