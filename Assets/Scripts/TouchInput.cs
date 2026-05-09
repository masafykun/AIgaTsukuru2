using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TouchInput Instance { get; private set; }

    public RectTransform knob;
    public float maxRadius = 80f;

    public Vector2 Direction { get; private set; }

    void Awake() => Instance = this;

    public void OnPointerDown(PointerEventData e) => Move(e);
    public void OnDrag(PointerEventData e)        => Move(e);

    public void OnPointerUp(PointerEventData e)
    {
        Direction = Vector2.zero;
        if (knob) knob.localPosition = Vector2.zero;
    }

    void Move(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, e.position, e.pressEventCamera, out var local);
        var clamped = Vector2.ClampMagnitude(local, maxRadius);
        if (knob) knob.localPosition = clamped;
        Direction = clamped / maxRadius;
    }
}
