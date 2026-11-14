using System.Globalization;
using System.Text;
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
    [Tooltip("게임이 끝났을 때(WIN/LOSE) Title 버튼을 누르면 이 크레딧을 보여 준 뒤 Title로 간다. 비우면 바로 Title")]
    [SerializeField] private GameObject creditPopup;

    private bool creditShown;

    [Header("하루 정산표 (기획: 잔액·수입·팁·환불·지출·합계·게임오버 여부)")]
    [SerializeField] private Color goodColor = new Color32(0x2E, 0x9E, 0x3A, 0xFF);  // 유리한 값
    [SerializeField] private Color badColor = new Color32(0xD9, 0x3B, 0x2E, 0xFF);   // 불리한 값
    [SerializeField] private string surviveMessage = "You survived today!";
    [SerializeField] private string gameOverMessage = "Game Over";
    [Tooltip("값이 시작되는 위치 (줄 너비 기준 %)")]
    [SerializeField] private float valueColumn = 55f;

    // ButtonClickDelay(실행 순서 1000)가 버튼 동작을 감싸기 전에 바꿔야 하므로 Awake에서 처리한다
    private void Awake()
    {
        var result = SceneFlow.LastResult;
        if (result == null) return;

        if (titleButton == null)
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.name == "TitleButton") { titleButton = b; break; }
        }
        if (titleButton == null) return;

        // 게임 종료(WIN/LOSE): Title 버튼 → 크레딧 → (크레딧이 닫히면) Title
        if (!result.Value.isDayEnd)
        {
            if (creditPopup == null) return; // 크레딧이 없으면 씬에 연결된 GoToTitle 그대로
            for (int i = 0; i < titleButton.onClick.GetPersistentEventCount(); i++)
                titleButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            titleButton.onClick.AddListener(ShowCreditsThenTitle);
            return;
        }

        // 씬에 연결된 GoToTitle은 이번 실행에서만 끄고 다음 날로 가는 동작을 붙인다 (씬 파일은 그대로)
        for (int i = 0; i < titleButton.onClick.GetPersistentEventCount(); i++)
            titleButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        titleButton.onClick.AddListener(DayCycle.GoToNextDay);

        var label = titleButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = nextDayLabel;
    }

    private void ShowCreditsThenTitle()
    {
        if (creditShown) return;
        creditShown = true;
        titleButton.interactable = false;

        var canvas = titleButton.GetComponentInParent<Canvas>();
        var parent = canvas != null ? canvas.rootCanvas.transform : null;
        var popup = Instantiate(creditPopup, parent, false);
        popup.transform.SetAsLastSibling(); // 결과 화면 위에 덮기
        var roll = popup.GetComponentInChildren<CreditRoll>();
        if (roll == null) { SceneFlow.GoToTitle(); return; }
        roll.Closed += SceneFlow.GoToTitle; // 끝까지 올라가거나 클릭·ESC로 닫으면 Title
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
        if (detailText != null) detailText.text = BuildSettlement(r);
    }

    // 하루 정산표: 소수점 2자리, 유리한 값 초록·불리한 값 빨강
    private string BuildSettlement(GameResult r)
    {
        var sb = new StringBuilder();
        float balance = GameSession.DayStartCapital ?? (r.capital - GameSession.DayTotal); // 오늘 시작 자본
        Row(sb, "Balance", Money(balance), balance >= 0f ? (Color?)null : badColor);
        Row(sb, "Income", "+" + Money(GameSession.DaySales), goodColor);
        Row(sb, "Tips", "+" + Money(GameSession.DayTips), goodColor);
        Row(sb, "Refund", "-" + Money(GameSession.DayRefund), badColor);
        Row(sb, "Expense", "-" + Money(GameSession.DayMaterialCost), badColor);
        // Total = Balance + Income + Tips - Refund - Expense = 하루가 끝난 지금 자본 (기획 정산표 레퍼런스와 같은 방식)
        Row(sb, "<b>Total</b>", "<b>" + (r.capital < 0f ? "-" : "") + Money(Mathf.Abs(r.capital)) + "</b>", r.capital >= 0f ? goodColor : badColor);

        bool gameOver = !r.isWin;
        sb.Append("\n<align=center>")
          .Append(Colored(gameOver ? gameOverMessage : surviveMessage, gameOver ? badColor : goodColor))
          .Append($"  (Day {Mathf.Max(1, r.day)} / {DayCycle.TotalDays})</align>");
        return sb.ToString();
    }

    private void Row(StringBuilder sb, string label, string value, Color? color)
    {
        sb.Append(label)
          .Append("<pos=").Append(valueColumn.ToString(CultureInfo.InvariantCulture)).Append("%>")
          .Append(color.HasValue ? Colored(value, color.Value) : value)
          .Append('\n');
    }

    private static string Colored(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    private static string Money(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
}
