
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteCooldownController : MonoBehaviour
{
    public enum FillMode
    {
        Radial360 = 0,
        Horizontal = 1,
        Vertical = 2
    }

    [Header("Fill")]
    [SerializeField] private FillMode fillMode = FillMode.Radial360;

    [Range(0f, 1f)]
    [SerializeField] private float fillAmount = 1f;

    [Header("Radial Settings")]
    [SerializeField] private bool clockwise = true;

    [Range(0f, 360f)]
    [SerializeField] private float startAngle = 0f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock block;

    private static readonly int FillID =
        Shader.PropertyToID("_FillAmount");

    private static readonly int ModeID =
        Shader.PropertyToID("_FillMode");

    private static readonly int ClockwiseID =
        Shader.PropertyToID("_Clockwise");

    private static readonly int AngleID =
        Shader.PropertyToID("_StartAngle");

    private static readonly int BoundsMinID =
        Shader.PropertyToID("_BoundsMin");

    private static readonly int BoundsSizeID =
        Shader.PropertyToID("_BoundsSize");

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (block == null)
            block = new MaterialPropertyBlock();

        if (spriteRenderer.sprite == null)
            return;

        Bounds bounds = spriteRenderer.sprite.bounds;

        spriteRenderer.GetPropertyBlock(block);

        block.SetFloat(FillID, fillAmount);
        block.SetFloat(ModeID, (float)fillMode);
        block.SetFloat(ClockwiseID, clockwise ? 1f : 0f);
        block.SetFloat(AngleID, startAngle);

        block.SetVector(BoundsMinID, bounds.min);
        block.SetVector(BoundsSizeID, bounds.size);

        spriteRenderer.SetPropertyBlock(block);
    }

    public void SetFill(float value)
    {
        fillAmount = Mathf.Clamp01(value);
        Apply();
    }

    public void SetCooldown(float remaining, float total)
    {
        SetFill(total > 0f ? remaining / total : 0f);
    }

    public void SetMode(FillMode mode)
    {
        fillMode = mode;
        Apply();
    }
}
