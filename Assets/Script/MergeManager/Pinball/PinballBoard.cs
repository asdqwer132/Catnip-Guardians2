using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent, DefaultExecutionOrder(220)]
public sealed class PinballBoard : MonoBehaviour
{
    [SerializeField] private MergeWallUIFitter wallFitter;
    [SerializeField] private PinballBoardLayout layout;
    [Tooltip("Pin used in an unequipped cell. Clear this to leave empty cells open.")]
    [SerializeField] private BoardPinData defaultPin;
    public const int InletCount = 5;
    [Header("Inlets (left to right)")]
    [SerializeField] private Vector2[] inletPositions =
    {
        new Vector2(.1f, .9f), new Vector2(.3f, .9f), new Vector2(.5f, .9f),
        new Vector2(.7f, .9f), new Vector2(.9f, .9f)
    };

    [Header("Board proportions")]
    [SerializeField, Range(.1f, .65f)] private float outletWidth = .25f;
    [SerializeField, Range(.04f, .4f)] private float rampRise = .17f;
    [SerializeField, Range(.1f, 1f)] private float pinSize = .7f;
    [SerializeField, Range(.01f, .12f)] private float maximumItemRadius = .038f;
    [Header("Preview")]
    [SerializeField] private bool showGizmos = true;

    private struct Pin
    {
        public PinballPin behaviour;
        public CircleCollider2D collider;
        public SpriteRenderer renderer;
        public PinballPin replacementSource;
    }

    private readonly List<Pin> pins = new List<Pin>();
    private readonly List<(MergeItemObject piece, int life, Vector2 center, float radius)> merges = new();
    private MergegameManager board;
    [Header("Scene Geometry")]
    [SerializeField] private Transform geometry;
    [SerializeField] private bool fitToUI = true;
    [SerializeField] private BoxCollider2D leftRamp, rightRamp, outlet;
    [SerializeField] private PinballPin[] scenePins;
    private BoxCollider2D previousFloor;
    private SpriteRenderer previousFloorVisual;
    private bool floorWasEnabled, floorWasVisible, lineWasVisible;
    private Vector3 worldOrigin, worldWidth, worldHeight;
    private bool geometryReady;
    private bool fitDirty;

    private void OnValidate() => fitDirty = true;

    public bool IsConfigured => isActiveAndEnabled && layout != null && layout.PinCells.Count > 0 && geometry != null && scenePins != null &&
        scenePins.Length == layout.PinCells.Count && outlet != null && leftRamp != null && rightRamp != null &&
        wallFitter != null && wallFitter.frame != null && wallFitter.box != null;
    internal bool TryGetInletPosition(int index, out Vector3 position)
    {
        Vector2 normalized = index < inletPositions.Length ? inletPositions[index] :
            new Vector2((index + .5f) / InletCount, .9f);
        position = At(worldOrigin, worldWidth, worldHeight, normalized);
        return geometryReady;
    }

    internal bool Owns(MergeItemObject piece) => board != null && board.CanOperate &&
        piece != null && piece.IsLive && piece.Owner == board;

    internal void Bind(MergegameManager value, IReadOnlyList<BoardPinData> equipment)
    {
        merges.Clear();
        if (board != value)
        {
            Unbind();
            board = value;
            board.Pinball = this;
            foreach (var part in wallFitter.box.visuals)
            {
                if (part?.role != MergeBoxSpriteRole.BottomWall || part.wallCollider == null) continue;
                previousFloor = part.wallCollider;
                floorWasEnabled = previousFloor.enabled;
                previousFloorVisual = part.renderer;
                floorWasVisible = previousFloorVisual != null && previousFloorVisual.enabled;
                break;
            }
            lineWasVisible = board.dropZone.showWorldLine;
            board.dropZone.showWorldLine = false;
            board.dropZone.RefreshLine();
        }

        if (pins.Count == 0)
            foreach (var cell in scenePins)
                pins.Add(new Pin { behaviour = cell, collider = cell.GetComponent<CircleCollider2D>(),
                    renderer = cell.GetComponentInChildren<SpriteRenderer>(true) });
        outlet.GetComponent<PinballOutlet>().Board = this;
        for (int i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            var definition = i < (equipment?.Count ?? 0) ? equipment[i] : null;
            if (definition == null) definition = defaultPin;
            var replacement = definition != null ? definition.worldPrefab : null;
            if (replacement != null && (!replacement.TryGetComponent<CircleCollider2D>(out _) ||
                replacement.GetComponentInChildren<SpriteRenderer>(true) == null))
                replacement = null;
            if (replacement != pin.replacementSource)
            {
                var anchor = scenePins[i];
                if (pin.behaviour != anchor)
                {
                    pin.behaviour.gameObject.SetActive(false);
                    Destroy(pin.behaviour.gameObject);
                }
                var instance = replacement != null ? Instantiate(replacement, anchor.transform.parent) : anchor;
                if (instance != anchor)
                {
                    instance.transform.SetLocalPositionAndRotation(anchor.transform.localPosition, anchor.transform.localRotation);
                    instance.transform.localScale = anchor.transform.localScale;
                    anchor.gameObject.SetActive(false);
                }
                pin = new Pin { behaviour = instance, collider = instance.GetComponent<CircleCollider2D>(),
                    renderer = instance.GetComponentInChildren<SpriteRenderer>(true), replacementSource = replacement };
                pins[i] = pin;
            }
            pin.behaviour.Bind(this, definition);
            if (replacement == null || pin.renderer.sprite == null)
                pin.renderer.sprite = definition != null ? definition.boardIcon ?? definition.icon : null;
            pin.behaviour.gameObject.SetActive(definition != null);
        }
        geometryReady = false;
        Fit();
    }

