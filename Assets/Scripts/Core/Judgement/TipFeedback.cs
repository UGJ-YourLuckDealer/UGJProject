using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 판정 결과 피드백 (임시 UI). 기획대로 팁 유무만 표시한다 (정답 공식 맞추기로 변질되지 않도록 단계는 숨김).
/// GameSystems 프리팹(씬 전환 후에도 유지)에 붙어 있어, 제출 직후 Counter로 넘어가는 동안에도 보인다.
/// feedbackText를 비워 두면 실행 중에 화면 Fade(ScreenFader)보다 위에 임시 텍스트를 만든다.
/// </summary>
public class TipFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private string tipMessage = "Tip!";
    [SerializeField] private string noTipMessage = "No Tip";
    [SerializeField, Min(0f)] private float showSeconds = 2f;
    [SerializeField] private float fontSize = 96f;
    [SerializeField] private Vector2 textPosition = new Vector2(0f, 200f); // 화면 중앙 기준 (1920x1080)

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (feedbackText == null) feedbackText = CreateFeedbackText();
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    private TMP_Text CreateFeedbackText()
    {
        var canvasObject = new GameObject("TipFeedbackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100; // ScreenFader(1000)보다 위: 씬 전환 Fade 중에도 보이게
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var textObject = new GameObject("TipText", typeof(RectTransform));
        textObject.transform.SetParent(canvasObject.transform, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, 200f);
        rect.anchoredPosition = textPosition;
        return text;
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
