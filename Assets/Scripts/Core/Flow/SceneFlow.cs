using UnityEngine;

/// <summary>
/// 씬 전환(Fade Out → 로드 → Fade In, ScreenFader): Title → Counter(손님 대화) → Making(키링 제작) → Counter(다음 손님) … → Result.
/// 씬 버튼은 SceneFlowLink를 통해 이 클래스를 호출한다.
/// </summary>
public static class SceneFlow
{
    public const string TitleScene = "Title";
    public const string CounterScene = "Counter";
    public const string MakingScene = "Making";
    public const string ResultScene = "Result";

    // 값은 GameSession에 저장한다 (static 값·초기화는 GameSession 한 곳에서 관리). GameEnded → LastResult 저장도 GameSession이 한다.

    /// <summary>Counter를 다시 불러올 때 시작할 손님 번호 (DialogueManager.customer).</summary>
    public static int NextCustomerIndex => GameSession.NextCustomerIndex;

    /// <summary>마지막 게임 결과 (Result 씬 표시용).</summary>
    public static GameResult? LastResult => GameSession.LastResult;

    /// <summary>Title의 Start: 자본·흐름을 초기화하고 첫 손님부터 시작.</summary>
    public static void StartNewGame()
    {
        if (ScreenFader.IsBusy) return; // 전환 중 중복 입력 무시
        PersistentSystems.ResetSession();
        GameSession.ResetCustomerProgress();
        ScreenFader.LoadScene(CounterScene);
    }

    /// <summary>Counter에서 주문 수락 → Making. 현재 손님 번호를 기억한다 (돌아오면 같은 손님의 반응 대사).</summary>
    public static void GoToMaking(DialogueManager dialogue)
    {
        if (ScreenFader.IsBusy) return;
        if (dialogue != null)
        {
            GameSession.NextCustomerIndex = dialogue.customer;
            if (dialogue.dialogueData != null) GameSession.CustomerCount = dialogue.dialogueData.Length;
        }
        ScreenFader.LoadSceneRising(MakingScene); // 책상이 올라오며 Making으로 (Fade 아님)
    }

    /// <summary>
    /// Counter를 떠날 때(SceneFlowLink.OnDestroy) 호출. SceneFlow를 거치지 않은 전환(팀원 코드의 직접 로드)도
    /// 손님 번호가 이어지도록 기록한다.
    /// </summary>
    public static void NoteCounterLeft(int currentCustomer, int count)
    {
        GameSession.NextCustomerIndex = currentCustomer;
        if (count > 0) GameSession.CustomerCount = count;
    }

    /// <summary>
    /// Making에서 제출 → 손님 처리 완료. 손님이 남았으면 Counter, 아니면 Result.
    /// 하루 정산이 남아 있으면(SettlementPending) 하루가 끝났어도 Counter로 가서 팁 대사 → 정산 팝업 순서로 보여 준다.
    /// </summary>
    public static void ReturnFromMaking()
    {

        if (ScreenFader.IsBusy) return; // 손님 처리 완료가 두 번 세지지 않게
        GameEvents.RaiseCustomerFinished(null); // GameFlowManager가 처리 수를 세고, 마지막이면 GameEnded를 보낸다

        if (LastResult == null && NextCustomerIndex >= GameSession.CustomerCount)
            GameSession.LastResult = BuildResultNow(); // 대사 데이터가 손님 수보다 적어 먼저 끝난 경우

        bool toResult = LastResult != null && !GameSession.SettlementPending;
        ScreenFader.LoadScene(toResult ? ResultScene : CounterScene); // Fade Out → 로드 → Fade In
    }

    /// <summary>정산 팝업을 닫은 뒤 게임 종료(게임 오버·마지막 날)일 때: Result로.</summary>
    public static void GoToResult()
    {
        if (ScreenFader.IsBusy) return;
        if (LastResult == null) GameSession.LastResult = BuildResultNow();
        ScreenFader.LoadScene(ResultScene);
    }

    /// <summary>Result의 타이틀 버튼.</summary>
    public static void GoToTitle()
    {
        if (ScreenFader.IsBusy) return;
        PersistentSystems.ResetSession();
        GameSession.ClearDialogueLog(); // 게임을 그만두면 대화 기록도 비움
        ScreenFader.LoadScene(TitleScene);
    }

    private static GameResult BuildResultNow()
    {
        var capital = Object.FindAnyObjectByType<CapitalSystem>();
        var flow = Object.FindAnyObjectByType<GameFlowManager>();
        float money = capital != null ? capital.Current : 0f;
        return new GameResult
        {
            isWin = GameFlow.IsWin(money),
            capital = money,
            customersFinished = flow != null ? flow.CustomersFinished : NextCustomerIndex,
        };
    }
}
