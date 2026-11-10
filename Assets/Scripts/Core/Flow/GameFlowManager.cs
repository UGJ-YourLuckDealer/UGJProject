using TMPro;
using UnityEngine;

/// <summary>
/// 게임 흐름·승패 (v0.0.1). GameEvents 신호를 받아 단계를 진행하고,
/// 단계가 바뀌면 GameEvents.PhaseChanged(화면 전환 요청), 종료 시 GameEvents.GameEnded를 보낸다.
/// 실제 화면/씬 전환은 PhaseChanged를 받는 쪽(팀원 작업)이 처리한다.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    [Tooltip("하루에 받는 손님 수. 다 받으면 하루 끝 (DayCycle.TotalDays일째면 게임 종료)")]
    [SerializeField, Min(1)] private int customersPerGame = 3;
    [SerializeField] private CapitalSystem capital;

    [Tooltip("임시 결과 표시 (선택). 비워 두면 Console에만 출력")]
    [SerializeField] private TMP_Text resultText;

    private GameFlow flow;

    public GamePhase Phase => flow != null ? flow.Phase : GamePhase.Classroom;
    public int CustomersFinished => flow != null ? flow.CustomersFinished : 0;

    private void Awake()
    {
        if (capital == null) capital = FindAnyObjectByType<CapitalSystem>();
        flow = new GameFlow(customersPerGame);
    }

    private void OnEnable()
    {
        GameEvents.OrderAccepted += OnOrderAccepted;
        GameEvents.KeyringServed += OnKeyringServed;
        GameEvents.CustomerFinished += OnCustomerFinished;
    }

    private void OnDisable()
    {
        GameEvents.OrderAccepted -= OnOrderAccepted;
        GameEvents.KeyringServed -= OnKeyringServed;
        GameEvents.CustomerFinished -= OnCustomerFinished;
    }

    private void Start()
    {
        if (resultText != null) resultText.gameObject.SetActive(false);
        flow.Begin();
        GameEvents.RaisePhaseChanged(flow.Phase);
    }

    private void OnOrderAccepted(CustomerData customer)
    {
        if (flow.OrderAccepted()) GameEvents.RaisePhaseChanged(flow.Phase);
    }

    private void OnKeyringServed(CustomerData customer, KeyringData keyring)
    {
        if (flow.KeyringServed()) GameEvents.RaisePhaseChanged(flow.Phase);
    }

    private void OnCustomerFinished(CustomerData customer)
    {
        if (flow.IsOver) return;
        GamePhase before = flow.Phase;
        bool ended = flow.CustomerFinished();
        // 기본 게임 오버 처리 (v0.0.2): 키링 제출 후 자본이 마이너스면 남은 손님과 상관없이 게임 오버
        if (!ended && capital != null && !GameFlow.IsWin(capital.Current)) ended = flow.ForceGameOver();
        if (flow.Phase != before) GameEvents.RaisePhaseChanged(flow.Phase);
        if (ended) EndGame();
    }

    private void EndGame()
    {
        float money = capital != null ? capital.Current : 0f;
        if (capital == null) Debug.LogWarning("[GameFlow] CapitalSystem이 없어 자본 0으로 판정합니다.");

        var result = new GameResult
        {
            isWin = GameFlow.IsWin(money),
            capital = money,
            customersFinished = flow.CustomersFinished,
            day = DayCycle.CurrentDay,
        };
        // 자본 ≥ 0으로 하루를 마쳤고 마지막 날이 아니면 다음 날로 이어진다
        result.isDayEnd = result.isWin && !DayCycle.IsLastDay;

        string title = result.isDayEnd ? $"Day {result.day} Clear" : (result.isWin ? "WIN" : "LOSE");
        string message = $"{title}  {money.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}";
        Debug.Log($"[GameFlow] {(result.isDayEnd ? "Day end" : "Game over")}: {message} ({result.customersFinished} customers)");
        if (resultText != null)
        {
            resultText.text = message;
            resultText.gameObject.SetActive(true);
        }

        GameEvents.RaiseGameEnded(result);
    }
}
