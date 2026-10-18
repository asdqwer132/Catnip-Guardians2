using UnityEngine;

public enum GameScreenLayout { SideBySide, TopAndBottom, BattleOnly, ItemOnly }

[System.Serializable]
public struct SceneViewportPadding
{
    [Min(0)] public float left, right, top, bottom;
    // 분할 비율은 바꾸지 않고 할당된 셀 안에서만 줄입니다. 과한 패딩도 화면을 뒤집지 않습니다.
    public Rect Inset(Rect allocation, Vector2 outputPixels, float uiScale)
    {
        if(allocation.width<=0 || allocation.height<=0 || Safe(outputPixels.x)<=0 || Safe(outputPixels.y)<=0) return allocation;
        uiScale=Safe(uiScale);
        double l=(double)Safe(left)*uiScale/outputPixels.x, r=(double)Safe(right)*uiScale/outputPixels.x;
        double b=(double)Safe(bottom)*uiScale/outputPixels.y, t=(double)Safe(top)*uiScale/outputPixels.y;
        double x=l+r>0?System.Math.Min(1,System.Math.Max(0,allocation.width-1d/outputPixels.x)/(l+r)):1;
        double y=b+t>0?System.Math.Min(1,System.Math.Max(0,allocation.height-1d/outputPixels.y)/(b+t)):1;
        return new Rect((float)(allocation.x+l*x),(float)(allocation.y+b*y),
            (float)(allocation.width-(l+r)*x),(float)(allocation.height-(b+t)*y));
    }
    private static float Safe(float value) => float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Max(0,value);
}

public static class GameViewportLayout
{
    // 먼저 분할한 뒤 콘텐츠 영역에 맞춥니다. 단일 화면 모드에도 상단 공간을 유지합니다.
    public static void Calculate(GameScreenLayout layout, float battleRatio, bool battleFirst, float gap,
        float topInset, out Rect battle, out Rect item)
    {
        Calculate(layout, battleRatio, battleFirst, gap, out battle, out item);
        topInset = float.IsNaN(topInset) ? 0f : Mathf.Clamp(topInset, 0f, 0.8f);
        float height = 1f - topInset;
        battle = new Rect(battle.x, battle.y * height, battle.width, battle.height * height);
        item = new Rect(item.x, item.y * height, item.width, item.height * height);
    }

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
