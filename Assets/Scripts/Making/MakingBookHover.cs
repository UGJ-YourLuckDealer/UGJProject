using UnityEngine;
using UnityEngine.EventSystems;

public class MakingBookHover : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler
{
    [Header("Move Settings")]
    public float moveAmount = 20f;
    public float moveSpeed = 10f;

    private RectTransform rectTransform;

    private Vector2 originalPosition;
    private Vector2 targetPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        originalPosition = rectTransform.anchoredPosition;
        targetPosition = originalPosition;
    }

    private void Update()
    {
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition,
            targetPosition, Time.deltaTime * moveSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = originalPosition + Vector2.up * moveAmount;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = originalPosition;
    }
}
