/// <summary>게임 흐름 단계. 화면(View) 전환은 GameEvents.PhaseChanged를 받아 처리한다.</summary>
public enum GamePhase
{
    Classroom, // 교실 View: 손님 대화·주문
    Crafting,  // 키링 작업 View: 키링 제작·서빙
    GameOver,  // 손님을 모두 처리함 → 승패 결과
}

/// <summary>게임 종료 결과.</summary>
public struct GameResult
{
    public bool isWin;
    public float capital;
    public int customersFinished;
}

/// <summary>
/// v0.0.1 게임 흐름 규칙 (Unity와 무관한 순수 로직).
/// 교실 → (주문 수락) → 키링 작업 → (서빙) → 교실 → (손님 처리 완료) → 다음 손님 … N명 처리 후 종료.
/// </summary>
public class GameFlow
{
    public int CustomersPerGame { get; }
    public GamePhase Phase { get; private set; } = GamePhase.Classroom;
    public int CustomersFinished { get; private set; }
    public bool IsOver => Phase == GamePhase.GameOver;

    public GameFlow(int customersPerGame)
    {
        CustomersPerGame = customersPerGame < 1 ? 1 : customersPerGame;
    }

    /// <summary>승패 기준 (v0.0.1): 종료 시 자본 ≥ 0이면 승리.</summary>
    public static bool IsWin(float capital) => capital >= 0f;

    public void Begin()
    {
        CustomersFinished = 0;
        Phase = GamePhase.Classroom;
    }

    /// <summary>주문 수락 → 키링 작업 View. 단계가 바뀌면 true.</summary>
    public bool OrderAccepted()
    {
        if (Phase != GamePhase.Classroom) return false;
        Phase = GamePhase.Crafting;
        return true;
    }

    /// <summary>서빙(제출) → 교실 View. 단계가 바뀌면 true.</summary>
    public bool KeyringServed()
    {
        if (Phase != GamePhase.Crafting) return false;
        Phase = GamePhase.Classroom;
        return true;
    }

    /// <summary>손님이 남아 있어도 바로 게임 오버 (예: 키링 제출 후 자본이 마이너스). 이미 끝났으면 false.</summary>
    public bool ForceGameOver()
    {
        if (IsOver) return false;
        Phase = GamePhase.GameOver;
        return true;
    }

    /// <summary>손님 1명 처리 완료. 마지막 손님이면 GameOver로 바꾸고 true.</summary>
    public bool CustomerFinished()
    {
        if (IsOver) return false;
        CustomersFinished++;
        if (CustomersFinished >= CustomersPerGame)
        {
            Phase = GamePhase.GameOver;
            return true;
        }
        Phase = GamePhase.Classroom; // 주문 거절 등으로 제작 중에 끝나도 교실로 복귀
        return false;
    }
}
