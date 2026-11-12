using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 하루 정산 팝업 (Settlement Popup 프리팹, ResourceDB 키 "popup_making_settlement", 기획: 정산 팝업.pdf).
/// 하루 마지막 손님의 키링을 제출하면 자동으로 뜬다. 정산표(가계부 클립보드)가 오른쪽에서 가운데로 들어오고
/// 잔액·수입·팁·환불·지출·합계를 하나씩 효과음과 함께 보여 준 뒤 게임오버 여부를 표시한다.
/// 숫자는 소수점 2자리, 유리한 값은 초록·불리한 값은 빨강.
/// 다 나온 뒤 빈 곳을 클릭하거나 마우스 우클릭하면 닫히고 바로 다음 날 손님 대기(Counter)로 간다.
/// (게임 오버·마지막 날이면 Result 화면으로)
/// 값: Balance = 오늘 시작 자본, Total = Balance + Income + Tips - Refund - Expense (= 지금 자본)
/// </summary>
public class SettlementPopupView : MonoBehaviour
{
    [Header("References (프리팹 안)")]
    [SerializeField] private Image dim;
    [SerializeField] private RectTransform box;
    [Tooltip("Balance, Income, Tips, Refund, Expense, Total 순서")]
    [SerializeField] private RectTransform[] rows = new RectTransform[6];
    [Tooltip("rows와 같은 순서의 값 텍스트")]
    [SerializeField] private TMP_Text[] values = new TMP_Text[6];
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text markText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private AudioSource audioSource;

    [Header("Text·Color")]
    [SerializeField] private string titleFormat = "Day {0} Summary";
    [SerializeField] private string surviveMessage = "You made it through today!";
    [SerializeField] private string clearMessage = "You survived all 7 days!";
    [SerializeField] private string gameOverMessage = "Game Over";
    [SerializeField] private Color normalColor = new Color(0.30f, 0.20f, 0.15f, 1f);
    [SerializeField] private Color goodColor = new Color32(0x2E, 0x9E, 0x3A, 0xFF);  // 유리한 값
    [SerializeField] private Color badColor = new Color32(0xD9, 0x3B, 0x2E, 0xFF);   // 불리한 값

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float slideSeconds = 0.8f;
    [SerializeField] private float slideFromX = 1400f;
    [Tooltip("정산 값이 하나씩 나오는 간격")]
    [SerializeField, Min(0f)] private float lineInterval = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.6f;

    [Tooltip("팝업이 떠 있는 동안 꺼 둘 컴포넌트 (마우스를 직접 읽어 팝업 뒤로 클릭이 새는 팀원 스크립트)")]
    [SerializeField] private string[] blockWhileOpen =
    {
        "ObjectInteraction", "ObjectDragManager", "ObjectDeleteManager", "KeyringMaterialObject",
        "DialogueManager", "DeskInteraction",
    };

    private readonly Dictionary<float, AudioClip> dings = new Dictionary<float, AudioClip>();
    private Color dimColor;
    private Vector2 boxPosition;
    private bool canClose;
    private bool leaving;
    private bool gameOver;
    private bool lastDay;

    private void Awake()
    {
        if (dim != null) dimColor = dim.color;
        if (box != null) boxPosition = box.anchoredPosition;
    }

