using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화 기록 내용 채우기 (hermit). Hook의 Making Note 프리팹 대화 탭(Tab 2 - Dialogue)에 붙는다.
/// 대화가 길면 노트 안에서 위아래로 스크롤해 볼 수 있다 (스크롤 구조는 실행 중에 갖춘다).
/// 팝업이 열릴 때 GameSession의 대화 기록 중 지금 주문(키링을 만들고 있는 손님)에서 나눈 대화만 읽어 한 줄씩 만든다.
/// 줄 모양은 프리팹 Content 안의 첫 번째 예시 줄(이름·아이콘·대사)을 복제해서 쓴다.
/// </summary>
public class DialogueHistoryView : MonoBehaviour
{
    [Tooltip("한 줄 원본 (비우면 Content 안의 첫 번째 예시 줄, 없으면 꺼져 있는 Dialogue Line Layout)")]
    [SerializeField] private GameObject lineTemplate;
    [Tooltip("줄을 넣을 곳 (비우면 ScrollRect.content)")]
    [SerializeField] private RectTransform content;
    [SerializeField] private string emptyMessage = "No conversation yet.";
    [Tooltip("열렸을 때 가장 최근 대화가 보이도록 맨 아래로 스크롤")]
    [SerializeField] private bool scrollToLatest = true;

    [Header("스크롤 (대화가 길면 노트 안에서 위아래로 넘겨 보기)")]
    [Tooltip("대화가 보이는 영역 높이 (노트 줄 영역 안에 들어오게). Content의 부모(Viewport)에 적용")]
    [SerializeField, Min(50f)] private float viewportHeight = 420f;
    [SerializeField, Min(1f)] private float scrollSensitivity = 40f;

    private ScrollRect scroll;
    private readonly System.Collections.Generic.List<GameObject> spawnedLines = new System.Collections.Generic.List<GameObject>();

    void OnEnable()
    {
        StartCoroutine(OnEnableCoroutine());
    }

    void OnDisable()
    {
        StopCoroutine(OnEnableCoroutine());
    }

    IEnumerator OnEnableCoroutine()
    {
        if (content == null)
        {
            var existing = GetComponentInChildren<ScrollRect>(true);
            if (existing != null) content = existing.content;
        }
        if (content == null)
        {
            Debug.LogWarning("[DialogueHistory] 줄을 넣을 Content를 찾지 못했습니다.");
            yield break;
        }
        if (lineTemplate == null)
        {
            Debug.LogError("[DialogueHistory] 줄 원본(Dialogue Line Layout)을 찾지 못했습니다.");
            yield break;
        }

        EnsureScroll();

        // 다시 열릴 때(탭 전환 등) 이전에 만든 줄이 쌓이지 않게 지우고 새로 만든다
        foreach (var old in spawnedLines)
            if (old != null) { old.SetActive(false); old.transform.SetParent(null, false); Destroy(old); }
        spawnedLines.Clear();

        var log = GameSession.CurrentOrderDialogue();
        if (log.Count == 0)
            AddLine("", emptyMessage, null);
        else
            foreach (var entry in log) AddLine(entry.speaker, entry.text, entry.portrait);

        // 레이아웃이 다시 계산된 뒤 맨 아래(가장 최근 대화)로 스크롤
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null;
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = scrollToLatest ? 0f : 1f;
        }
    }

    /// <summary>
    /// 대화가 길어도 노트 밖으로 삐져나오지 않고 위아래로 스크롤해 볼 수 있게, 실행 중에 스크롤 구조를 갖춘다 (프리팹 파일은 그대로).
    /// Content의 부모(Viewport)는 높이를 viewportHeight로 고정하고(Mask로 영역 밖은 잘림), Content는 Viewport 너비에 맞춰 줄바꿈되며
    /// 세로로만 늘어나게 하고, Viewport에 ScrollRect를 붙여 마우스 휠·드래그로 넘겨 볼 수 있게 한다.
    /// </summary>
    private void EnsureScroll()
    {
        var viewport = content.parent as RectTransform;
        if (viewport == null) return;

        scroll = viewport.GetComponent<ScrollRect>();
        if (scroll != null && scroll.content == content) return; // 이미 갖춤

        // Viewport: 내용에 맞춰 커지던 설정을 끄고 높이를 고정
        var viewportFitter = viewport.GetComponent<ContentSizeFitter>();
        if (viewportFitter != null) viewportFitter.enabled = false;
        var viewportLayout = viewport.GetComponent<VerticalLayoutGroup>();
        float sidePadding = viewportLayout != null ? viewportLayout.padding.left : 16f;
        if (viewportLayout != null) viewportLayout.enabled = false; // Content 위치·크기는 ScrollRect가 다룬다
        var element = viewport.GetComponent<LayoutElement>();
        if (element == null) element = viewport.gameObject.AddComponent<LayoutElement>();
        element.minHeight = element.preferredHeight = viewportHeight;
        element.flexibleHeight = 0f;
        if (viewport.GetComponent<Mask>() == null && viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        // Content: Viewport 너비에 맞춰 줄바꿈, 세로로만 내용만큼
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(sidePadding, content.offsetMin.y);
        content.offsetMax = new Vector2(-sidePadding, content.offsetMax.y);
        content.anchoredPosition = new Vector2(0f, 0f);
        var contentFitter = content.GetComponent<ContentSizeFitter>();
        if (contentFitter == null) contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (scroll == null) scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = scrollSensitivity;
    }

    private void AddLine(string speaker, string text, Sprite portrait)
    {
        var line = Instantiate(lineTemplate, content, false);
        line.SetActive(true);
        spawnedLines.Add(line);

        foreach (var t in line.GetComponentsInChildren<TMP_Text>(true))
            t.text = t.name == "Name Text" ? speaker : text;

        // 아이콘: 초상화가 있으면 넣는다 (UI Extensions UICircle 등 sprite 속성이 있는 그래픽)
        var icon = line.transform.Find("Icon Layout/Icon");
        if (icon != null && portrait != null)
        {
            foreach (var graphic in icon.GetComponents<Graphic>())
            {
                if (graphic is Image image) { image.sprite = portrait; continue; }
                var property = graphic.GetType().GetProperty("sprite");
                if (property != null && property.CanWrite && property.PropertyType == typeof(Sprite))
                    property.SetValue(graphic, portrait);
            }
        }
    }
}
