using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 정산 기록 (hermit). Making 씬에서 생긴 자본 변화(+ 수입, - 재료비)와 팁 여부를 GameSession에 모은다.
/// 정산 팝업(Settlement Popup 프리팹)은 이 값을 읽어 보여 주므로 씬과 상관없이 열 수 있다.
/// GameSystems 프리팹(씬 전환 후에도 유지)에 붙는다.
/// </summary>
public class SettlementRecorder : MonoBehaviour
{
    private float lastCapital;
    private bool hasCapital;
    private bool inMaking;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameEvents.CapitalChanged += OnCapitalChanged;
        GameEvents.KeyringJudged += OnKeyringJudged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameEvents.CapitalChanged -= OnCapitalChanged;
        GameEvents.KeyringJudged -= OnKeyringJudged;
    }

    private void Start() => OnSceneEntered(SceneManager.GetActiveScene());

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) OnSceneEntered(scene);
    }

    private void OnSceneEntered(Scene scene)
    {
        bool making = scene.name == SceneFlow.MakingScene;
        if (making && !inMaking) GameSession.ResetCustomerSettlement(); // 손님 1명 정산 시작
        inMaking = making;

        var capital = FindAnyObjectByType<CapitalSystem>();
        hasCapital = capital != null;
        if (hasCapital) lastCapital = capital.Current;
    }

    private void OnCapitalChanged(float value)
    {
        if (!hasCapital) { lastCapital = value; hasCapital = true; return; }
        float delta = value - lastCapital;
        lastCapital = value;
        if (inMaking) GameSession.AddSettlement(delta);
    }

    private void OnKeyringJudged(CustomerData customer, JudgementResult result)
    {
        if (inMaking && result.HasTip) GameSession.MarkTip();
    }
}
