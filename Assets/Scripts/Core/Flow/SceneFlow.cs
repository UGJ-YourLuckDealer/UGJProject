using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// v0.0.1 씬 전환: Title → Counter(손님 대화) → Making(키링 제작) → Counter(다음 손님) … → Result.
/// 씬 버튼은 SceneFlowLink를 통해 이 클래스를 호출한다.
/// </summary>
public static class SceneFlow
{
    public const string TitleScene = "Title";
    public const string CounterScene = "Counter";
    public const string MakingScene = "Making";
    public const string ResultScene = "Result";

    /// <summary>Counter를 다시 불러올 때 시작할 손님 번호 (DialogueManager.customer).</summary>
    public static int NextCustomerIndex { get; private set; }

    /// <summary>마지막 게임 결과 (Result 씬 표시용).</summary>
    public static GameResult? LastResult { get; private set; }

    private static int customerCount = int.MaxValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        NextCustomerIndex = 0;
        LastResult = null;
        customerCount = int.MaxValue;
    }

    // GameEvents는 SubsystemRegistration에서 구독이 비워지므로 그 뒤에 구독한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Subscribe() => GameEvents.GameEnded += r => LastResult = r;

    /// <summary>Title의 Start: 자본·흐름을 초기화하고 첫 손님부터 시작.</summary>
    public static void StartNewGame()
    {
        PersistentSystems.ResetSession();
        NextCustomerIndex = 0;
        LastResult = null;
        customerCount = int.MaxValue;
        SceneManager.LoadScene(CounterScene);
    }

    /// <summary>Counter에서 주문 수락 → Making. 현재 손님 다음 번호를 기억한다.</summary>
    public static void GoToMaking(DialogueManager dialogue)
    {
        if (dialogue != null)
        {
            NextCustomerIndex = dialogue.customer + 1;
            if (dialogue.dialogueData != null) customerCount = dialogue.dialogueData.Length;
        }
        SceneManager.LoadScene(MakingScene);
    }

    /// <summary>Making에서 제출 → 손님 처리 완료. 손님이 남았으면 Counter, 아니면 Result.</summary>
    public static void ReturnFromMaking()
    {
        GameEvents.RaiseCustomerFinished(null); // GameFlowManager가 처리 수를 세고, 마지막이면 GameEnded를 보낸다

        if (LastResult == null && NextCustomerIndex >= customerCount)
            LastResult = BuildResultNow(); // 대사 데이터가 손님 수보다 적어 먼저 끝난 경우

        SceneManager.LoadScene(LastResult != null ? ResultScene : CounterScene);
    }

    /// <summary>Result의 타이틀 버튼.</summary>
    public static void GoToTitle()
    {
        PersistentSystems.ResetSession();
        SceneManager.LoadScene(TitleScene);
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
