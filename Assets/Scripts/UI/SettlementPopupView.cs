using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정산 팝업 (Settlement Popup 프리팹, ResourceDB 키 "popup_making_settlement").
/// PopupService.ShowPopup으로 열리면 GameSession의 정산 값(수입·재료비·팁)을 읽어,
/// 결산 상자가 오른쪽에서 가운데로 들어온 뒤 Income / Expense / Total을 하나씩 효과음과 함께 보여 주고
/// 다음 손님(Counter, 하루·게임이 끝나면 Result)으로 가는 버튼을 띄운다.
/// 모양·위치는 프리팹에서 바꾸면 되고, 아래 참조만 연결돼 있으면 된다.
/// </summary>
public class SettlementPopupView : MonoBehaviour
{
    [Header("References (프리팹 안)")]
    [SerializeField] private Image dim;
    [SerializeField] private RectTransform box;
    [Tooltip("Income, Expense, Total 순서")]
    [SerializeField] private RectTransform[] rows = new RectTransform[3];
    [Tooltip("Income, Expense, Total 값 텍스트 (rows와 같은 순서)")]
    [SerializeField] private TMP_Text[] values = new TMP_Text[3];
    [SerializeField] private GameObject tipStamp;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextLabel;
    [SerializeField] private AudioSource audioSource;

    [Header("Text·Color")]
    [SerializeField] private string nextCustomerLabel = "Next Customer";
    [SerializeField] private string resultLabel = "Result";
    [SerializeField] private Color gainColor = new Color32(0x2E, 0x9E, 0x3A, 0xFF);
    [SerializeField] private Color lossColor = new Color32(0xD9, 0x3B, 0x2E, 0xFF);

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float slideSeconds = 0.5f;
    [SerializeField] private float slideFromX = 1400f;
    [Tooltip("결산 값이 하나씩 나오는 간격")]
    [SerializeField, Min(0f)] private float lineInterval = 0.45f;
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
    private bool leaving;

    private void Awake()
    {
        if (dim != null) dimColor = dim.color;
        if (box != null) boxPosition = box.anchoredPosition;
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
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
        float income = GameSession.CustomerIncome;
        float expense = GameSession.CustomerExpense;
        float total = income - expense;
        SetValue(0, "+" + Money(income), gainColor);
        SetValue(1, "-" + Money(expense), lossColor);
        SetValue(2, (total >= 0f ? "+" : "-") + Money(Mathf.Abs(total)), total >= 0f ? gainColor : lossColor);
        if (nextLabel != null) nextLabel.text = WillEndAfterThis() ? resultLabel : nextCustomerLabel;

        StartCoroutine(Play(GameSession.CustomerTip));
    }

    private bool WillEndAfterThis()
    {
        var capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null && !GameFlow.IsWin(capital.Current)) return true; // 자본 < 0 → 게임 오버
        var flow = FindAnyObjectByType<GameFlowManager>();
        return flow != null && flow.CustomersFinished + 1 >= flow.CustomersPerGame; // 오늘 마지막 손님
    }

    private static string Money(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    private void SetValue(int i, string text, Color color)
    {
        if (i >= values.Length || values[i] == null) return;
        values[i].text = text;
        values[i].color = color;
    }

    private IEnumerator Play(bool hasTip)
    {
        foreach (var r in rows) if (r != null) r.gameObject.SetActive(false);
        if (tipStamp != null) tipStamp.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        // 어두운 패널 + 오른쪽에서 가운데로 들어오는 상자
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

        // 결산 값을 하나씩 (띠링)
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null) continue;
            yield return new WaitForSecondsRealtime(lineInterval);
            rows[i].gameObject.SetActive(true);
            PlayDing(i);
            yield return Punch(rows[i]);
        }

        if (hasTip && tipStamp != null)
        {
            yield return new WaitForSecondsRealtime(lineInterval);
            tipStamp.SetActive(true);
            PlayDing(rows.Length);
            yield return Punch((RectTransform)tipStamp.transform);
        }

        if (nextButton != null)
        {
            yield return new WaitForSecondsRealtime(lineInterval);
            nextButton.gameObject.SetActive(true);
            yield return Punch((RectTransform)nextButton.transform);
        }
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

    private void OnNextClicked()
    {
        if (leaving) return;
        leaving = true;
        nextButton.interactable = false;
        SceneFlow.ReturnFromMaking(); // 손님 처리 완료 → Counter (하루·게임이 끝났으면 Result)
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
