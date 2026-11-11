using System.Collections.Generic;
using UnityEngine;

/// <summary>대화 로그 한 줄 (대화 기록 팝업용).</summary>
public struct DialogueLogEntry
{
    public string speaker;   // 말한 사람 이름
    public string text;      // 대사 전체
    public Sprite portrait;  // 초상화 (없으면 null)
    public int day;          // 몇 일차 대화인지
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

    // ---- 일차 (DayCycle) ----

    public static int CurrentDay { get; internal set; } = 1;

    /// <summary>다음 날로 이어받을 자본. 없으면 null (시작 자본 사용).</summary>
    public static float? CarriedCapital { get; internal set; }

    // ---- 손님 방문 준비 (CustomerVisit) ----

    public static bool VisitReady { get; internal set; }
    public static int ReadyCustomer { get; internal set; } = -1;

    // ---- 대화 로그 ----

    static readonly List<DialogueLogEntry> dialogueLog = new List<DialogueLogEntry>();

    /// <summary>지금까지 진행한 대화 (오래된 것부터). 씬과 상관없이 유지되어 어느 씬의 팝업에서도 읽을 수 있다.</summary>
    public static IReadOnlyList<DialogueLogEntry> DialogueLog => dialogueLog;

    /// <summary>대화 로그에 한 줄 추가 (DialogueLogRecorder가 호출).</summary>
    public static void AddDialogueLog(string speaker, string text, Sprite portrait)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        dialogueLog.Add(new DialogueLogEntry { speaker = speaker, text = text, portrait = portrait, day = CurrentDay });
    }

    public static void ClearDialogueLog() => dialogueLog.Clear();

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

    // ---- 초기화 ----

    /// <summary>새 게임·다음 날 시작: 손님 진행만 처음부터 (일차·자본 이어받기는 유지).</summary>
    public static void ResetCustomerProgress()
    {
        NextCustomerIndex = 0;
        CustomerCount = int.MaxValue;
        LastResult = null;
        ResetVisit();
    }

    /// <summary>1일차부터 다시 (이어받을 자본도 비움).</summary>
    public static void ResetDays()
    {
        CurrentDay = 1;
        CarriedCapital = null;
        ClearDialogueLog();
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
