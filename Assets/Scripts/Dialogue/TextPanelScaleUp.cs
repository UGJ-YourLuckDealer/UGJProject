using System.Collections;
using UnityEngine;

public class TextPanelScaleUp : MonoBehaviour
{
    [SerializeField] private float startScale = 0.3f;
    [SerializeField] private float duration = 0.3f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    void Start()
    {
        transform.localScale = originalScale * startScale;
        
    }

    public void StartScaleUp()
    {
        StartCoroutine(ScaleUp());
    }

    // Update is called once per frame
    IEnumerator ScaleUp()
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localScale = Vector3.Lerp(originalScale * startScale, originalScale, t);

            yield return null;
        }

        transform.localScale = originalScale;
    }
}
