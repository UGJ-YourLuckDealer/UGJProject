using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Result 씬: 하루 끝 또는 게임 결과(승패·남은 자본·처리한 손님 수·일차)를 표시한다.
/// 하루만 끝났으면(GameResult.isDayEnd) "Day N Clear"를 보여 주고 Title 버튼을 Next Day 버튼으로 바꾼다.
/// </summary>
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [Tooltip("비우면 씬에서 이름이 TitleButton인 버튼을 찾는다")]
    [SerializeField] private Button titleButton;
    [SerializeField] private string dayClearFormat = "Day {0} Clear";
    [SerializeField] private string nextDayLabel = "Next Day";

    // ButtonClickDelay(실행 순서 1000)가 버튼 동작을 감싸기 전에 바꿔야 하므로 Awake에서 처리한다
    private void Awake()
    {
        var result = SceneFlow.LastResult;
        if (result == null || !result.Value.isDayEnd) return;

        if (titleButton == null)
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.name == "TitleButton") { titleButton = b; break; }
        }
        if (titleButton == null) return;

        // 씬에 연결된 GoToTitle은 이번 실행에서만 끄고 다음 날로 가는 동작을 붙인다 (씬 파일은 그대로)
        for (int i = 0; i < titleButton.onClick.GetPersistentEventCount(); i++)
            titleButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        titleButton.onClick.AddListener(DayCycle.GoToNextDay);

        var label = titleButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = nextDayLabel;
    }

    private void Start()
    {
        var result = SceneFlow.LastResult;
        if (result == null)
        {
            if (titleText != null) titleText.text = "No Result";
            if (detailText != null) detailText.text = "Start from the Title scene.";
            return;
        }

        var r = result.Value;
        if (titleText != null)
            titleText.text = r.isDayEnd ? string.Format(dayClearFormat, r.day) : (r.isWin ? "WIN" : "LOSE");
        if (detailText != null)
            detailText.text = $"Money  {r.capital.ToString("0.00", CultureInfo.InvariantCulture)}\nDay  {Mathf.Max(1, r.day)} / {DayCycle.TotalDays}";
    }
}