    private void OnEnable()
    {
        GameSession.IsUIBlockingInput = true;
        var names = new HashSet<string>(blockWhileOpen);
        foreach (var b in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (b.enabled && names.Contains(b.GetType().Name)) b.enabled = false; // 다음 씬으로 넘어가면 사라짐
    }

    private void OnDestroy() => GameSession.IsUIBlockingInput = false;

    private void Start()
    {
        var capital = FindAnyObjectByType<CapitalSystem>();
        float now = capital != null ? capital.Current : 0f;
        float total = GameSession.DayTotal;
        float balance = GameSession.DayStartCapital ?? (now - total);
        gameOver = !GameFlow.IsWin(now);
        lastDay = DayCycle.IsLastDay;

        if (titleText != null) titleText.text = string.Format(titleFormat, DayCycle.CurrentDay);
        SetValue(0, Money(balance), balance >= 0f ? normalColor : badColor);
        SetValue(1, "+" + Money(GameSession.DaySales), goodColor);
        SetValue(2, "+" + Money(GameSession.DayTips), goodColor);
        SetValue(3, "-" + Money(GameSession.DayRefund), badColor);
        SetValue(4, "-" + Money(GameSession.DayMaterialCost), badColor);
        SetValue(5, Money(now), now >= 0f ? goodColor : badColor);
        if (markText != null)
        {
            markText.text = gameOver ? gameOverMessage : (lastDay ? clearMessage : surviveMessage);
            markText.color = gameOver ? badColor : goodColor;
        }

        StartCoroutine(Play());
    }

    private static string Money(float v) => (v < 0f ? "-" : "") + Mathf.Abs(v).ToString("0.00", CultureInfo.InvariantCulture);

    private void SetValue(int i, string text, Color color)
    {
        if (i >= values.Length || values[i] == null) return;
        values[i].text = text;
        values[i].color = color;
    }

    private IEnumerator Play()
    {
        foreach (var r in rows) if (r != null) r.gameObject.SetActive(false);
        if (markText != null) markText.gameObject.SetActive(false);
        if (hintText != null) hintText.gameObject.SetActive(false);

        // 어두운 패널 + 오른쪽에서 가운데로 들어오는 정산표
        Vector2 from = boxPosition + new Vector2(slideFromX, 0f);
        for (float t = 0f; t < slideSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / slideSeconds;
            if (dim != null) dim.color = new Color(dimColor.r, dimColor.g, dimColor.b, dimColor.a * Mathf.Clamp01(k * 2f));
            if (box != null) box.anchoredPosition = Vector2.LerpUnclamped(from, boxPosition, 1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }
        if (dim != null) dim.color = dimColor;
        if (box != null) box.anchoredPosition = boxPosition;

        // 정산 값을 하나씩 (띠링)
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null) continue;
            yield return new WaitForSecondsRealtime(lineInterval);
            rows[i].gameObject.SetActive(true);
            PlayDing(i);
            yield return Punch(rows[i]);
        }

        if (markText != null)
        {
            yield return new WaitForSecondsRealtime(lineInterval);
            markText.gameObject.SetActive(true);
            PlayDing(rows.Length);
            yield return Punch(markText.rectTransform);
        }

        if (hintText != null) hintText.gameObject.SetActive(true);
        canClose = true;
    }

    private IEnumerator Punch(RectTransform target)
    {
        Vector3 baseScale = target.localScale;
        const float seconds = 0.15f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            target.localScale = baseScale * Mathf.Lerp(1.25f, 1f, t / seconds);
            yield return null;
        }
        target.localScale = baseScale;
    }

    // 다 나온 뒤 빈 곳 클릭(좌클릭) 또는 우클릭으로 닫기
    private void Update()
    {
        if (!canClose || leaving) return;
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) Close();
    }

    private void Close()
    {
        if (ScreenFader.IsBusy) return;
        leaving = true;

        // 손님 처리 완료는 Making에서 돌아올 때 이미 셌으므로 여기서는 화면만 넘긴다
        SettlementPopup.FinishDay(!(gameOver || lastDay)); // 다음 날 Counter, 게임 종료면 Result (WIN / LOSE)

        Destroy(gameObject); // 화면 전환(Fade)보다 위에 남지 않게
    }

    // ---- 임시 효과음 "띠링" (값마다 조금씩 높아짐) ----

    private void PlayDing(int index)
    {
        if (audioSource == null) return;
        float freq = 1046.5f * Mathf.Pow(1.122f, index); // C6부터 한 음씩 위로
        if (!dings.TryGetValue(freq, out var clip)) dings[freq] = clip = Ding(freq);
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private static AudioClip Ding(float freq)
    {
        const int rate = 44100;
        const float noteSec = 0.09f;
        int perNote = (int)(rate * noteSec);
        float[] freqs = { freq, freq * 1.5f };
        var data = new float[perNote * freqs.Length];
        for (int n = 0; n < freqs.Length; n++)
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Min(1f, i / (rate * 0.004f)) * (1f - (float)i / perNote);
                data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * 0.5f;
            }
        var clip = AudioClip.Create("sfx_settlement_ding_temp", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