    private void Unbind()
    {
        if (board != null)
        {
            board.Pinball = null;
            board.dropZone.showWorldLine = lineWasVisible;
            board.dropZone.RefreshLine();
        }
        RestoreFloor();
        previousFloor = null;
        previousFloorVisual = null;
        board = null;
        geometryReady = false;
        if (geometry != null) geometry.gameObject.SetActive(false);
    }

    private void RestoreFloor()
    {
        if (previousFloor != null) previousFloor.enabled = floorWasEnabled;
        if (previousFloorVisual != null) previousFloorVisual.enabled = floorWasVisible;
    }

    private void OnDisable()
    {
        merges.Clear();
        Unbind();
    }

    private void LateUpdate()
    {
        if (board == null) return;
        Fit();
        foreach (var request in merges)
            if (Owns(request.piece) && request.life == request.piece.LifeId)
                board.TryMergeAtPin(request.piece, request.center, request.radius);
        merges.Clear();
    }

    internal float FitItemRadius(float radius)
    {
        float width = geometryReady ? worldWidth.magnitude : 0f;
        return width > 0f ? Mathf.Min(radius, Mathf.Max(.01f, width * maximumItemRadius)) : radius;
    }

    internal void QueueMerge(MergeItemObject piece, Vector2 center, float radius)
    {
        if (Owns(piece)) merges.Add((piece, piece.LifeId, center, radius));
    }

    internal void TryExit(MergeItemObject piece)
    {
        if (!Owns(piece) || piece.IsHeld || !piece.CanSelect) return;
        if (piece is MineralMergeItemObject || piece is MonsterMergeItemObject)
        {
            piece.ReturnToPool();
            return;
        }

        var session = GameSession.Instance;
        if (session == null || piece.item == null) return;
        var transfer = new GameItemToBattleData();
        transfer.items.Add(new InventoryItem(piece.item, 1));

        if (board.RetainSelectedItems && board.spawnPoint != null)
        {
            Vector3 spawn = board.spawnPoint.position;
            piece.body.position = spawn;
            piece.transform.position = spawn;
            piece.Velocity = Vector2.zero;
            piece.body.angularVelocity = 0f;
        }
        else piece.ReturnToPool();

        session.SendItemsToBattle(transfer);
    }

