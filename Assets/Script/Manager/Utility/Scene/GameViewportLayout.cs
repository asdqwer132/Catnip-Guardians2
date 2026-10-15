using UnityEngine;

public enum GameScreenLayout { SideBySide, TopAndBottom, BattleOnly, ItemOnly }

public static class GameViewportLayout
{
    public static void Calculate(GameScreenLayout layout, float battleRatio, bool battleFirst, float gap, out Rect battle, out Rect item)
    {
        battle = item = new Rect(0, 0, 0, 0);
        if (layout == GameScreenLayout.BattleOnly) { battle = new Rect(0, 0, 1, 1); return; }
        if (layout == GameScreenLayout.ItemOnly) { item = new Rect(0, 0, 1, 1); return; }
        battleRatio = float.IsNaN(battleRatio) ? 0.5f : Mathf.Clamp(battleRatio, 0.1f, 0.9f);
        gap = float.IsNaN(gap) ? 0f : Mathf.Clamp(gap, 0f, 0.1f);
        float first = (1f - gap) * (battleFirst ? battleRatio : 1f - battleRatio);
        float second = 1f - first - gap;
        Rect a, b;
        if (layout == GameScreenLayout.TopAndBottom)
        { a = new Rect(0, 1f - first, 1, first); b = new Rect(0, 0, 1, second); }
        else { a = new Rect(0, 0, first, 1); b = new Rect(first + gap, 0, second, 1); }
        battle = battleFirst ? a : b; item = battleFirst ? b : a;
    }
}
