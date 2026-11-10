using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 화면 Fade In/Out (v0.0.2). 게임 실행 시 첫 화면을 Fade In하고,
/// 씬 전환을 Fade Out → 로드 → Fade In으로 처리한다. 전환 중에는 클릭을 막는다.
/// UI는 Resources/ScreenFader.prefab (색·시간은 프리팹 Inspector에서 조정).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private const string PrefabPath = "ScreenFader"; // Resources/ScreenFader.prefab

    [SerializeField] private CanvasGroup group;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.35f;

    private static ScreenFader instance;
    private bool busy;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        instance = null;
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
    }

    // 게임 실행 시 첫 화면을 검은 화면에서 Fade In
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void FadeInOnLaunch()
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

    private IEnumerator FadeAndLoad(string sceneName)
    {
        busy = true;
        yield return Fade(1f, fadeOutSeconds);
        Time.timeScale = 1f; // 일시정지 상태로 전환되지 않게
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;
        yield return Fade(0f, fadeInSeconds);
        busy = false;
    }

    private IEnumerator Fade(float target, float seconds)
    {
        group.blocksRaycasts = true;
        float start = group.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(start, target, t / seconds));
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
