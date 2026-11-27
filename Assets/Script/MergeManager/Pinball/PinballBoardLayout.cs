using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PinballBoardLayout", menuName = "Items/Pinball/Board Layout")]
public sealed class PinballBoardLayout : ScriptableObject
{
    [Serializable]
    public struct PinCell
    {
        [Tooltip("Normalized position from the bottom left of the board.")]
        public Vector2 center;
        [Tooltip("Cell size relative to the width and height of the board.")]
        public Vector2 size;
    }

    [SerializeField] private PinCell[] pinCells = Array.Empty<PinCell>();
    public IReadOnlyList<PinCell> PinCells => pinCells;

#if UNITY_EDITOR
    public void Capture(RectTransform area, IReadOnlyList<BagEquipmentSlotUI> slots)
    {
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(area);
        Rect frame = area.rect;
        if (frame.width <= 0f || frame.height <= 0f)
            throw new InvalidOperationException("The pin board must have a positive width and height.");

        var cells = new PinCell[slots.Count];
        var corners = new Vector3[4];
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = (RectTransform)slots[i].transform;
            slot.GetWorldCorners(corners);
            Vector2 bottomLeft = area.InverseTransformPoint(corners[0]);
            Vector2 topRight = area.InverseTransformPoint(corners[2]);
            cells[i] = new PinCell
            {
                center = ((bottomLeft + topRight) * .5f - frame.min) / frame.size,
                size = (topRight - bottomLeft) / frame.size
            };
        }
        UnityEditor.Undo.RecordObject(this, "Capture Pinball Board Layout");
        pinCells = cells;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
