using UnityEngine;

/// <summary>
/// Submit 후 정산 팝업 열기 (Making). SceneFlowLink(hookResultSubmit)가 팀원 Submit 버튼에 실행 중에만 Show를 연결한다.
/// 팝업 자체는 Settlement Popup 프리팹(ResourceDB 키 "popup_making_settlement")이고, PopupService로 연다.
/// 정산 값은 SettlementRecorder가 GameSession에 모아 두므로 팝업은 어느 씬에서든 독립적으로 열 수 있다.
/// </summary>
public class SettlementPopup : MonoBehaviour
{
    public const string PopupKey = "popup_making_settlement";

    [SerializeField] private string popupKey = PopupKey;

    private bool shown;

    /// <summary>Submit 직후 호출 (팀원 판정 Result_Kiring이 수익을 반영한 다음).</summary>
    public void Show()
    {
        if (shown) return;
        shown = true;

        var popupService = FindAnyObjectByType<PopupService>();
        if (popupService != null)
        {
            popupService.ShowPopup(popupKey);
            if (popupService.TopPopup != null) return;
        }

        // 팝업을 열 수 없으면(PopupService 없음, ResourceDB에 키 없음) 흐름이 멈추지 않게 바로 다음으로
        Debug.LogWarning($"[Settlement] 정산 팝업({popupKey})을 열 수 없어 바로 다음 손님으로 넘어갑니다. ResourceDB.popupDict와 씬의 PopupService를 확인해 주세요.");
        SceneFlow.ReturnFromMaking();
    }
}
