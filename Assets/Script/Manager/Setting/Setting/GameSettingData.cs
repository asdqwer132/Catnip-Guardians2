using System;
using UnityEngine;

[Serializable]
public class GameSettingData
{
    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Cursor")]
    [Range(0.5f, 3f)] public float cursorScale = 1f;

    [Header("Indicator")]
    [Range(0, 2)] public int indicatorSpriteSize = 1;

    [Header("Display")]
    public bool fullscreen = true;
    public int resolutionIndex = 0;

    [Header("Gameplay")]
    public bool showDamagePopup = true;
    public bool screenShake = true;
    public bool showHealthBar = true;
}