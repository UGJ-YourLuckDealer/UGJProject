using System;
using UnityEngine;

/// <summary>
/// 시스템 간 연결 규칙 (hermit ↔ macro). 보내는 쪽은 Raise...를 호출하고, 받는 쪽은 이벤트를 구독한다.
/// 씬 참조 없이 연결되므로 팀원 패키지를 그대로 붙일 수 있다.
/// 구독한 MonoBehaviour는 OnDisable/OnDestroy에서 반드시 구독 해제한다.
/// </summary>
public static class GameEvents
{
    // ---- 손님/키링 제작 → 게임 흐름·자본·판정 (macro → hermit) ----

    /// <summary>손님 주문 수락. 레시피 판매가 선지급에 사용.</summary>
    public static event Action<CustomerData> OrderAccepted;

    /// <summary>재료를 키링에 달았음 = 재료비 차감 요청.</summary>
    public static event Action<IngredientData> IngredientUsed;

    /// <summary>서빙 버튼: 제작된 키링 정보(재료·개수)를 판정으로 전달.</summary>
    public static event Action<CustomerData, KeyringData> KeyringServed;

    /// <summary>손님 1명 처리 완료 (판정 후 반응 대사까지 끝남). 게임 흐름이 다음 손님/종료로 진행.</summary>
    public static event Action<CustomerData> CustomerFinished;

    // ---- 판정 → 손님·UI (hermit → macro) ----

    /// <summary>판정 결과. 손님 반응 대사·팁 표시에 사용.</summary>
    public static event Action<CustomerData, JudgementResult> KeyringJudged;

    // ---- 게임 흐름·자본 → 화면/UI (hermit → 화면 전환·UI 담당) ----

    /// <summary>흐름 단계 변경 = 화면(View) 전환 요청. Classroom: 교실 View, Crafting: 키링 작업 View, GameOver: 결과.</summary>
    public static event Action<GamePhase> PhaseChanged;

    /// <summary>현재 자본이 바뀜 (재화 표시용).</summary>
    public static event Action<float> CapitalChanged;

    /// <summary>손님을 모두 처리해 게임 종료. 승패·남은 자본.</summary>
    public static event Action<GameResult> GameEnded;

    // ---- Counter 손님 방문 시퀀스 (hermit → macro) ----

    /// <summary>하루 시작: Counter 첫 손님 전에 "Day N" 날짜 표시를 시작함. 인자 = 일차.</summary>
    public static event Action<int> DayStarted;

    /// <summary>
    /// 손님 방문 시퀀스(문 소리 → 초상화 Fade → 대화)를 시작해도 됨. 인자 = 손님 번호(DialogueManager.customer).
    /// 하루 첫 손님은 날짜 표시가 끝난 뒤, 이후 손님은 Counter 화면 Fade In이 끝난 뒤 한 번 온다.
    /// 구독이 늦었으면 CustomerVisit.IsReady / CustomerVisit.ReadyCustomer로 확인.
    /// </summary>
    public static event Action<int> CustomerVisitReady;

    public static void RaisePhaseChanged(GamePhase phase) => PhaseChanged?.Invoke(phase);
    public static void RaiseCapitalChanged(float capital) => CapitalChanged?.Invoke(capital);
    public static void RaiseGameEnded(GameResult result) => GameEnded?.Invoke(result);
    public static void RaiseDayStarted(int day) => DayStarted?.Invoke(day);
    public static void RaiseCustomerVisitReady(int customer) => CustomerVisitReady?.Invoke(customer);

    public static void RaiseOrderAccepted(CustomerData customer) => OrderAccepted?.Invoke(customer);
    public static void RaiseIngredientUsed(IngredientData ingredient) => IngredientUsed?.Invoke(ingredient);
    public static void RaiseKeyringServed(CustomerData customer, KeyringData keyring) => KeyringServed?.Invoke(customer, keyring);
    public static void RaiseCustomerFinished(CustomerData customer) => CustomerFinished?.Invoke(customer);
    public static void RaiseKeyringJudged(CustomerData customer, JudgementResult result) => KeyringJudged?.Invoke(customer, result);

    /// <summary>모든 구독을 비운다. Play 시작마다 GameSession.ResetAll이 호출 (Domain Reload가 꺼져 있어 구독이 Play 간에 남음).</summary>
    internal static void ClearAll()
    {
        OrderAccepted = null;
        IngredientUsed = null;
        KeyringServed = null;
        CustomerFinished = null;
        KeyringJudged = null;
        PhaseChanged = null;
        CapitalChanged = null;
        GameEnded = null;
        DayStarted = null;
        CustomerVisitReady = null;
    }
}
