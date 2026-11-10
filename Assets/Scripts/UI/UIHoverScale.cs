using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// hermit UI 버튼 호버 확대 (설정·Quit 프리팹용). 일시정지 중에도 동작하고, 꺼질 때 원래 크기로 되돌린다.
/// </summary>
public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float scaleSpeed = 10f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine routine;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        originalScale = rectTransform.localScale;
    }

    private void OnDisable()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (rectTransform != null) rectTransform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => ScaleTo(originalScale * hoverScale);
    public void OnPointerExit(PointerEventData eventData) => ScaleTo(originalScale);

    private void ScaleTo(Vector3 target)
    {
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Animate(target));
    }

    private IEnumerator Animate(Vector3 target)
    {
        while (Vector3.Distance(rectTransform.localScale, target) > 0.001f)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, target, Time.unscaledDeltaTime * scaleSpeed);
            yield return null;
        }
        rectTransform.localScale = target;
        routine = null;
    }
}
