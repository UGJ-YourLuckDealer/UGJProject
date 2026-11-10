using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 일차 표시 (임시 UI). Counter·Making에서 화면 왼쪽 위에 "Day N"을 보여 주고,
/// 하루가 시작될 때(Counter 첫 손님) 화면 가운데에 "Day N" 배너를 잠깐 띄운다.
/// GameSystems 프리팹에 붙고, UI는 실행 중에 만든다 (CapitalUI 위, 설정 UI 아래).
/// </summary>
public class DayDisplay : MonoBehaviour
{
    [SerializeField] private string dayFormat = "Day {0}";
    [SerializeField] private float labelFontSize = 56f;
    [SerializeField] private Vector2 labelPosition = new Vector2(40f, -30f); // 왼쪽 위 기준 (1920x1080)
    [SerializeField] private float bannerFontSize = 160f;
    [Tooltip("씬 Fade In이 끝난 뒤 배너를 띄우기까지 기다리는 시간")]
    [SerializeField, Min(0f)] private float bannerDelay = 0.6f;
    [SerializeField, Min(0.1f)] private float bannerSeconds = 1.5f;

    private TMP_Text label;
    private TMP_Text banner;
    private int bannerShownDay;

    private void Awake()
    {
        var canvasObject = new GameObject("DayDisplayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // CapitalUI(50) 위, SettingsUI(100) 아래
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        label = CreateText(canvasObject.transform, "DayLabel", labelFontSize, new Vector2(0f, 1f), TextAlignmentOptions.TopLeft);
        label.rectTransform.anchoredPosition = labelPosition;
        banner = CreateText(canvasObject.transform, "DayBanner", bannerFontSize, new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
        banner.rectTransform.sizeDelta = new Vector2(1200f, 300f);
        label.gameObject.SetActive(false);
        banner.gameObject.SetActive(false);
    }

    private TMP_Text CreateText(Transform parent, string name, float size, Vector2 anchor, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = new Vector2(500f, 120f);
        return text;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start() => Refresh(SceneManager.GetActiveScene());

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) Refresh(scene);
    }

    private void Refresh(Scene scene)
    {
        bool inGame = scene.name == SceneFlow.CounterScene || scene.name == SceneFlow.MakingScene;
        string dayText = string.Format(dayFormat, DayCycle.CurrentDay);
        label.text = dayText;
        label.gameObject.SetActive(inGame);

        // 하루 시작 = Counter에서 첫 손님 차례
        if (scene.name == SceneFlow.CounterScene && SceneFlow.NextCustomerIndex == 0 && bannerShownDay != DayCycle.CurrentDay)
        {
            bannerShownDay = DayCycle.CurrentDay;
            StopAllCoroutines();
            StartCoroutine(ShowBanner(dayText));
        }
    }

    private IEnumerator ShowBanner(string text)
    {
        banner.text = text;
        banner.alpha = 0f;
        banner.gameObject.SetActive(true);
        if (bannerDelay > 0f) yield return new WaitForSecondsRealtime(bannerDelay);

        const float fade = 0.3f;
        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime) { banner.alpha = t / fade; yield return null; }
        banner.alpha = 1f;
        yield return new WaitForSecondsRealtime(bannerSeconds);
        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime) { banner.alpha = 1f - t / fade; yield return null; }
        banner.gameObject.SetActive(false);
    }
}
