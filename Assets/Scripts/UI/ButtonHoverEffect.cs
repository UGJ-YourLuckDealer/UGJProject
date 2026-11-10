using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float scaleSpeed = 10f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    // 버튼이 꺼질 때(창 닫기·씬 전환) 커진 크기가 남지 않도록 원래 크기로 되돌린다
    private void OnDisable()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        if (rectTransform != null) rectTransform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ChangeScale(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ChangeScale(originalScale);
    }

    private void ChangeScale(Vector3 targetScale)
    {
        if (!isActiveAndEnabled) return;

        if(scaleCoroutine!=null)
        {
            StopCoroutine(scaleCoroutine);
        }

        scaleCoroutine = StartCoroutine(ScaleAnimation(targetScale));
    }

    private IEnumerator ScaleAnimation(Vector3 targetScale)
    {
        while(Vector3.Distance(rectTransform.localScale, targetScale) > 0.001f)
        {
            // 설정 창 일시정지(timeScale = 0) 중에도 동작하도록 unscaledDeltaTime 사용
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);

            yield return null;
        }

        rectTransform.localScale = targetScale;
        scaleCoroutine = null;
    }

}
