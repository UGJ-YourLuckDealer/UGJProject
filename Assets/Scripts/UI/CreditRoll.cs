using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 크레딧 팝업 (Credit Popup 프리팹, ResourceDB 키 "popup_title_credit"). Title의 Credit 버튼으로 연다.
/// 어두운 화면에서 제목·역할·이름이 아래에서 위로 천천히 흘러가고, 끝까지 올라가면 자동으로 닫힌다.
/// 클릭·우클릭·ESC로 언제든 닫을 수 있다. 내용(sections)은 프리팹 인스펙터에서 고친다.
/// </summary>
public class CreditRoll : MonoBehaviour
{
    [Serializable]
    public class Section
    {
        public string role;
        [TextArea(1, 4)] public string[] names;
    }

    [Header("References (프리팹 안)")]
    [SerializeField] private RectTransform root;     // 화면 전체
    [SerializeField] private RectTransform content;  // 흘러가는 글 묶음

    [Header("Content")]
    [SerializeField] private string title = "Credits";
    [SerializeField] private Section[] sections =
    {
        new Section { role = "Team", names = new[] { "Luckers" } },
        new Section { role = "Developer", names = new[] { "hermit", "macro_t0713", "Hook" } },
        new Section { role = "Game Design", names = new[] { "Hook" } },
        new Section { role = "Art", names = new[] { "Gaeun Park", "Sun Kim (k.sun.artist@gmail.com)" } },
        new Section { role = "Music", names = new[] { "\"School Time\" by 東月 (Dongnguyet)" } },
    };
    [SerializeField] private string closing = "Thank you for playing!";

    [Header("Style")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color roleColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color nameColor = Color.white;
    [SerializeField] private float titleSize = 72f;
    [SerializeField] private float roleSize = 34f;
    [SerializeField] private float nameSize = 46f;
    [SerializeField] private float lineGap = 14f;
    [SerializeField] private float sectionGap = 80f;

    [Header("Motion")]
    [Tooltip("흘러가는 속도 (1920x1080 기준 초당 픽셀)")]
    [SerializeField, Min(10f)] private float scrollSpeed = 70f;
    [Tooltip("아래 화살표를 누르고 있는 동안 곱할 속도 배율")]
    [SerializeField, Min(1f)] private float fastMultiplier = 3f;
    [Tooltip("열린 직후 이 시간 동안은 클릭으로 닫히지 않음 (여는 클릭에 바로 닫히지 않게)")]
    [SerializeField, Min(0f)] private float closeGuardSeconds = 0.5f;

    private float contentHeight;
    private float elapsed;
    private bool isClosing;

    private void Start()
    {
        if (root == null) root = (RectTransform)transform;
        Build();
        // 화면 아래에서 시작
        content.anchoredPosition = new Vector2(0f, -root.rect.height * 0.5f - 20f);
    }

    private void Build()
    {
        float y = 0f;
        y = AddLine(title, titleSize, titleColor, FontStyles.Bold, y) - sectionGap;
        foreach (var section in sections)
        {
            y = AddLine(section.role, roleSize, roleColor, FontStyles.Normal, y) - lineGap;
            foreach (var n in section.names) y = AddLine(n, nameSize, nameColor, FontStyles.Bold, y) - lineGap;
            y -= sectionGap;
        }
        if (!string.IsNullOrEmpty(closing)) y = AddLine(closing, roleSize, titleColor, FontStyles.Italic, y);
        contentHeight = -y;
    }

    private float AddLine(string text, float size, Color color, FontStyles style, float y)
    {
        var go = new GameObject("Line", typeof(RectTransform));
        go.transform.SetParent(content, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        float h = size * 1.3f;
        var rect = t.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(1400f, h);
        rect.anchoredPosition = new Vector2(0f, y);
        return y - h;
    }

    private void Update()
    {
        if (isClosing) return;
        elapsed += Time.unscaledDeltaTime;

        // 아래에서 위로 천천히 (아래 화살표를 누르고 있으면 빠르게)
        var keyboard = Keyboard.current;
        bool fast = keyboard != null && keyboard.downArrowKey.isPressed;
        float speed = fast ? scrollSpeed * fastMultiplier : scrollSpeed;
        content.anchoredPosition += Vector2.up * (speed * Time.unscaledDeltaTime);

        // 글이 화면 위로 다 지나가면 자동으로 닫기
        if (content.anchoredPosition.y - contentHeight > root.rect.height * 0.5f) { Close(); return; }

        if (elapsed < closeGuardSeconds) return;
        var mouse = Mouse.current;
        bool pressed = (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                    || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
        if (pressed) Close();
    }

    private void Close()
    {
        isClosing = true;
        Destroy(gameObject); // PopupService는 사라진 팝업을 닫힌 것으로 본다
    }
}
