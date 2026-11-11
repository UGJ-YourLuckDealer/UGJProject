using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 정산 기록 (hermit). Making 씬에서 생긴 자본 변화(+ 수입, - 재료비)와 팁 여부를 GameSession에 모은다.
/// 키링 제출 판정(GameEvents.KeyringJudged)마다 하루 집계(수입·팁·환불·지출)에 더한다.
/// 정산 팝업(Settlement Popup 프리팹)은 이 값을 읽어 보여 주므로 씬과 상관없이 열 수 있다.
/// GameSystems 프리팹(씬 전환 후에도 유지)에 붙는다.
/// </summary>
public class SettlementRecorder : MonoBehaviour
{
    [Tooltip("레시피 요소 1개당 판매가 (기획: 모든 요소 $3, Result.cs 판정과 같은 값)")]
    [SerializeField] private float salePricePerElement = 3f;

    private float makingStartCapital;
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
        if (making && hasCapital)
        {
            makingStartCapital = capital.Current; // 이번 주문 시작 자본
            if (GameSession.DayStartCapital == null) GameSession.DayStartCapital = capital.Current; // 오늘 시작 자본
        }
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
        if (!inMaking) return;
        if (result.HasTip) GameSession.MarkTip();

        // 판정(Result.Result_Kiring)이 수익을 자본에 더한 직후에 온다
        float sale = result.total * salePricePerElement;
        float tip = result.grade == JudgementGrade.Perfect ? sale * KeyringJudge.TipRate : 0f;
        float refund = result.grade == JudgementGrade.Refund ? sale : 0f;
        float earned = sale + tip - refund;

        // 재료비 = 번 돈 - 이번 주문 동안 실제 자본 변화 (재료를 빼서 돌려받은 돈도 반영)
        var capital = FindAnyObjectByType<CapitalSystem>();
        float change = capital != null ? capital.Current - makingStartCapital : earned;
        float materialCost = Mathf.Max(0f, earned - change);

        GameSession.AddOrderSettlement(sale, tip, refund, materialCost);
    }
}
