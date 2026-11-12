using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 Fade In/Out (v0.0.2). 게임 실행 시 첫 화면을 Fade In하고,
/// 씬 전환을 Fade Out → 로드 → Fade In으로 처리한다. 전환 중에는 클릭을 막는다.
/// UI는 Resources/ScreenFader.prefab (색·시간은 프리팹 Inspector에서 조정).
/// Counter → Making은 Fade 대신 Making 화면(책상)이 아래에서 올라오는 전환(LoadSceneRising)을 쓴다.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private const string PrefabPath = "ScreenFader"; // Resources/ScreenFader.prefab

    [SerializeField] private CanvasGroup group;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.35f;
    [Tooltip("완전히 어두워진 상태로 잠깐 머무는 시간 (전환이 끊겨 보이지 않게)")]
    [SerializeField, Min(0f)] private float holdBlackSeconds = 0.1f;

    [Header("책상이 올라오는 전환 (Counter → Making)")]
    [Tooltip("다음 씬 화면이 아래에서 올라오는 시간")]
    [SerializeField, Min(0.05f)] private float riseSeconds = 0.55f;
    [Tooltip("다 올라온 뒤 캡처 화면을 걷는 시간 (Overlay UI만 살짝 나타남)")]
    [SerializeField, Min(0f)] private float revealSeconds = 0.1f;
    [Tooltip("전환 Canvas 정렬 순서 (페이더 1000보다 위)")]
    [SerializeField] private int coverSortingOrder = 1001;

    private Canvas coverCanvas;
    private RawImage fromShot; // 클릭 순간의 현재 씬 캡처 (가만히 있음)
    private RawImage toShot;   // 다음 씬 카메라가 그린 실제 화면 (아래에서 올라옴)

    private bool busy;

    // 유지 중인 페이더는 GameSession.Fader에 저장한다 (static 값·초기화는 GameSession에서 관리).
    private static ScreenFader instance
    {
        get => GameSession.Fader;
        set => GameSession.Fader = value;
    }

    public static bool IsBusy => instance != null && instance.busy;

    public static ScreenFader Instance
    {
        get
        {
            if (instance == null)
            {
                var prefab = Resources.Load<ScreenFader>(PrefabPath);
                if (prefab == null) return null;
                instance = Instantiate(prefab);
                instance.name = "ScreenFader";
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

    /// <summary>static 이벤트 연결 해제 (GameSession.ResetAll이 Play 시작마다 호출).</summary>
    internal static void ClearStaticHooks() => SceneManager.sceneLoaded -= OnAnySceneLoaded;

    /// <summary>게임 실행 시 첫 화면을 검은 화면에서 Fade In (GameSession이 첫 씬 로드 후 호출).</summary>
    internal static void FadeInOnLaunch()
    {
        var f = Instance;
        if (f == null) return;
        f.SetAlpha(1f);
        f.StartCoroutine(f.Fade(0f, f.fadeInSeconds));
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    // SceneFlow를 거치지 않고 바뀐 씬(팀원 코드의 직접 로드)도 Fade In으로 부드럽게 보여 준다
    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || instance == null || instance.busy) return;
        instance.StopAllCoroutines();
        instance.SetAlpha(1f);
        instance.StartCoroutine(instance.Fade(0f, instance.fadeInSeconds));
    }

    /// <summary>Fade Out → 씬 로드 → Fade In. 페이더가 없으면 바로 로드한다.</summary>
    public static void LoadScene(string sceneName)
    {
        var f = Instance;
        if (f == null) { SceneManager.LoadScene(sceneName); return; }
        if (f.busy) return; // 전환 중 중복 요청 무시
        f.StartCoroutine(f.FadeAndLoad(sceneName));
    }

    /// <summary>
    /// 현재 화면을 멈춘 채 다음 씬을 로드하고, 다음 씬 화면이 아래에서 올라오며 그대로 다음 씬이 된다.
    /// </summary>
    public static void LoadSceneRising(string sceneName)
    {
        var f = Instance;
        if (f == null) { SceneManager.LoadScene(sceneName); return; }
        if (f.busy) return;
        f.StartCoroutine(f.RiseAndLoad(sceneName));
    }

    // 1) 지금 화면을 캡처해 그대로 띄워 둠 → 2) 그 뒤에서 다음 씬을 실제로 로드 →
    // 3) 다음 씬 카메라 화면을 텍스처로 받아 아래에서 올림 → 4) 다 올라오면 캡처를 걷음.
    // 마지막에 보이는 그림이 다음 씬 자체라 넘어가는 순간 위치·색 차이가 생기지 않는다.
    private IEnumerator RiseAndLoad(string sceneName)
    {
        busy = true;
        EnsureCover();

        yield return new WaitForEndOfFrame();
        var fromTex = CaptureScreenAsSrgb();
        fromShot.texture = fromTex;
        fromShot.enabled = true;
        toShot.enabled = false;
        coverCanvas.gameObject.SetActive(true); // 이 순간부터 클릭도 막힘 (RawImage raycast)

        Time.timeScale = 1f; // 일시정지 상태로 전환되지 않게
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;
        yield return null; // 다음 씬 Start·레이아웃이 끝난 뒤
        yield return new WaitForEndOfFrame();

        var toTex = RenderSceneCamera();
        if (toTex != null)
        {
            toShot.texture = toTex;
            toShot.enabled = true;
            var rect = toShot.rectTransform;
            float height = ((RectTransform)coverCanvas.transform).rect.height;
            Vector2 hidden = new Vector2(0f, -height);
            rect.anchoredPosition = hidden;
            for (float t = 0f; t < riseSeconds; t += Mathf.Min(Time.unscaledDeltaTime, MaxStep))
            {
                rect.anchoredPosition = Vector2.LerpUnclamped(hidden, Vector2.zero, EaseOutCubic(t / riseSeconds));
                yield return null;
            }
            rect.anchoredPosition = Vector2.zero;
            fromShot.enabled = false;
        }

        // 다 올라온 화면은 실제 씬과 같으므로 걷어도 그대로. 클릭은 바로 통과시키고 Overlay UI만 짧게 나타나게 한다
        var group = coverCanvas.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        for (float t = 0f; t < revealSeconds; t += Mathf.Min(Time.unscaledDeltaTime, MaxStep))
        {
            group.alpha = 1f - t / revealSeconds;
            yield return null;
        }

        coverCanvas.gameObject.SetActive(false);
        group.alpha = 1f;
        group.blocksRaycasts = true;
        fromShot.texture = null;
        toShot.texture = null;
        Destroy(fromTex);
        if (toTex != null) { toTex.Release(); Destroy(toTex); }
        busy = false;
    }

    // 화면 캡처. 캡처 텍스처는 Linear로 표시돼 프로젝트(Linear 색 공간)에서 실제보다 밝게 보이므로,
    // 같은 픽셀을 sRGB 텍스처에 옮겨 화면과 같은 밝기로 보이게 한다
    private static Texture2D CaptureScreenAsSrgb()
    {
        var raw = ScreenCapture.CaptureScreenshotAsTexture();
        if (QualitySettings.activeColorSpace != ColorSpace.Linear) return raw;
        var srgb = new Texture2D(raw.width, raw.height, raw.format, false, false);
        srgb.LoadRawTextureData(raw.GetRawTextureData<byte>());
        srgb.Apply(false, true); // GPU로 올린 뒤 CPU 사본은 버림
        Destroy(raw);
        return srgb;
    }

    // 지금 활성 씬의 Main Camera 화면을 텍스처로 그린다 (실패하면 null → 캡처를 바로 걷는다)
    private static RenderTexture RenderSceneCamera()
    {
        var cam = Camera.main;
        if (cam == null) return null;
        var rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.Create();
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            RenderPipeline.SubmitRenderRequest(cam, request);
        }
        else
        {
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;
        }
        return rt;
    }

    private const float MaxStep = 1f / 30f;

    private static float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    // 전환용 Canvas는 페이더 계층과 분리된 별도 Overlay Canvas (페이더 투명도의 영향을 받지 않고 페이더보다 위)
    private void EnsureCover()
    {
        if (coverCanvas != null) return;
        var root = new GameObject("RisingCoverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root); // 씬이 바뀌어도 유지
        coverCanvas = root.GetComponent<Canvas>();
        coverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        coverCanvas.sortingOrder = coverSortingOrder;
        fromShot = CreateFullScreenImage("FromShot", root.transform);
        toShot = CreateFullScreenImage("ToShot", root.transform);
        root.SetActive(false);
    }

    private static RawImage CreateFullScreenImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<RawImage>();
        image.raycastTarget = true;
        return image;
    }

    private void OnDestroy()
    {
        if (coverCanvas != null) Destroy(coverCanvas.gameObject);
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        busy = true;
        yield return Fade(1f, fadeOutSeconds);
        Time.timeScale = 1f; // 일시정지 상태로 전환되지 않게
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;
        if (holdBlackSeconds > 0f) yield return new WaitForSecondsRealtime(holdBlackSeconds);
        yield return Fade(0f, fadeInSeconds);
        busy = false;
    }

    private IEnumerator Fade(float target, float seconds)
    {
        group.blocksRaycasts = true;
        float start = group.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            // SmoothStep: 천천히 시작해 천천히 끝나 자연스럽게 어두워지고 밝아진다
            SetAlpha(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds)));
            yield return null;
        }
        SetAlpha(target);
        group.blocksRaycasts = target > 0f;
    }

    private void SetAlpha(float a)
    {
        group.alpha = a;
        group.blocksRaycasts = a > 0f;
    }
}
