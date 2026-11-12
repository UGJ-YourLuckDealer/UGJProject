using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬과 SceneFlow를 잇는 hermit 연결 컴포넌트. 팀원 스크립트·오브젝트는 수정하지 않는다.
/// - Counter: 시작 손님 번호를 DialogueManager에 넣고, 씬을 떠날 때(팀원 코드가 직접 Making을 불러도) 손님 번호를 기억한다.
///   오른쪽 위에 돈 표시 UI(Resources/CounterMoneyBar)를 띄운다.
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

    private const string CounterMoneyBarPath = "CounterMoneyBar"; // Resources/CounterMoneyBar.prefab

    private int cachedCustomer = -1;
    private int cachedCustomerCount;
    private bool isCounter;

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
        if (isCounter) SpawnCounterMoneyBar();
    }

    // Counter 씬 파일은 건드리지 않고 실행 중에 돈 표시 UI를 붙인다 (씬 오브젝트라 씬을 떠나면 함께 사라짐)
    private void SpawnCounterMoneyBar()
    {
        var prefab = Resources.Load<GameObject>(CounterMoneyBarPath);
        if (prefab == null) return;
        var bar = Instantiate(prefab);
        bar.name = prefab.name;
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
        // 결산 팝업이 있으면 Submit → 결산 → (팝업 버튼) → ReturnFromMaking, 없으면 바로 ReturnFromMaking
        var settlement = FindAnyObjectByType<SettlementPopup>();
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentMethodName(i) == "Result_Kiring")
                {
                    if (settlement != null) b.onClick.AddListener(settlement.Show); // 팀원 판정(Result_Kiring) 다음에 실행됨
                    else b.onClick.AddListener(ReturnFromMaking);
                    break;
                }
    }

    public void StartNewGame() => SceneFlow.StartNewGame();
    public void GoToMaking() => SceneFlow.GoToMaking(dialogue);
    public void ReturnFromMaking() => SceneFlow.ReturnFromMaking();
    public void GoToTitle() => SceneFlow.GoToTitle();
}
