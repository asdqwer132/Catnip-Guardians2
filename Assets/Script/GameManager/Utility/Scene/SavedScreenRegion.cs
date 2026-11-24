using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI의 anchor/flexible 비율과 고정 여백을 함께 저장합니다.
// 단순 정규화 Rect만 저장하면 화면 비율 변경 시 LayoutGroup의 고정 간격이 달라집니다.
[System.Serializable]
public sealed class SavedScreenRegion
{
    [SerializeField] private Vector2 canvasSize, position, size, positionSlope, sizeSlope;

    public Rect Evaluate(Vector2 currentSize)
    {
        Vector2 delta = currentSize - canvasSize;
        Vector2 min = (position + delta * positionSlope) / currentSize;
        Vector2 max = (position + delta * positionSlope + size + delta * sizeSlope) / currentSize;
        min = Vector2.Max(Vector2.zero,Vector2.Min(Vector2.one,min));
        max = Vector2.Max(min,Vector2.Min(Vector2.one,max));
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }

    public static SavedScreenRegion Capture(RectTransform area,Rect viewport,Vector2 canvasSize)
    {
        if(area==null || canvasSize.x<=0 || canvasSize.y<=0) return null;
        var canvas=area.GetComponentInParent<Canvas>();
        if(canvas==null) return null;
        var path=new List<RectTransform>();
        for(var child=area;child!=null && child!=canvas.rootCanvas.transform;child=child.parent as RectTransform) path.Add(child);
        path.Reverse();
        Vector2 minSlope=Vector2.zero, extentSlope=Vector2.one;
        foreach(var child in path)
        {
            var group=child.parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
            for(int axis=0;axis<2;axis++)
            {
                float relativeMin=child.anchorMin[axis];
                float relativeSize=child.anchorMax[axis]-child.anchorMin[axis];
                if(group!=null && group.isActiveAndEnabled && !Ignored(child)) GroupSlope(group,child,axis,out relativeMin,out relativeSize);
                minSlope[axis]+=extentSlope[axis]*relativeMin;
                extentSlope[axis]*=relativeSize;
            }
        }
        return new SavedScreenRegion { canvasSize=canvasSize,position=viewport.min*canvasSize,size=viewport.size*canvasSize,
            positionSlope=minSlope,sizeSlope=extentSlope };
    }
    private static bool Ignored(RectTransform child)
    {
        if(!child.gameObject.activeInHierarchy) return true;
        foreach(var ignorer in child.GetComponents<ILayoutIgnorer>()) if(ignorer.ignoreLayout) return true;
        return false;
    }
    private static float Flexible(HorizontalOrVerticalLayoutGroup group,RectTransform child,int axis)
    {
        bool control=axis==0?group.childControlWidth:group.childControlHeight;
        bool expand=axis==0?group.childForceExpandWidth:group.childForceExpandHeight;
        float value=control?LayoutUtility.GetFlexibleSize(child,axis):0;
        return expand?Mathf.Max(1,value):value;
    }
    private static void GroupSlope(HorizontalOrVerticalLayoutGroup group,RectTransform child,int axis,out float min,out float extent)
    {
        bool control=axis==0?group.childControlWidth:group.childControlHeight;
        float align=axis==0?(int)group.childAlignment%3*.5f:1-(int)group.childAlignment/3*.5f;
        bool main=(group is HorizontalLayoutGroup)==(axis==0);
        if(!main)
        {
            extent=control && Flexible(group,child,axis)>0?1:0;
            min=(1-extent)*align;
            return;
        }
        var children=new List<RectTransform>();
        foreach(Transform transform in group.transform)
            if(transform is RectTransform rect && !Ignored(rect)) children.Add(rect);
        if(group.reverseArrangement) children.Reverse();
        float total=0, before=0, own=Flexible(group,child,axis);
        bool reached=false;
        foreach(var rect in children)
        {
            if(rect==child) reached=true;
            float flex=Flexible(group,rect,axis);
            if(!reached) before+=flex;
            total+=flex;
        }
        if(total<=0) { extent=0;min=align;return; }
        extent=control?own/total:0;
        min=axis==0?before/total:1-(before+own)/total;
        if(!control) min+=align*own/total;
    }
}
