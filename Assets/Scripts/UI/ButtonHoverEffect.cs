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
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * scaleSpeed);

            yield return null;
        }

        rectTransform.localScale = targetScale;
        scaleCoroutine = null;
    }

}
