using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 판정 결과 피드백 (임시 UI). 기획대로 팁 유무만 표시한다 (정답 공식 맞추기로 변질되지 않도록 단계는 숨김).
/// </summary>
public class TipFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private string tipMessage = "Tip!";
    [SerializeField] private string noTipMessage = "No Tip";
    [SerializeField, Min(0f)] private float showSeconds = 2f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    private void OnEnable() => GameEvents.KeyringJudged += OnJudged;

    private void OnDisable()
    {
        GameEvents.KeyringJudged -= OnJudged;
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    private void OnJudged(CustomerData customer, JudgementResult result)
    {
        if (feedbackText == null) return;
        feedbackText.text = result.HasTip ? tipMessage : noTipMessage;
        feedbackText.gameObject.SetActive(true);
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        if (showSeconds > 0f) hideRoutine = StartCoroutine(HideAfter(showSeconds));
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        feedbackText.gameObject.SetActive(false);
        hideRoutine = null;
    }
}
