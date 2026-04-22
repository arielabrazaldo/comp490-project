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

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas        canvas;
    private Vector2       dragOffset;

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
        if (parentRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, GetCamera(eventData), out Vector2 localPoint);
        dragOffset = rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, GetCamera(eventData), out Vector2 localPoint);
        rectTransform.anchoredPosition = localPoint + dragOffset;
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
