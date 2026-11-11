using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 하루 시계 (임시 UI, 프로토타입 v0.0.3 "시계 처리"). 휴식 타이머가 없으므로 손님 처리 비율로 진행한다.
/// 하루 손님 N명 중 처리한 수 / N 만큼 시계 안쪽(Timer_Inside)이 시계 방향으로 채워진다 (예: 3명 → 1명마다 1/3).
/// GameSystems 프리팹(하루 동안 유지)에 붙고, UI는 실행 중에 만든다. Counter·Making에서만 보인다.
/// </summary>
[RequireComponent(typeof(GameFlowManager))]
public class DayClock : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Sprite faceSprite;   // Timer
    [SerializeField] private Sprite fillSprite;   // Timer_Inside

    [Header("Layout (1920x1080 기준, 화면 왼쪽 위)")]
    [SerializeField] private Vector2 position = new Vector2(100f, -100f);
    [SerializeField] private float size = 140f;
    [Tooltip("시계 안쪽이 바깥 테두리 안에 들어오도록 줄이는 비율")]
    [SerializeField, Range(0.5f, 1f)] private float fillScale = 0.8f;

    [Header("Motion")]
    [Tooltip("진행이 바뀔 때 채워지는 속도 (초당 비율)")]
    [SerializeField, Min(0.1f)] private float fillSpeed = 0.6f;

    private GameFlowManager flow;
    private GameObject canvasObject;
    private Image fill;
    private float shown;

    /// <summary>오늘 진행 비율 0~1 (처리한 손님 / 하루 손님 수).</summary>
    public float Progress => flow != null && flow.CustomersPerGame > 0
        ? Mathf.Clamp01((float)flow.CustomersFinished / flow.CustomersPerGame)
        : 0f;

    private void Awake()
    {
        flow = GetComponent<GameFlowManager>();
        Build();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start()
    {
        shown = Progress;
        Refresh(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) Refresh(scene);
    }

    private void Refresh(Scene scene)
    {
        bool inGame = scene.name == SceneFlow.CounterScene || scene.name == SceneFlow.MakingScene;
        if (canvasObject != null) canvasObject.SetActive(inGame);
    }

    private void Update()
    {
        if (fill == null) return;
        shown = Mathf.MoveTowards(shown, Progress, fillSpeed * Time.unscaledDeltaTime);
        fill.fillAmount = shown;
    }

    private void Build()
    {
        canvasObject = new GameObject("DayClockCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55; // 게임 UI 위, 툴팁(70)·설정 아래
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var face = NewImage("Clock", canvasObject.transform, faceSprite);
        var rect = face.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);

        fill = NewImage("Progress", face.transform, fillSprite);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;
        var fillRect = fill.rectTransform;
        fillRect.anchorMin = fillRect.anchorMax = fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = new Vector2(size * fillScale, size * fillScale);
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false; // 클릭을 막지 않음
        return image;
    }
}
