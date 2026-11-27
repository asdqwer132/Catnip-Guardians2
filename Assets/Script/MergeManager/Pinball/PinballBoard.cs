using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent, DefaultExecutionOrder(220)]
public sealed class PinballBoard : MonoBehaviour
{
    [SerializeField] private MergeWallUIFitter wallFitter;
    [SerializeField] private PinballBoardLayout layout;
    [Tooltip("Pin used in an unequipped cell. Clear this to leave empty cells open.")]
    [SerializeField] private BoardPinData defaultPin;
    [SerializeField] private PhysicsMaterial2D surfaceMaterial;
    [Header("Board proportions")]
    [SerializeField, Range(.1f, .65f)] private float outletWidth = .25f;
    [SerializeField, Range(.04f, .4f)] private float rampRise = .17f;
    [SerializeField, Range(.1f, 1f)] private float pinSize = .7f;
    [SerializeField, Range(.01f, .12f)] private float maximumItemRadius = .038f;
    [Header("Preview")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color rampColor = new Color(.55f, .37f, .20f);
    [SerializeField] private Color outletColor = new Color(.34f, .64f, .43f);

    private struct Pin
    {
        public PinballPin behaviour;
        public CircleCollider2D collider;
        public SpriteRenderer renderer;
    }

    private readonly List<Pin> pins = new List<Pin>();
    private readonly List<(MergeItemObject piece, int life, Vector2 center, float radius)> merges = new();
    private MergegameManager board;
    private Transform geometry;
    private BoxCollider2D leftRamp, rightRamp, outlet;
    private Sprite strip;
    private BoxCollider2D previousFloor;
    private SpriteRenderer previousFloorVisual;
    private bool floorWasEnabled, floorWasVisible, lineWasVisible;
    private Vector3 worldOrigin, worldWidth, worldHeight;
    private bool geometryReady;
    private bool fitDirty;

    private void OnValidate() => fitDirty = true;

    public bool IsConfigured => isActiveAndEnabled && layout != null && layout.PinCells.Count > 0 &&
        wallFitter != null && wallFitter.frame != null && wallFitter.box != null;
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

        if (geometry == null) CreateGeometry();
        for (int i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            var definition = i < (equipment?.Count ?? 0) ? equipment[i] : null;
            if (definition == null) definition = defaultPin;
            pin.behaviour.Bind(this, definition);
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

    private void OnDestroy()
    {
        if (geometry != null) Destroy(geometry.gameObject);
        if (strip != null) Destroy(strip);
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

    private void CreateGeometry()
    {
        geometry = new GameObject("Pinball Physics").transform;
        geometry.SetParent(wallFitter.box.transform, false);
        geometry.gameObject.SetActive(false);
        Texture2D texture = Texture2D.whiteTexture;
        strip = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), texture.width, 0, SpriteMeshType.FullRect);

        for (int i = 0; i < layout.PinCells.Count; i++)
        {
            GameObject cell = CreatePart($"Pin {i + 1}");
            var collider = cell.AddComponent<CircleCollider2D>();
            collider.sharedMaterial = surfaceMaterial;
            GameObject visual = new GameObject("Icon");
            visual.transform.SetParent(cell.transform, false);
            pins.Add(new Pin { behaviour = cell.AddComponent<PinballPin>(), collider = collider, renderer = AddRenderer(visual) });
        }
        leftRamp = CreateRamp("Left ramp");
        rightRamp = CreateRamp("Right ramp");
        GameObject exit = CreatePart("Battle outlet");
        exit.AddComponent<PinballOutlet>().Board = this;
        outlet = exit.AddComponent<BoxCollider2D>();
        outlet.isTrigger = true;
        var marker = CreatePart("Outlet marker");
        marker.transform.SetParent(exit.transform, false);
        var renderer = AddRenderer(marker);
        renderer.sprite = strip;
        renderer.color = outletColor;
    }

    private GameObject CreatePart(string label)
    {
        var part = new GameObject(label);
        part.transform.SetParent(geometry, false);
        return part;
    }

    private static SpriteRenderer AddRenderer(GameObject target)
    {
        var renderer = target.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "GameItemWorld";
        renderer.sortingOrder = -40;
        return renderer;
    }

    private BoxCollider2D CreateRamp(string label)
    {
        var part = CreatePart(label);
        var collider = part.AddComponent<BoxCollider2D>();
        collider.sharedMaterial = surfaceMaterial;
        var renderer = AddRenderer(part);
        renderer.sprite = strip;
        renderer.color = rampColor;
        return collider;
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
            pin.behaviour.transform.position = At(origin, width, height, cell.center);
            Vector3 lossyScale = pin.behaviour.transform.lossyScale;
            pin.collider.radius = diameter * .5f / Mathf.Max(.0001f, Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
            if (pin.renderer.sprite == null) continue;
            var size = pin.renderer.sprite.bounds.size;
            float scale = diameter / Mathf.Max(size.x, size.y);
            pin.renderer.transform.localScale = new Vector3(scale / Mathf.Abs(lossyScale.x),
                scale / Mathf.Abs(lossyScale.y), 1f);
        }

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
        if (!showGizmos || !TryGetFrame(out var origin, out var width, out var height)) return;
        Color previous = Gizmos.color;
        Gizmos.color = Color.cyan;
        foreach (var cell in layout.PinCells)
            Gizmos.DrawWireSphere(At(origin, width, height, cell.center), GetDiameter(cell, width, height) * .5f);
        GetRamps(origin, width, height, out var left, out var innerLeft, out var innerRight, out var right);
        Gizmos.color = new Color(1f, .68f, .25f);
        Gizmos.DrawLine(left, innerLeft);
        Gizmos.DrawLine(innerRight, right);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(innerLeft, innerRight);
        Gizmos.color = previous;
    }
}
