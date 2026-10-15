# Repository working rules

- Fetch and inspect the latest `origin/work` before editing. Preserve user scene, prefab and data changes.
- The user authors UI. Connect existing UI references and events; do not generate UI objects or layouts in code.
- Do not introduce `FindObjectsByType`, `FindObjectsOfType`, `FindObjectOfType`, `FindFirstObjectByType` or `FindAnyObjectByType` calls. Use explicit references or lifecycle registration instead.
- Fix compiler/analyzer warnings at their source. Replace APIs deprecated by the project's pinned Unity version; do not hide warnings with pragmas or warning-suppression settings. Compilation against the cloud's older reference DLLs does not establish that Unity Editor has no warnings.
- Do not assign `EventSystem.current` during initialization. Let Unity manage its EventSystem registration and current selection.
- Keep battle and item scenes independently testable. General may load Core and its child scenes only through explicit, configurable options.
