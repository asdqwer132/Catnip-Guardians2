using UnityEngine;
using UnityEngine.InputSystem;

public class UIParallaxByMouse : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public RectTransform layer;
        public float moveAmount = 10f;
    }

    [Header("Layers")]
    public ParallaxLayer[] layers;

    [Header("Setting")]
    public bool useYMovement = false;
    public float smoothSpeed = 8f;

    private Vector2[] startPositions;

    private void Awake()
    {
        startPositions = new Vector2[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layer != null)
                startPositions[i] = layers[i].layer.anchoredPosition;
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        float xPercent = (mousePos.x / Screen.width - 0.5f) * 2f;
        float yPercent = (mousePos.y / Screen.height - 0.5f) * 2f;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layer == null)
                continue;

            Vector2 targetPos = startPositions[i];

            targetPos.x += xPercent * layers[i].moveAmount;

            if (useYMovement)
                targetPos.y += yPercent * layers[i].moveAmount;

            layers[i].layer.anchoredPosition = Vector2.Lerp(
                layers[i].layer.anchoredPosition,
                targetPos,
                Time.unscaledDeltaTime * smoothSpeed
            );
        }
    }
}