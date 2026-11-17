using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class CoolButton : SpriteButton
{
    [Tooltip("별도의 지속시간 이미지가 있으면 연결합니다. 없으면 Cool 이미지로 지속시간과 쿨타임을 순서대로 표시합니다.")]
    public UnityEngine.UI.Image Duration;

    public Image Cool;
    [Header("스킬 단축키 · None이면 사용 안 함")]
    public Key shortcutKey = Key.None;
    public TMP_Text shortcutText;

}
