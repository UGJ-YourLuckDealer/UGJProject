using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 하루 시작 날짜 표시 (v0.0.3). 하루 첫 손님 전에 한 번만 화면 가운데에 "Day N"을 띄우고(GameEvents.DayStarted),
/// 날짜 표시가 끝나면 손님 방문 시퀀스 시작 신호(CustomerVisit / GameEvents.CustomerVisitReady)를 보낸다.
/// 이후 손님은 Counter 화면 Fade In이 끝나면 바로 신호를 보낸다.
/// showDayLabel을 켜면 Counter·Making 왼쪽 위에 "Day N"을 계속 표시한다 (기본 꺼짐).
/// GameSystems 프리팹에 붙고, UI는 실행 중에 만든다 (CapitalUI 위, 설정 UI 아래).
/// </summary>
public class DayDisplay : MonoBehaviour
{
    [SerializeField] private string dayFormat = "Day {0}";
    [Tooltip("Counter·Making 왼쪽 위에 Day N을 계속 표시 (기획: 날짜는 첫 손님 전에 한 번만 표시하므로 기본 꺼짐)")]
    [SerializeField] private bool showDayLabel = false;
    [SerializeField] private float labelFontSize = 56f;
    [SerializeField] private Vector2 labelPosition = new Vector2(40f, -30f); // 왼쪽 위 기준 (1920x1080)
    [SerializeField] private float bannerFontSize = 160f;

    [Header("Style (크레딧 화면과 같은 스타일: Mochiy Pop One, 노란 글씨 + 폰트 머티리얼의 갈색 외곽선)")]
    [Tooltip("비우면 TMP 기본 폰트 + 검정 외곽선")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color textColor = new Color(1f, 0.85f, 0.45f, 1f);
    [Tooltip("Counter 화면 Fade In이 끝난 뒤 날짜를 띄우기까지 기다리는 시간")]
    [SerializeField, Min(0f)] private float bannerDelay = 0.1f;
    [SerializeField, Min(0.1f)] private float bannerSeconds = 1.5f;

    private TMP_Text label;
    private TMP_Text banner;
    private int bannerShownDay;
    private int refreshedSceneHandle; // 같은 씬에 Start와 sceneLoaded가 둘 다 와도 한 번만 처리

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
        text.color = textColor;
        if (font != null)
        {
            text.font = font; // 외곽선은 폰트 기본 머티리얼(갈색 외곽선)을 그대로 쓴다
        }
        else
        {
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
        }
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
        if (scene.handle == refreshedSceneHandle) return;
        refreshedSceneHandle = scene.handle;

        bool inGame = scene.name == SceneFlow.CounterScene || scene.name == SceneFlow.MakingScene;
        string dayText = string.Format(dayFormat, DayCycle.CurrentDay);
        label.text = dayText;
        label.gameObject.SetActive(inGame && showDayLabel);

        if (scene.name != SceneFlow.CounterScene) return;

        // Counter에 들어올 때마다 방문 준비 상태를 새로 시작
        CustomerVisit.Reset();
        bool dayStart = SceneFlow.NextCustomerIndex == 0 && bannerShownDay != DayCycle.CurrentDay; // 하루 첫 손님
        if (dayStart) bannerShownDay = DayCycle.CurrentDay;
        StopAllCoroutines();
        banner.gameObject.SetActive(false);
        StartCoroutine(CounterIntro(dayStart, dayText));
    }

    private IEnumerator CounterIntro(bool dayStart, string dayText)
    {
        yield return null; // 씬 오브젝트의 Start(구독)가 먼저 실행되도록 한 프레임 대기
        while (ScreenFader.IsBusy) yield return null; // Counter 화면 Fade In이 끝날 때까지

        if (dayStart)
        {
            GameEvents.RaiseDayStarted(DayCycle.CurrentDay);
            yield return ShowBanner(dayText);
        }

        var dialogue = FindAnyObjectByType<DialogueManager>();
        CustomerVisit.MarkReady(dialogue != null ? dialogue.customer : SceneFlow.NextCustomerIndex);
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
