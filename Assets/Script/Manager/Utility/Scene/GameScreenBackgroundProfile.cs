using UnityEngine;

[CreateAssetMenu(menuName = "00.Game/Screen Background Profile", fileName = "GameScreenBackground")]
public sealed class GameScreenBackgroundProfile : ScriptableObject
{
    public Texture2D backgroundTexture;
    public Color tint = Color.white;
    public Color emptyColor = new Color(.06f, .06f, .06f, 1);
    public ScreenBackgroundFit fit = ScreenBackgroundFit.Cover;
    public Vector2 tileCount = new Vector2(12, 8);
    [Min(1)] public float referenceHeight = 1440;
}
