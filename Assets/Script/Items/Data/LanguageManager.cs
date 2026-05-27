using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager instance;
    public language selectedLan = language.english;
    private void Awake()
    {
        if (instance == null) { instance = this; }
    }
}
