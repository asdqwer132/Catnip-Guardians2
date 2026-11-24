using System.Collections.Generic;
using UnityEngine;

public class RecipeTreeSlotUI : RecipeSlotUI
{
    [Header("Recipe Tree Lines")]
    [SerializeField] private List<RecipeTreeLineUI> incomingLines = new();
    [SerializeField] private List<RecipeTreeLineUI> outgoingLines = new();

    public IReadOnlyList<RecipeTreeLineUI> IncomingLines => incomingLines;
    public IReadOnlyList<RecipeTreeLineUI> OutgoingLines => outgoingLines;

    public void AddIncomingLine(RecipeTreeLineUI line)
    {
        AddUnique(incomingLines, line);
    }

    public void AddOutgoingLine(RecipeTreeLineUI line)
    {
        AddUnique(outgoingLines, line);
    }

    public void ClearLineReferences()
    {
        incomingLines.Clear();
        outgoingLines.Clear();
    }

    public void SetIncomingLinesVisible(bool visible) => SetVisible(incomingLines, visible);
    public void SetOutgoingLinesVisible(bool visible) => SetVisible(outgoingLines, visible);
    public void SetAllLinesVisible(bool visible)
    {
        SetVisible(incomingLines, visible);
        SetVisible(outgoingLines, visible);
    }

    public void HighlightIncomingLines(bool highlighted) => SetHighlighted(incomingLines, highlighted);
    public void HighlightOutgoingLines(bool highlighted) => SetHighlighted(outgoingLines, highlighted);
    public void HighlightAllLines(bool highlighted)
    {
        SetHighlighted(incomingLines, highlighted);
        SetHighlighted(outgoingLines, highlighted);
    }

    private static void AddUnique(List<RecipeTreeLineUI> lines, RecipeTreeLineUI line)
    {
        if (line != null && !lines.Contains(line))
            lines.Add(line);
    }

    private static void SetVisible(List<RecipeTreeLineUI> lines, bool visible)
    {
        foreach (RecipeTreeLineUI line in lines)
            if (line != null) line.SetVisible(visible);
    }

    private static void SetHighlighted(List<RecipeTreeLineUI> lines, bool highlighted)
    {
        foreach (RecipeTreeLineUI line in lines)
            if (line != null) line.SetHighlighted(highlighted);
    }
}