    private void Fit()
    {
        if (!TryGetFrame(out var origin, out var width, out var height))
        {
            if (geometryReady)
            {
                geometry.gameObject.SetActive(false);
                RestoreFloor();
                geometryReady = false;
            }
            return;
        }

        int layer = board.gameObject.layer;
        bool layerChanged = geometry.gameObject.layer != layer || !geometryReady;
        if (!fitDirty && !layerChanged && origin == worldOrigin && width == worldWidth && height == worldHeight) return;
        worldOrigin = origin;
        worldWidth = width;
        worldHeight = height;
        geometryReady = true;
        fitDirty = false;
        if (layerChanged)
            foreach (var child in geometry.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;

        for (int i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            var cell = layout.PinCells[i];
            float diameter = GetDiameter(cell, width, height);
            if (fitToUI) pin.behaviour.transform.position = At(origin, width, height, cell.center);
            Vector3 lossyScale = pin.behaviour.transform.lossyScale;
            float worldScale = Mathf.Max(.0001f, Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
            if (fitToUI) pin.collider.radius = diameter * .5f / worldScale;
            else diameter = pin.collider.radius * 2f * worldScale;
            if (pin.renderer.sprite == null) continue;
            var size = pin.renderer.sprite.bounds.size;
            float scale = diameter / Mathf.Max(size.x, size.y);
            pin.renderer.transform.localScale = new Vector3(scale / Mathf.Abs(lossyScale.x),
                scale / Mathf.Abs(lossyScale.y), 1f);
        }

        if (fitToUI)
        {
            GetRamps(origin, width, height, out var left, out var innerLeft, out var innerRight, out var right);
            float thickness = Mathf.Max(.025f, width.magnitude * .018f);
            FitRamp(leftRamp, left, innerLeft, thickness);
            FitRamp(rightRamp, innerRight, right, thickness);
            outlet.transform.position = (innerLeft + innerRight) * .5f;
            outlet.transform.rotation = Quaternion.FromToRotation(Vector3.right, width);
            float depth = Mathf.Max(.35f, width.magnitude * .18f);
            outlet.size = new Vector2((innerRight - innerLeft).magnitude / Mathf.Abs(outlet.transform.lossyScale.x),
                depth / Mathf.Abs(outlet.transform.lossyScale.y));
            outlet.offset = Vector2.down * (outlet.size.y * .4f);
            Transform marker = outlet.transform.GetChild(0);
            marker.localPosition = Vector3.down * (thickness * 3f / Mathf.Abs(outlet.transform.lossyScale.y));
            marker.localScale = new Vector3(outlet.size.x, thickness / Mathf.Abs(outlet.transform.lossyScale.y), 1f);
        }

        if (previousFloor != null) previousFloor.enabled = false;
        if (previousFloorVisual != null) previousFloorVisual.enabled = false;
        geometry.gameObject.SetActive(true);
        Physics2D.SyncTransforms();
    }

    private static void FitRamp(BoxCollider2D ramp, Vector3 from, Vector3 to, float thickness)
    {
        Vector3 direction = to - from;
        ramp.transform.SetPositionAndRotation((from + to) * .5f, Quaternion.FromToRotation(Vector3.right, direction));
        Vector3 scale = ramp.transform.lossyScale;
        ramp.size = new Vector2(direction.magnitude / Mathf.Abs(scale.x), thickness / Mathf.Abs(scale.y));
        var renderer = ramp.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = ramp.size;
    }

    private bool TryGetFrame(out Vector3 origin, out Vector3 width, out Vector3 height)
    {
        origin = width = height = default;
        if (!IsConfigured || !wallFitter.TryProjectBoardPoint(Vector2.zero, out origin) ||
            !wallFitter.TryProjectBoardPoint(Vector2.right, out var right) ||
            !wallFitter.TryProjectBoardPoint(Vector2.up, out var top)) return false;
        width = right - origin;
        height = top - origin;
        return width.sqrMagnitude > .0001f && height.sqrMagnitude > .0001f;
    }

    private float GetDiameter(PinballBoardLayout.PinCell cell, Vector3 width, Vector3 height)
        => Mathf.Min(cell.size.x * width.magnitude, cell.size.y * height.magnitude) * pinSize;

    private static Vector3 At(Vector3 origin, Vector3 width, Vector3 height, Vector2 uv)
        => origin + uv.x * width + uv.y * height;

    private void GetRamps(Vector3 origin, Vector3 width, Vector3 height,
        out Vector3 left, out Vector3 innerLeft, out Vector3 innerRight, out Vector3 right)
    {
        left = At(origin, width, height, new Vector2(0f, rampRise));
        right = At(origin, width, height, new Vector2(1f, rampRise));
        innerLeft = At(origin, width, height, new Vector2(.5f - outletWidth * .5f, .035f));
        innerRight = At(origin, width, height, new Vector2(.5f + outletWidth * .5f, .035f));
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.color = Color.cyan;
        if (pins.Count > 0)
        {
            foreach (var pin in pins) DrawPinGizmo(pin.collider);
        }
        else if (scenePins != null)
        {
            foreach (var pin in scenePins)
                if (pin != null) DrawPinGizmo(pin.GetComponent<CircleCollider2D>());
        }

        Gizmos.color = new Color(1f, .68f, .25f);
        DrawBoxGizmo(leftRamp);
        DrawBoxGizmo(rightRamp);
        Gizmos.color = Color.green;
        DrawBoxGizmo(outlet);
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private static void DrawPinGizmo(CircleCollider2D collider)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) return;
        var scale = collider.transform.lossyScale;
        float radius = collider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireSphere(collider.transform.TransformPoint(collider.offset), radius);
    }

    private static void DrawBoxGizmo(BoxCollider2D collider)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) return;
        Gizmos.matrix = collider.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(collider.offset, new Vector3(collider.size.x, collider.size.y, 0f));
    }
}
