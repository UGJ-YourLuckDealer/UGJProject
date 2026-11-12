using UnityEngine;

/// <summary>
/// Submit 후 처리 (Making). SceneFlowLink(hookResultSubmit)가 팀원 Submit 버튼에 실행 중에만 Show를 연결한다.
/// 프로토타입 v0.0.3: 키링마다 결산하지 않고, 하루 마지막 손님(또는 자본 &lt; 0으로 게임 오버)일 때만 하루 정산을 한다.
/// 하루가 끝나면 Counter로 돌아가 손님의 팁(반응) 대사를 먼저 보여 주고, 대사가 끝나면 SceneFlowLink가
/// 하루 정산 팝업(Settlement Popup 프리팹, ResourceDB 키 "popup_making_settlement")을 PopupService로 띄운다.
/// 정산 값은 SettlementRecorder가 GameSession에 하루 단위로 모은다.
/// </summary>
public class SettlementPopup : MonoBehaviour
{
    public const string PopupKey = "popup_making_settlement";

    private bool shown;

    /// <summary>Submit 직후 호출 (팀원 판정 Result_Kiring이 수익을 반영한 다음).</summary>
    public void Show()
    {
        if (shown) return;
        shown = true;

        // 하루가 끝나도 Counter로 돌아가 팁 대사를 본 뒤 정산 팝업 (SceneFlowLink.Update)
        if (IsDayOver()) GameSession.SettlementPending = true;
        SceneFlow.ReturnFromMaking(); // 손님 처리 완료 → Counter (팁 대사)
    }

    /// <summary>
    /// 지금 씬에 하루 정산 팝업을 연다 (Counter에서 팁 대사가 끝난 뒤).
    /// 팝업을 열 수 없으면(PopupService 없음, ResourceDB에 키 없음) 흐름이 멈추지 않게 바로 다음으로 넘어간다.
    /// </summary>
    public static void OpenDaySettlement()
    {
        GameSession.SettlementPending = false;
        var popupService = FindAnyObjectByType<PopupService>();
        if (popupService != null)
        {
            popupService.ShowPopup(PopupKey);
            if (popupService.TopPopup != null) return;
        }

        Debug.LogWarning($"[Settlement] 하루 정산 팝업({PopupKey})을 열 수 없어 바로 넘어갑니다. ResourceDB.popupDict와 씬의 PopupService를 확인해 주세요.");
        FinishDay(SceneFlow.LastResult == null || SceneFlow.LastResult.Value.isDayEnd);
    }

    /// <summary>정산이 끝난 뒤: 하루만 끝났으면 다음 날 Counter, 게임 종료(게임 오버·마지막 날)면 Result.</summary>
    public static void FinishDay(bool nextDay)
    {
        if (nextDay) DayCycle.GoToNextDay();
        else SceneFlow.GoToResult();
    }

    /// <summary>이번 손님으로 하루가 끝나는지 (오늘 마지막 손님이거나 자본 &lt; 0으로 게임 오버).</summary>
    public static bool IsDayOver()
    {
        var capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null && !GameFlow.IsWin(capital.Current)) return true;
        var flow = FindAnyObjectByType<GameFlowManager>();
        return flow != null && flow.CustomersFinished + 1 >= flow.CustomersPerGame;
    }
}
