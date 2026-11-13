using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대화 기록 팝업 내용 채우기 (hermit). Hook의 Dialogue History Popup 프리팹 루트에 붙는다.
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
        var scroll = GetComponent<ScrollRect>();
        if (content == null && scroll != null) content = scroll.content;
        if (content == null)
        {
            Debug.LogWarning("[DialogueHistory] 줄을 넣을 Content를 찾지 못했습니다.");
            yield break;
        }

        // 원본: Content 안의 첫 번째 예시 줄(Hook이 꾸민 모양)을 꺼진 복사본으로 보관
        if (lineTemplate != null)
        {
            lineTemplate.SetActive(false);
        }
        else
        {
            Debug.LogError("[DialogueHistory] 줄 원본(Dialogue Line Layout)을 찾지 못했습니다.");
            yield break;
        }

        // // 프리팹에 들어 있던 예시 줄 지우기 (부모에서 바로 떼어 레이아웃 계산에서 빠지게)
        // for (int i = content.childCount - 1; i >= 0; i--)
        // {
        //     var sample = content.GetChild(i).gameObject;
        //     sample.SetActive(false);
        //     sample.transform.SetParent(null, false);
        //     Destroy(sample);
        // }

        var log = GameSession.CurrentOrderDialogue();
        if (log.Count == 0)
            AddLine("", emptyMessage, null);
        else
            foreach (var entry in log) AddLine(entry.speaker, entry.text, entry.portrait);

        // 레이아웃이 다시 계산된 뒤 맨 아래(가장 최근 대화)로 스크롤
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null;
        if (scrollToLatest && scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    private void AddLine(string speaker, string text, Sprite portrait)
    {
        var line = Instantiate(lineTemplate, content, false);
        line.SetActive(true);

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
