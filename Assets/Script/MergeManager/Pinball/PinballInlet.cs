using System;
using UnityEngine;

[Serializable]
public sealed class PinballInlet
{
    public Transform marker;
    public Transform spawnPoint;
    public SpriteRenderer itemIcon;
    public SpriteCooldownController cooldown;
    [Min(0)] public float interval;
    public MergeItem previewMaterial;
    [Min(0)] public int previewCount;
    [ReadOnly, SerializeField] private int remaining;

    private MergeItem material;
    private float elapsed;

    public void Bind(MergeItem item, int count)
    {
        material = item;
        remaining = item != null ? Mathf.Max(0, count) : 0;
        elapsed = 0;
        if (itemIcon != null)
        {
            itemIcon.sprite = item != null ? item.item.icon : null;
            itemIcon.enabled = itemIcon.sprite != null;
        }
        cooldown?.SetFill(0);
    }

    internal void Tick(MergegameManager board, float intervalSeconds, float deltaTime)
    {
        if (remaining <= 0 || spawnPoint == null) return;

        elapsed = Mathf.Min(intervalSeconds, elapsed + deltaTime);
        cooldown?.SetCooldown(intervalSeconds - elapsed, intervalSeconds);
        if (elapsed < intervalSeconds || !board.TryDropFromInlet(material, spawnPoint.position)) return;

        elapsed = 0;
        remaining--;
        cooldown?.SetFill(remaining > 0 ? 1 : 0);
        TutorialSignals.Raise(board.gameObject.scene, TutorialSignals.ItemDropped);
    }
}
