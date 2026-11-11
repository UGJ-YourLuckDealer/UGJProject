using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 유도 툴팁 서비스 (AudioService와 같은 방식). 어디서든 GuideTooltip.Instance로 불러 쓴다.
///     GuideTooltip.Instance.Show(대상 Transform, "문구");
///     GuideTooltip.Instance.Hide();
/// - 씬에 Guide Tooltip 프리팹이 있으면 그것을, 없으면 Resources/GuideTooltip 프리팹을 만들어 쓰고 씬이 바뀌어도 유지한다 (중복은 제거).
/// - 대상(UI 또는 월드 오브젝트) 근처에 말풍선을 띄우고 위아래로 살짝 움직이며 따라간다. 클릭을 받지 않아 뒤의 대상 클릭을 막지 않는다.
/// - showOnDialogueEnd를 켜 두면 씬의 DialogueManager 마지막 대사(isEnd)가 다 출력됐을 때 targetName 대상(Counter 책상) 위에 자동으로 뜬다.
/// 인스턴스 참조는 GameSession.GuideTooltip에 저장한다 (static 값은 GameSession에서 관리).
/// </summary>
public class GuideTooltip : MonoBehaviour
{
    private const string PrefabPath = "GuideTooltip"; // Resources/GuideTooltip.prefab

    public static GuideTooltip Instance
    {
        get
        {
            if (GameSession.GuideTooltip == null)
            {
                var found = FindAnyObjectByType<GuideTooltip>();
                if (found == null)
                {
                    var prefab = Resources.Load<GuideTooltip>(PrefabPath);
                    if (prefab == null)
                    {
                        Debug.LogWarning("[GuideTooltip] Resources/GuideTooltip 프리팹이 없습니다.");
                        return null;
                    }
                    found = Instantiate(prefab);
                    found.name = "GuideTooltip";
                }
                found.Register();
            }
            return GameSession.GuideTooltip;
        }
    }

    [Header("References (프리팹 안)")]
    [SerializeField] private RectTransform root;      // 자체 Canvas의 RectTransform
    [SerializeField] private RectTransform bubble;    // 움직이는 말풍선
    [SerializeField] private TMP_Text label;
    [SerializeField] private CanvasGroup group;

    [Header("Default")]
    [SerializeField] private string defaultMessage = "Make a keyring!";
    [Tooltip("대상 위치에서 말풍선까지 거리 (1920x1080 기준)")]
    [SerializeField] private Vector2 defaultOffset = new Vector2(0f, 180f);

    [Header("Auto (대화 끝 유도)")]
    [Tooltip("씬의 DialogueManager 마지막 대사가 다 출력되면 자동으로 표시")]
    [SerializeField] private bool showOnDialogueEnd = true;
    [Tooltip("자동 표시할 때 대상 오브젝트 이름 (Counter 책상)")]
    [SerializeField] private string dialogueEndTargetName = "Desk_Image";

    [Header("Motion")]
    [SerializeField, Min(0f)] private float bobHeight = 12f;
    [SerializeField, Min(0f)] private float bobSpeed = 3f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.2f;

    private Transform target;
    private Vector2 offset;
    private bool visible;
    private bool autoShown;
    private float bobTime;
    private DialogueManager dialogue;

    public bool IsVisible => visible;

    private void Awake()
    {
        if (GameSession.GuideTooltip != null && GameSession.GuideTooltip != this)
        {
            Destroy(gameObject); // 이미 유지 중인 툴팁이 있으면 새로 불러온 쪽 제거
            return;
        }
        Register();
        if (root == null) root = (RectTransform)transform;
        if (group != null) group.alpha = 0f;
        if (bubble != null) bubble.gameObject.SetActive(false);
        offset = defaultOffset;
    }

    private void Register()
    {
        if (GameSession.GuideTooltip == this) return;
        GameSession.GuideTooltip = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (GameSession.GuideTooltip == this) GameSession.GuideTooltip = null;
    }

    private void Start() => FindDialogue();

    // 씬이 바뀌면 숨기고, 새 씬의 대화를 다시 찾는다
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        HideImmediate();
        FindDialogue();
    }

    private void FindDialogue() => dialogue = showOnDialogueEnd ? FindAnyObjectByType<DialogueManager>() : null;

    /// <summary>대상 근처에 툴팁 표시. message를 비우면 기본 문구, offset을 비우면 기본 거리.</summary>
    public void Show(Transform newTarget, string message = null, Vector2? newOffset = null)
    {
        target = newTarget;
        offset = newOffset ?? defaultOffset;
        if (label != null) label.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
        if (!visible) bobTime = 0f;
        visible = true;
        if (bubble != null) bubble.gameObject.SetActive(true);
    }

    public void Hide()
    {
        visible = false;
        autoShown = false;
    }

    private void HideImmediate()
    {
        Hide();
        target = null;
        if (group != null) group.alpha = 0f;
        if (bubble != null) bubble.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (showOnDialogueEnd) UpdateDialogueEnd();

        if (group != null)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime / fadeSeconds);
            if (!visible && group.alpha <= 0f && bubble != null && bubble.gameObject.activeSelf)
                bubble.gameObject.SetActive(false);
        }

        if (bubble == null || !bubble.gameObject.activeSelf) return;

        // 대상 따라가기 + 위아래 흔들기 (대상이 사라지면 숨김)
        if (target == null && visible) { Hide(); return; }
        bobTime += Time.unscaledDeltaTime;
        Vector2 basePos = offset;
        if (target != null && TryGetLocalPoint(target, out var local)) basePos = local + offset;
        bubble.anchoredPosition = basePos + Vector2.up * (Mathf.Sin(bobTime * bobSpeed) * bobHeight);
    }

    // 마지막 대사가 끝까지 출력됐을 때만 자동 표시 (타이핑 중에는 숨김)
    private void UpdateDialogueEnd()
    {
        if (dialogue == null) return;
        var line = dialogue.line;
        bool ended = line != null && line.isEnd
            && dialogue.dialogueText != null && dialogue.dialogueText.text == line.dialogueText;

        if (ended && !visible)
        {
            var desk = GameObject.Find(dialogueEndTargetName);
            if (desk != null) { Show(desk.transform); autoShown = true; }
        }
        else if (!ended && autoShown)
        {
            Hide();
        }
    }

    // 대상 위치 → 이 툴팁 Canvas 안의 좌표 (대상이 UI든 월드 오브젝트든)
    private bool TryGetLocalPoint(Transform t, out Vector2 local)
    {
        local = Vector2.zero;
        Vector2 screen;
        if (t is RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            screen = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
        }
        else
        {
            var cam = Camera.main;
            if (cam == null) return false;
            screen = cam.WorldToScreenPoint(t.position);
        }
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out local);
    }
}
