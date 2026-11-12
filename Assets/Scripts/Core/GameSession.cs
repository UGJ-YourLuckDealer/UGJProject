using System.Collections.Generic;
using UnityEngine;

/// <summary>대화 로그 한 줄 (대화 기록 팝업용).</summary>
public struct DialogueLogEntry
{
    public string speaker;   // 말한 사람 이름
    public string text;      // 대사 전체
    public Sprite portrait;  // 초상화 (없으면 null)
    public int day;          // 몇 일차 대화인지
    public int customer;     // 몇 번째 손님(주문)의 대화인지 (DialogueManager.customer)
    public bool isReaction;  // 키링 제출 후 반응 대사인지
}

/// <summary>
/// hermit 파트의 static(씬을 넘어 유지되는 값)을 한곳에서 관리한다.
/// - 값: 손님 진행(SceneFlow), 일차(DayCycle), 손님 방문 준비(CustomerVisit), 유지 오브젝트(PersistentSystems, ScreenFader)
/// - 초기화: Play 시작마다 ResetAll (Enter Play Mode에서 Domain Reload가 꺼져 있어 static이 Play 사이에 남기 때문)
/// - 다른 파일은 이 값을 읽고 쓰기만 하고, static 값이나 [RuntimeInitializeOnLoadMethod]를 따로 두지 않는다.
/// 외부(팀원 코드)는 기존처럼 SceneFlow.NextCustomerIndex, DayCycle.CurrentDay 등으로 읽으면 된다.
/// </summary>
public static class GameSession
{
    // ---- 손님 진행 (SceneFlow) ----

    /// <summary>Counter를 다시 불러올 때 시작할 손님 번호 (DialogueManager.customer).</summary>
    public static int NextCustomerIndex { get; internal set; }

    /// <summary>오늘 손님 수 (대사 데이터 수). 모르면 int.MaxValue.</summary>
    public static int CustomerCount { get; internal set; } = int.MaxValue;

    /// <summary>마지막 게임·하루 결과 (Result 씬 표시용). 없으면 null.</summary>
    public static GameResult? LastResult { get; internal set; }

    /// <summary>하루 마지막 손님 제출 후 Counter에서 팁 대사가 끝나면 하루 정산 팝업을 띄워야 하는지.</summary>
    public static bool SettlementPending { get; internal set; }

    // ---- 일차 (DayCycle) ----

    public static int CurrentDay { get; internal set; } = 1;

    /// <summary>다음 날로 이어받을 자본. 없으면 null (시작 자본 사용).</summary>
    public static float? CarriedCapital { get; internal set; }

    // ---- 손님 방문 준비 (CustomerVisit) ----

    public static bool VisitReady { get; internal set; }
    public static int ReadyCustomer { get; internal set; } = -1;

    // ---- 정산 (손님 1명 / 하루) ----

    /// <summary>지금 손님 키링 제작에서 번 돈 (판매가·팁).</summary>
    public static float CustomerIncome { get; internal set; }
    /// <summary>지금 손님 키링 제작에 쓴 돈 (재료비).</summary>
    public static float CustomerExpense { get; internal set; }
    /// <summary>지금 손님에게 팁을 받았는지.</summary>
    public static bool CustomerTip { get; internal set; }

    /// <summary>오늘 하루 합계 (하루 결과창용).</summary>
    public static float DayIncome { get; internal set; }
    public static float DayExpense { get; internal set; }
    public static int DayTipCount { get; internal set; }

    /// <summary>오늘 받은 주문의 판매가 합 (환불 주문 포함, 정산표 "수입").</summary>
    public static float DaySales { get; internal set; }
    /// <summary>오늘 받은 팁 합.</summary>
    public static float DayTips { get; internal set; }
    /// <summary>오늘 환불된 주문의 판매가 합 (정산표 "환불").</summary>
    public static float DayRefund { get; internal set; }
    /// <summary>오늘 쓴 재료비 합 (정산표 "지출").</summary>
    public static float DayMaterialCost { get; internal set; }
    /// <summary>오늘 하루 자본 변화 = 수입 + 팁 - 환불 - 지출.</summary>
    public static float DayTotal => DaySales + DayTips - DayRefund - DayMaterialCost;
    /// <summary>오늘 하루 시작 자본 (정산표 "Balance"). 첫 키링 제작에 들어갈 때 기록, 기록 전에는 null.</summary>
    public static float? DayStartCapital { get; internal set; }

