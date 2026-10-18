# Repository working rules

- Fetch and inspect the latest `origin/work` before editing. Preserve user scene, prefab and data changes.
- The user authors UI. Connect existing UI references and events; do not generate UI objects or layouts in code.
- Do not introduce `FindObjectsByType`, `FindObjectsOfType`, `FindObjectOfType`, `FindFirstObjectByType` or `FindAnyObjectByType` calls. Use explicit references or lifecycle registration instead.
- Fix compiler/analyzer warnings at their source. Replace APIs deprecated by the project's pinned Unity version; do not hide warnings with pragmas or warning-suppression settings. Compilation against the cloud's older reference DLLs does not establish that Unity Editor has no warnings.
- Do not assign `EventSystem.current` during initialization. Let Unity manage its EventSystem registration and current selection.
- Keep scene EventSystems and their input modules serialized disabled, with SceneEventSystemOwner enabled and its Input Modules connected. Disable the previous input owner before enabling a replacement.
- Keep scene AudioListeners serialized disabled and managed by SceneAudioListenerOwner. Core/General provide fallback audio; visible content cameras take priority, with only one listener enabled at a time.
- Keep battle and item scenes independently testable. General may load Core and its child scenes only through explicit, configurable options.
- Configure camera roles, standalone/composed layer masks and tags in SceneCameraBinding. Use its lifecycle registry for content/settings camera selection. Core Background and General Navigation have separate roles; Navigation uses its own layer and header viewport. Background cameras use configured priorities and must not render gameplay layers; do not infer their role from a scene/object name or Camera.main. Keep the user's authored UI intact.
- Keep content lighting local (Freeform Light2D) with scene-specific sorting targets. SceneAudioChannel owns per-scene SFX/music sources; one active AudioListener remains a Unity requirement. Do not revert local scene playback to a shared source when a channel exists.
