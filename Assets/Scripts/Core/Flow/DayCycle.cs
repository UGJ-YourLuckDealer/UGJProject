using UnityEngine;

/// <summary>
/// 일차 카운트·하루 시작/종료 (v0.0.1 이후). 하루 = 손님 N명(GameFlowManager.customersPerGame).
/// 하루가 끝나면 Result에서 다음 날로 넘어가고, TotalDays일차를 자본 ≥ 0으로 마치면 승리.
/// (잼 제출 빌드: 같은 손님 루프가 반복되지 않도록 1일만 진행. 날을 늘리려면 TotalDays만 바꾸면 된다.)
/// 다음 날은 SceneFlow.StartNewGame으로 손님 순서를 처음부터 다시 시작하되, 자본은 이어서 쓴다.
/// </summary>
public static class DayCycle
{
    /// <summary>승리까지 버텨야 하는 일수. 손님 데이터가 하루치(3명)뿐이라 1일만 진행한다.</summary>
    public const int TotalDays = 1;

    // 값(현재 일차, 이어받을 자본)은 GameSession에 저장한다.
    public static int CurrentDay => GameSession.CurrentDay;
    public static bool IsLastDay => CurrentDay >= TotalDays;

    /// <summary>Title에서 새 게임 시작: 1일차, 시작 자본부터.</summary>
    public static void BeginNewGame() => GameSession.ResetDays();

    /// <summary>Result의 Next Day 버튼: 지금 자본을 들고 다음 날 첫 손님부터 시작.</summary>
    public static void GoToNextDay()
    {
        if (ScreenFader.IsBusy) return; // 전환 중 중복 입력 무시
        var capital = Object.FindAnyObjectByType<CapitalSystem>();
        GameSession.CarriedCapital = capital != null ? capital.Current : (float?)null;
        GameSession.CurrentDay++;
        Debug.Log($"[DayCycle] Day {CurrentDay} start");
        SceneFlow.StartNewGame(); // 손님 순서·결과 초기화 후 Counter (자본은 CapitalSystem이 이어받음)
    }

    /// <summary>CapitalSystem 생성 시 시작 자본. 전날 자본이 있으면 그것을 한 번만 돌려준다.</summary>
    public static float TakeStartingCapital(float defaultCapital)
    {
        float value = GameSession.CarriedCapital ?? defaultCapital;
        GameSession.CarriedCapital = null;
        return value;
    }
}
