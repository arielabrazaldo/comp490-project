using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// Attach to a tile slot in the Board Editor canvas to make it draggable.
/// Calls OnPositionChanged with the tile index and new anchored position when the drag ends.
/// </summary>
public class TileDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int              TileIndex         { get; set; }
    public Action<int, Vector2> OnPositionChanged { get; set; }

    /// <summary>
    /// True if the pointer moved more than DragThreshold pixels since the last
    /// BeginDrag. Read in the tile's Button.onClick to suppress accidental opens.
    /// Automatically reset on the next BeginDrag.
    /// </summary>
    public bool WasDragged { get; set; }
    private const float DragThreshold = 5f;

    private static readonly Vector2 CanvasSize = new Vector2(1575f, 950f);

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas        canvas;
    private Vector2       dragOffset;
    private Vector2       dragStartScreenPos;

    public void Initialize(int index, Canvas parentCanvas, Action<int, Vector2> onPositionChanged)
    {
        TileIndex         = index;
        canvas            = parentCanvas;
        OnPositionChanged = onPositionChanged;
        rectTransform     = GetComponent<RectTransform>();
        parentRect        = rectTransform.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        WasDragged       = false;
        dragStartScreenPos = eventData.position;

        if (parentRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, GetCamera(eventData), out Vector2 localPoint);
        dragOffset = rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Vector2.Distance(eventData.position, dragStartScreenPos) > DragThreshold)
            WasDragged = true;

        if (parentRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, GetCamera(eventData), out Vector2 localPoint);

        Vector2 halfTile   = rectTransform.sizeDelta * 0.5f;
        Vector2 halfCanvas = CanvasSize * 0.5f;
        Vector2 minPos     = -halfCanvas + halfTile;
        Vector2 maxPos     =  halfCanvas - halfTile;

        Vector2 desired = localPoint + dragOffset;
        rectTransform.anchoredPosition = new Vector2(
            Mathf.Clamp(desired.x, minPos.x, maxPos.x),
            Mathf.Clamp(desired.y, minPos.y, maxPos.y));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnPositionChanged?.Invoke(TileIndex, rectTransform.anchoredPosition);
    }

    private Camera GetCamera(PointerEventData eventData)
    {
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return canvas.worldCamera;
        return null;
    }
}
