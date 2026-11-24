using UnityEngine;
using UnityEngine.UI;

// 기존 UnityEvent/Inspector 연결을 유지하며 공통 단일 클립 재생기로 전달합니다.
[RequireComponent(typeof(Image), typeof(ShopSlotOpenAnimationUI))]
public class ShopBoxOpenAnimationUI : MonoBehaviour
{
    public Image boxImage;
    public Animator boxAnimator;
    private ShopSlotOpenAnimationUI playback;
    private ItemBoxData currentBoxData;
    private void Awake() => Resolve();
    private void Resolve()
    {
        if (boxImage == null) boxImage = GetComponent<Image>();
        if (boxAnimator == null) boxAnimator = GetComponent<Animator>();
        if (playback == null) playback = GetComponent<ShopSlotOpenAnimationUI>();
        playback.icon = boxImage;
        if (boxAnimator != null) playback.targetAnimator = boxAnimator;
    }
    public void SetBox(ItemBoxData boxData)
    {
        Resolve(); currentBoxData = boxData; playback.ResetToBox(boxData);
    }
    public void PlayOpen() { Resolve(); playback.Play(currentBoxData); }
    public void PlayIdle() { Resolve(); playback.ResetToBox(currentBoxData); }
    public void Clear() => SetBox(null);
}
