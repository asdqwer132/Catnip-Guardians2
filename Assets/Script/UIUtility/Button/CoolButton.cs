using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class CoolButton : SpriteButton
{

    public Image Cool;
    [Header("스킬 단축키 · None이면 사용 안 함")]
    public Key shortcutKey = Key.None;
    public TMP_Text shortcutText;

}
