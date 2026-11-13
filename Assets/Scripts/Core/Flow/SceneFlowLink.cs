using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬과 SceneFlow를 잇는 hermit 연결 컴포넌트. 팀원 스크립트·오브젝트는 수정하지 않는다.
/// - Counter: 시작 손님 번호를 DialogueManager에 넣고, 씬을 떠날 때(팀원 코드가 직접 Making을 불러도) 손님 번호를 기억한다.
///   오른쪽 위에 돈 표시 UI(Resources/CounterMoneyBar)를 띄우고, 하루 마지막 손님이면 팁 대사를 클릭한 뒤 하루 정산 팝업을 연다.
/// - Making: 팀원 Submit 버튼(Result.Result_Kiring)에 실행 중에만 ReturnFromMaking을 붙인다.
///   지금 손님 번호에 맞는 레시피(customerReceipts)를 팀원 판정(Result.receipt_data)에 넣는다.
/// - 버튼 OnClick에서 SceneFlow 함수를 부르는 용도로도 쓴다.
/// </summary>
public class SceneFlowLink : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogue;

    [Tooltip("Making: 팀원 Submit 버튼(Result_Kiring을 부르는 버튼)을 찾아 실행 중에 ReturnFromMaking을 연결")]
    [SerializeField] private bool hookResultSubmit;

    [Tooltip("Making: 손님 번호(DialogueManager.customer) 순서대로 판정할 레시피. 비어 있으면 Result에 넣어 둔 기본 레시피를 쓴다")]
    [SerializeField] private Receipt_Data[] customerReceipts;

    [Tooltip("Counter: 하루 마지막 팁 대사를 클릭해 대사·초상화가 사라진 뒤 정산 팝업을 열기까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float settlementDelay = 0.8f;

    [Tooltip("Making: 보드에 아무것도 없이 Submit을 눌렀을 때 보여 줄 문구")]
    [SerializeField] private string emptyBoardMessage = "Make a keyring first!";
    [SerializeField, Min(0.1f)] private float emptyBoardHintSeconds = 1.5f;
    [Tooltip("Submit 버튼 위쪽 끝에서 안내 말풍선 중심까지의 거리 (말풍선 높이 110의 절반 + 여백). 버튼이 어디로 옮겨져도 버튼 바로 위 가운데에 뜬다")]
    [SerializeField] private float emptyBoardHintGap = 70f;

    private const string CounterMoneyBarPath = "CounterMoneyBar"; // Resources/CounterMoneyBar.prefab

    private int cachedCustomer = -1;
    private int cachedCustomerCount;
    private bool isCounter;
    private bool settlementOpened;
    private const string RestartDialogueMethod = "ReStartDialogue"; // DialogueManager가 대사를 닫은 뒤 Invoke로 예약하는 메서드

    private void Awake()
    {
        if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        // DialogueManager.Start보다 먼저 실행되어 다음 손님부터 대화를 시작하게 한다.
        if (dialogue != null && SceneFlow.NextCustomerIndex > 0 && dialogue.dialogueData != null
            && SceneFlow.NextCustomerIndex < dialogue.dialogueData.Length)
            dialogue.customer = SceneFlow.NextCustomerIndex;

        if (hookResultSubmit) HookSubmitButtons();
        ApplyCustomerReceipt();

        isCounter = gameObject.scene.name == SceneFlow.CounterScene;
        if (isCounter)
        {
            SpawnCounterMoneyBar();
            FillReactionSpeakerName();
        }
    }

    // 손님 순서별 이름 (대화 데이터에 이름이 비어 있을 때만 사용)
    private static readonly string[] customerNames = { "Candy", "Biskuit", "Licorice" };

    /// <summary>
    /// 키링 제출 뒤 Counter의 반응(팁) 대사에 손님 이름이 나오게 한다.
    /// 팀원 DialogueManager.LastDialogue는 이름을 line.character_Name에서 읽는데, 씬이 새로 열리면 line이 비어 있어 이름이 빈칸이 된다.
    /// DialogueManager.Start(→ LastDialogue)보다 먼저 지금 손님의 이름을 line에 넣어 둔다 (팀원 코드는 수정하지 않음).
    /// </summary>
    private void FillReactionSpeakerName()
    {
        if (dialogue == null || Customer_Data.isfinish) return; // isfinish == false일 때만 반응 대사
        if (dialogue.line == null) dialogue.line = new DialogueLine();
        if (!string.IsNullOrEmpty(dialogue.line.character_Name)) return;

        int customer = Customer_Data.customer; // DialogueManager.Start가 같은 값을 쓴다
        string name = null;
        var data = dialogue.dialogueData;
        if (data != null && customer >= 0 && customer < data.Length && data[customer] != null && data[customer].dialogueLines != null)
        {
            foreach (var l in data[customer].dialogueLines)
                if (l != null && !string.IsNullOrEmpty(l.character_Name)) { name = l.character_Name; break; }
        }
        if (string.IsNullOrEmpty(name) && customer >= 0 && customer < customerNames.Length) name = customerNames[customer];
        if (!string.IsNullOrEmpty(name)) dialogue.line.character_Name = name;
    }

    // Counter 씬 파일은 건드리지 않고 실행 중에 돈 표시 UI를 붙인다 (씬 오브젝트라 씬을 떠나면 함께 사라짐)
    private void SpawnCounterMoneyBar()
    {
        var prefab = Resources.Load<GameObject>(CounterMoneyBarPath);
        if (prefab == null) return;
        var bar = Instantiate(prefab);
        bar.name = prefab.name;
    }

    private void Update()
    {
        // 하루 마지막 손님: Counter로 돌아와 나온 팁(반응) 대사를 클릭하면 하루 정산 팝업.
        // 팁 대사를 클릭하면 팀원 DialogueManager.EndDialogue가 대사를 닫고 2초 뒤 다음 대화(ReStartDialogue)를 예약한다.
        // 그 예약이 생긴 순간을 '팁 대사 클릭'으로 보고 예약은 바로 취소한 뒤,
        // 대사창·초상화가 사라진 빈 화면을 잠깐(settlementDelay) 보여 주고 팝업을 연다 (팀원 코드는 수정하지 않음).
        if (!isCounter || settlementOpened || !GameSession.SettlementPending) return;
        if (dialogue == null || dialogue.IsInvoking(RestartDialogueMethod)) OpenSettlement();
    }

    private void OpenSettlement()
    {
        settlementOpened = true;
        if (dialogue != null) dialogue.CancelInvoke(); // 대사가 끝난 뒤 예약된 다음 대화 시작(ReStartDialogue)이 팝업 뒤에서 돌지 않게
        StartCoroutine(OpenSettlementAfterDelay());
    }

    private System.Collections.IEnumerator OpenSettlementAfterDelay()
    {
        // 기다리는 동안 책상 클릭으로 Making에 들어가지 않게 잠시 끈다 (팝업이 열리면 팝업이 다시 막는다)
        var desks = FindObjectsByType<DeskInteraction>(FindObjectsSortMode.None);
        var disabled = new System.Collections.Generic.List<DeskInteraction>();
        foreach (var d in desks)
            if (d.enabled) { d.enabled = false; disabled.Add(d); }

        if (settlementDelay > 0f) yield return new WaitForSecondsRealtime(settlementDelay);

        foreach (var d in disabled)
            if (d != null) d.enabled = true;
        SettlementPopup.OpenDaySettlement();
    }

    private void ApplyCustomerReceipt()
    {
        // Counter에서 GoToMaking/NoteCounterLeft가 지금 손님 번호를 기록한다
        int current = SceneFlow.NextCustomerIndex;
        if (customerReceipts == null || current < 0 || current >= customerReceipts.Length) return;
        if (customerReceipts[current] == null) return;
        var result = FindAnyObjectByType<Result>();
        if (result != null) result.receipt_data = customerReceipts[current];
    }

    private void LateUpdate()
    {
        // 씬을 떠날 때는 DialogueManager가 이미 파괴됐을 수 있어 값을 미리 기억해 둔다
        if (dialogue == null) return;
        cachedCustomer = dialogue.customer;
        cachedCustomerCount = dialogue.dialogueData != null ? dialogue.dialogueData.Length : 0;
    }

    private void OnDestroy()
    {
        // 팀원 코드가 SceneManager로 직접 씬을 바꿔도 손님 진행이 이어지도록 기록
        if (cachedCustomer >= 0) SceneFlow.NoteCounterLeft(cachedCustomer, cachedCustomerCount);
    }

    private void HookSubmitButtons()
    {
        // 결산 팝업이 있으면 Submit → 결산 → (팝업 버튼) → ReturnFromMaking, 없으면 바로 ReturnFromMaking.
        // 보드에 아무것도 없으면 제출하지 않도록, 팀원 판정(Result_Kiring) 연결은 이번 실행에서만 끄고
        // 확인 후 직접 부른다 (프리팹·씬 파일은 그대로).
        var settlement = FindAnyObjectByType<SettlementPopup>();
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentMethodName(i) == "Result_Kiring")
                {
                    var result = b.onClick.GetPersistentTarget(i) as Result;
                    if (result == null) break; // 알 수 없는 대상이면 원래 동작 그대로
                    b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    var button = b;
                    b.onClick.AddListener(() => Submit(button, result, settlement));
                    break;
                }
    }

    private void Submit(Button button, Result result, SettlementPopup settlement)
    {
        if (IsBoardEmpty())
        {
            ShowEmptyBoardHint(button.transform);
            return;
        }
        result.Result_Kiring(); // 팀원 판정
        if (settlement != null) settlement.Show();
        else ReturnFromMaking();
    }

    // 보드에 키링 본체(체인)가 없으면 아무것도 만들지 않은 것으로 본다
    private static bool IsBoardEmpty()
    {
        var manager = FindAnyObjectByType<ObjectDragManager>();
        return manager == null || manager.CurrentChain == null;
    }

    private void ShowEmptyBoardHint(Transform target)
    {
        var tooltip = GuideTooltip.Instance;
        if (tooltip == null) return;
        tooltip.Show(target, emptyBoardMessage, HintOffsetAbove(target));
        StopCoroutine(nameof(HideHintLater));
        StartCoroutine(nameof(HideHintLater));
    }

    // 말풍선은 대상의 가운데를 기준으로 offset만큼 떨어져 뜬다 (GuideTooltip, Canvas 1920x1080·Match 0.5).
    // 실행 중 버튼의 실제 화면 높이를 재서 '버튼 위쪽 끝 + gap' 위치가 되도록 offset을 만든다.
    private Vector2 HintOffsetAbove(Transform target)
    {
        float halfHeight = 0f;
        if (target is RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners); // 0 왼쪽 아래, 1 왼쪽 위
            float screenHeight = RectTransformUtility.WorldToScreenPoint(cam, corners[1]).y - RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y;
            float tooltipScale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(Screen.width / 1920f, 2f), Mathf.Log(Screen.height / 1080f, 2f), 0.5f));
            if (tooltipScale > 0f) halfHeight = Mathf.Abs(screenHeight) / tooltipScale * 0.5f;
        }
        return new Vector2(0f, halfHeight + emptyBoardHintGap);
    }

    private System.Collections.IEnumerator HideHintLater()
    {
        yield return new WaitForSecondsRealtime(emptyBoardHintSeconds);
        if (GuideTooltip.Instance != null) GuideTooltip.Instance.Hide();
    }

    public void StartNewGame() => SceneFlow.StartNewGame();
    public void GoToMaking() => SceneFlow.GoToMaking(dialogue);
    public void ReturnFromMaking() => SceneFlow.ReturnFromMaking();
    public void GoToTitle() => SceneFlow.GoToTitle();
}
