using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 키링 제출 후 결산 팝업 (Making). Submit을 누르면 설정 화면과 같은 반투명 검은 패널이 깔리고
/// 오른쪽에서 가운데로 결산 상자(UI_ToolTipBox)가 들어온다. 수입·재료비·합계를 하나씩 효과음과 함께 보여 준 뒤
/// 다음 손님(Counter, 하루·게임이 끝나면 Result)으로 가는 버튼을 띄운다.
/// 금액은 이 씬에서 생긴 자본 변화(GameEvents.CapitalChanged)를 모아 계산한다: + 는 수입, - 는 재료비.
/// SceneFlowLink(hookResultSubmit)가 팀원 Submit 버튼에 실행 중에만 Show를 연결한다. UI는 실행 중에 만든다.
/// </summary>
public class SettlementPopup : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Sprite boxSprite;     // UI_ToolTipBox
    [SerializeField] private Sprite buttonSprite;  // UI_DefaultBox
    [Tooltip("커서를 올리거나 누를 때 바뀌는 버튼 이미지 (다른 버튼과 같은 UI_DefaultSelectedBox)")]
    [SerializeField] private Sprite buttonHighlightSprite;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.6f); // 설정 화면 패널과 같은 색
    [SerializeField] private Color textColor = new Color(0.30f, 0.20f, 0.15f, 1f);
    [SerializeField] private Color gainColor = new Color32(0x2E, 0x9E, 0x3A, 0xFF);
    [SerializeField] private Color lossColor = new Color32(0xD9, 0x3B, 0x2E, 0xFF);
    [SerializeField] private Vector2 boxSize = new Vector2(620f, 620f);

    [Header("Text")]
    [SerializeField] private string titleLabel = "Settlement";
    [SerializeField] private string incomeLabel = "Income";
    [SerializeField] private string expenseLabel = "Expense";
    [SerializeField] private string totalLabel = "Total";
    [SerializeField] private string nextCustomerLabel = "Next Customer";
    [SerializeField] private string resultLabel = "Result";
    [Tooltip("레시피를 완벽하게 맞춰 팁을 받았을 때 상자에 찍히는 도장 (팁 유무만 표시)")]
    [SerializeField] private string tipStamp = "Tip!";
    [SerializeField] private Color tipColor = new Color32(0xFF, 0x8C, 0x1A, 0xFF);

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float slideSeconds = 0.5f;
    [Tooltip("결산 값이 하나씩 나오는 간격")]
    [SerializeField, Min(0f)] private float lineInterval = 0.45f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.6f;

    private GameObject root;
    private Image dim;
    private RectTransform box;
    private RectTransform[] rows;
    private TMP_Text[] values;
    private Button nextButton;
    private TMP_Text nextLabel;
    private TMP_Text tipText;
    private bool hasTip;
    private AudioSource audioSource;

    private float lastCapital;
    private bool hasCapital;
    private float income;
    private float expense;
    private bool shown;

    private void Awake()
    {
        Build();
        root.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.CapitalChanged += OnCapitalChanged;
        GameEvents.KeyringJudged += OnKeyringJudged;
    }

    private void OnDisable()
    {
        GameEvents.CapitalChanged -= OnCapitalChanged;
        GameEvents.KeyringJudged -= OnKeyringJudged;
    }

    private void OnKeyringJudged(CustomerData customer, JudgementResult result)
    {
        if (!shown) hasTip = result.HasTip;
    }

    private void Start()
    {
        var capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null && !hasCapital) { lastCapital = capital.Current; hasCapital = true; }
    }

    private void OnCapitalChanged(float value)
    {
        if (!hasCapital) { lastCapital = value; hasCapital = true; return; }
        float delta = value - lastCapital;
        lastCapital = value;
        if (shown) return;
        if (delta > 0f) income += delta;
        else expense -= delta;
    }

    /// <summary>Submit 직후 호출 (팀원 판정 Result_Kiring이 수익을 반영한 다음).</summary>
    public void Show()
    {
        if (shown) return;
        shown = true;

        // 결산 중에는 뒤의 재료를 집거나 끌 수 없게 (실행 중에만 끄고, 다음 씬으로 넘어가면 사라짐)
        foreach (var m in FindObjectsByType<ObjectInteraction>(FindObjectsSortMode.None)) m.enabled = false;
        foreach (var m in FindObjectsByType<ObjectDragManager>(FindObjectsSortMode.None)) m.enabled = false;

        float total = income - expense;
        SetValue(0, "+" + Money(income), gainColor);
        SetValue(1, "-" + Money(expense), lossColor);
        SetValue(2, (total >= 0f ? "+" : "-") + Money(Mathf.Abs(total)), total >= 0f ? gainColor : lossColor);
        nextLabel.text = WillEndAfterThis() ? resultLabel : nextCustomerLabel;

        StartCoroutine(Play());
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
        values[i].text = text;
        values[i].color = color;
    }

    private IEnumerator Play()
    {
        root.SetActive(true);
        foreach (var r in rows) r.gameObject.SetActive(false);
        tipText.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);

        // 어두운 패널 + 오른쪽에서 가운데로 들어오는 상자
        Vector2 from = new Vector2(1400f, 0f);
        for (float t = 0f; t < slideSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / slideSeconds;
            dim.color = new Color(dimColor.r, dimColor.g, dimColor.b, dimColor.a * Mathf.Clamp01(k * 2f));
            box.anchoredPosition = Vector2.LerpUnclamped(from, Vector2.zero, 1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }
        dim.color = dimColor;
        box.anchoredPosition = Vector2.zero;

        // 결산 값을 하나씩 (띠링)
        for (int i = 0; i < rows.Length; i++)
        {
            yield return new WaitForSecondsRealtime(lineInterval);
            rows[i].gameObject.SetActive(true);
            PlayDing(i);
            yield return Punch(rows[i]);
        }

        if (hasTip)
        {
            yield return new WaitForSecondsRealtime(lineInterval);
            tipText.gameObject.SetActive(true);
            PlayDing(rows.Length);
            yield return Punch(tipText.rectTransform);
        }

        yield return new WaitForSecondsRealtime(lineInterval);
        nextButton.gameObject.SetActive(true);
        yield return Punch((RectTransform)nextButton.transform);
    }

    private IEnumerator Punch(RectTransform target)
    {
        const float seconds = 0.15f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            target.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, t / seconds);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private void OnNextClicked()
    {
        nextButton.interactable = false;
        SceneFlow.ReturnFromMaking(); // 손님 처리 완료 → Counter (하루·게임이 끝났으면 Result)
    }

    // ---- UI 생성 (1920x1080 기준) ----

    private void Build()
    {
        root = new GameObject("SettlementCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // CapitalUI(50)·Day(60) 위, 설정 UI(100) 아래
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        dim = NewImage("Dim", root.transform, null, dimColor);
        Stretch(dim.rectTransform);
        dim.raycastTarget = true; // 뒤의 Submit 등 UI 클릭 막기

        var boxImage = NewImage("Box", root.transform, boxSprite, Color.white);
        box = boxImage.rectTransform;
        box.sizeDelta = boxSize;

        var title = NewText("Title", box, titleLabel, 60f, TextAlignmentOptions.Center, textColor);
        Place(title.rectTransform, new Vector2(0f, 205f), new Vector2(500f, 90f));

        string[] labels = { incomeLabel, expenseLabel, totalLabel };
        rows = new RectTransform[labels.Length];
        values = new TMP_Text[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            var row = new GameObject("Row_" + labels[i], typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(box, false);
            Place(row, new Vector2(0f, 90f - i * 90f), new Vector2(480f, 80f));
            var label = NewText("Label", row, labels[i], 46f, TextAlignmentOptions.Left, textColor);
            Place(label.rectTransform, new Vector2(-120f, 0f), new Vector2(240f, 80f));
            values[i] = NewText("Value", row, "", 46f, TextAlignmentOptions.Right, textColor);
            Place(values[i].rectTransform, new Vector2(120f, 0f), new Vector2(240f, 80f));
            if (i == labels.Length - 1) { label.fontStyle = FontStyles.Bold; values[i].fontStyle = FontStyles.Bold; }
            rows[i] = row;
        }

        tipText = NewText("TipStamp", box, tipStamp, 54f, TextAlignmentOptions.Center, tipColor);
        tipText.fontStyle = FontStyles.Bold;
        Place(tipText.rectTransform, new Vector2(215f, 225f), new Vector2(200f, 80f));
        tipText.rectTransform.localEulerAngles = new Vector3(0f, 0f, -15f);

        var buttonImage = NewImage("NextButton", box, buttonSprite, Color.white);
        Place(buttonImage.rectTransform, new Vector2(0f, -205f), new Vector2(380f, 110f));
        buttonImage.raycastTarget = true; // 클릭을 받아야 하는 유일한 이미지 (NewImage는 기본으로 꺼 둠)
        nextButton = buttonImage.gameObject.AddComponent<Button>();
        nextButton.targetGraphic = buttonImage;
        if (buttonHighlightSprite != null)
        {
            // Title·설정 버튼과 같은 연출: 커서를 올리면 강조 박스로 바뀜 (+ UIHoverScale 확대, ButtonClickDelay 눌림 지연)
            nextButton.transition = Selectable.Transition.SpriteSwap;
            nextButton.spriteState = new SpriteState
            {
                highlightedSprite = buttonHighlightSprite,
                pressedSprite = buttonHighlightSprite,
                selectedSprite = buttonHighlightSprite,
            };
        }
        nextButton.onClick.AddListener(OnNextClicked);
        buttonImage.gameObject.AddComponent<UIHoverScale>();
        nextLabel = NewText("Text", buttonImage.rectTransform, nextCustomerLabel, 40f, TextAlignmentOptions.Center, textColor);
        Stretch(nextLabel.rectTransform);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // ---- 임시 효과음 "띠링" (값마다 조금씩 높아짐) ----

    private void PlayDing(int index)
    {
        if (audioSource == null) return;
        float baseFreq = 1046.5f * Mathf.Pow(1.122f, index); // C6부터 한 음씩 위로
        audioSource.PlayOneShot(Ding(baseFreq), sfxVolume);
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