    /// <summary>키링 제출 1건을 하루 정산에 더한다 (SettlementRecorder).</summary>
    public static void AddOrderSettlement(float sale, float tip, float refund, float materialCost)
    {
        DaySales += sale;
        DayTips += tip;
        DayRefund += refund;
        DayMaterialCost += materialCost;
    }

    /// <summary>Making에 들어올 때마다 (SettlementRecorder).</summary>
    public static void ResetCustomerSettlement()
    {
        CustomerIncome = 0f;
        CustomerExpense = 0f;
        CustomerTip = false;
    }

    public static void ResetDaySettlement()
    {
        DayIncome = 0f;
        DayExpense = 0f;
        DayTipCount = 0;
        DaySales = 0f;
        DayTips = 0f;
        DayRefund = 0f;
        DayMaterialCost = 0f;
        DayStartCapital = null;
        ResetCustomerSettlement();
    }

    /// <summary>자본 변화 기록 (SettlementRecorder): + 는 수입, - 는 지출.</summary>
    public static void AddSettlement(float delta)
    {
        if (delta > 0f) { CustomerIncome += delta; DayIncome += delta; }
        else if (delta < 0f) { CustomerExpense -= delta; DayExpense -= delta; }
    }

    public static void MarkTip()
    {
        if (CustomerTip) return;
        CustomerTip = true;
        DayTipCount++;
    }

    // ---- 마지막 판정 (손님 반응 대화용) ----

    /// <summary>
    /// 가장 최근 키링 제출 판정 결과. Making에서 판정하고 다음 Counter에서 손님 반응 대화를 보여 줄 때 읽는다.
    /// 판정 전이면 null. 새 게임(1일차)에서만 비우고, 다음 날로 넘어가도 유지된다 (마지막 손님 반응이 다음 날 첫 화면에 나올 수 있으므로).
    /// </summary>
    public static JudgementResult? LastJudgement { get; internal set; }
    /// <summary>LastJudgement가 몇 번째 손님(DialogueManager.customer)의 판정인지.</summary>
    public static int LastJudgedCustomer { get; internal set; } = -1;
    /// <summary>LastJudgement에서 받은 팁 금액 (팁이 없으면 0).</summary>
    public static float LastTipAmount { get; internal set; }

    /// <summary>
    /// 손님 반응 대사 번호 (DialogueData.dialogueBifurcations 순서 = Best, Good, Bad / Customer_Data.State와 같은 순서).
    /// Perfect(팁) = 0 Best, Pass = 1 Good, Refund = 2 Bad. 판정 전이면 -1.
    /// </summary>
    public static int ReactionIndex
    {
        get
        {
            if (LastJudgement == null) return -1;
            switch (LastJudgement.Value.grade)
            {
                case JudgementGrade.Perfect: return 0;
                case JudgementGrade.Pass: return 1;
                default: return 2;
            }
        }
    }

    /// <summary>판정 결과 저장 (SettlementRecorder).</summary>
    public static void SetLastJudgement(JudgementResult result, int customer, float tipAmount)
    {
        LastJudgement = result;
        LastJudgedCustomer = customer;
        LastTipAmount = tipAmount;
    }

    public static void ClearLastJudgement()
    {
        LastJudgement = null;
        LastJudgedCustomer = -1;
        LastTipAmount = 0f;
    }

    // ---- 대화 로그 ----

    static readonly List<DialogueLogEntry> dialogueLog = new List<DialogueLogEntry>();

    /// <summary>지금까지 진행한 대화 (오래된 것부터). 씬과 상관없이 유지되어 어느 씬의 팝업에서도 읽을 수 있다.</summary>
    public static IReadOnlyList<DialogueLogEntry> DialogueLog => dialogueLog;

    /// <summary>대화 로그에 한 줄 추가 (DialogueLogRecorder가 호출).</summary>
    public static void AddDialogueLog(string speaker, string text, Sprite portrait, int customer, bool isReaction = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        dialogueLog.Add(new DialogueLogEntry
        {
            speaker = speaker, text = text, portrait = portrait,
            day = CurrentDay, customer = customer, isReaction = isReaction,
        });
    }

    public static void ClearDialogueLog() => dialogueLog.Clear();

    /// <summary>지금 주문(키링 제작 중인 손님)의 번호. Counter에서 Making으로 넘어갈 때 기록된 다음 손님 번호 - 1.</summary>
    public static int CurrentOrderCustomer => NextCustomerIndex - 1;

    /// <summary>지금 주문에서 나눈 대화만 (오늘, 지금 손님, 제출 후 반응 대사 제외).</summary>
    public static List<DialogueLogEntry> CurrentOrderDialogue()
    {
        var result = new List<DialogueLogEntry>();
        int customer = CurrentOrderCustomer;
        foreach (var entry in dialogueLog)
            if (entry.day == CurrentDay && entry.customer == customer && !entry.isReaction)
                result.Add(entry);
        return result;
    }

    // ---- 입력 잠금 ----

    /// <summary>
    /// 설정 팝업 등 화면을 덮는 UI가 열려 있어 게임 입력(대화 넘기기, 재료 드래그 등)을 받으면 안 되는 상태.
    /// 마우스를 직접 읽는 코드는 이 값이 true면 입력을 무시하면 된다.
    /// </summary>
    public static bool IsUIBlockingInput { get; internal set; }

    // ---- 씬을 넘어 유지되는 오브젝트 ----

    /// <summary>유지 중인 GameSystems (자본·흐름·판정).</summary>
    public static PersistentSystems Systems { get; internal set; }

    /// <summary>화면 Fade 오브젝트 (Resources/ScreenFader 프리팹에서 생성).</summary>
    public static ScreenFader Fader { get; internal set; }

    /// <summary>유도 툴팁 서비스 (Resources/GuideTooltip 프리팹 또는 씬에 놓인 것).</summary>
    public static GuideTooltip GuideTooltip { get; internal set; }

    // ---- 초기화 ----

    /// <summary>새 게임·다음 날 시작: 손님 진행만 처음부터 (일차·자본 이어받기는 유지).</summary>
    public static void ResetCustomerProgress()
    {
        NextCustomerIndex = 0;
        CustomerCount = int.MaxValue;
        LastResult = null;
        SettlementPending = false;
        ResetVisit();
        ResetDaySettlement();
    }

    /// <summary>1일차부터 다시 (이어받을 자본도 비움).</summary>
    public static void ResetDays()
    {
        CurrentDay = 1;
        CarriedCapital = null;
        ClearDialogueLog();
        ClearLastJudgement();
    }

    /// <summary>손님 방문 준비 상태만 초기화 (Counter에 들어올 때마다).</summary>
    public static void ResetVisit()
    {
        VisitReady = false;
        ReadyCustomer = -1;
    }

    /// <summary>모든 static 값과 이벤트 구독을 처음 상태로 (Play 시작 시 자동 호출).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAll()
    {
        GameEvents.ClearAll();
        ScreenFader.ClearStaticHooks();
        ResetCustomerProgress();
        ResetDays();
        IsUIBlockingInput = false;
        Systems = null;
        Fader = null;
        GuideTooltip = null;
    }

    // ResetAll(이벤트 비우기) 다음, 첫 씬 로드 전에 static 구독을 건다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeStaticHandlers()
    {
        GameEvents.GameEnded += result => LastResult = result;
    }

    // 첫 씬 로드 후: 게임 실행 첫 화면 Fade In
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstSceneLoad()
    {
        ScreenFader.FadeInOnLaunch();
    }
}
