using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬과 SceneFlow를 잇는 hermit 연결 컴포넌트. 팀원 스크립트·오브젝트는 수정하지 않는다.
/// - Counter: 시작 손님 번호를 DialogueManager에 넣고, 씬을 떠날 때(팀원 코드가 직접 Making을 불러도) 다음 손님 번호를 기억한다.
/// - Making: 팀원 Submit 버튼(Result.Result_Kiring)에 실행 중에만 ReturnFromMaking을 붙인다.
/// - 버튼 OnClick에서 SceneFlow 함수를 부르는 용도로도 쓴다.
/// </summary>
public class SceneFlowLink : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogue;

    [Tooltip("Making: 팀원 Submit 버튼(Result_Kiring을 부르는 버튼)을 찾아 실행 중에 ReturnFromMaking을 연결")]
    [SerializeField] private bool hookResultSubmit;

    private int cachedCustomer = -1;
    private int cachedCustomerCount;

    private void Awake()
    {
        if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        // DialogueManager.Start보다 먼저 실행되어 다음 손님부터 대화를 시작하게 한다.
        if (dialogue != null && SceneFlow.NextCustomerIndex > 0 && dialogue.dialogueData != null
            && SceneFlow.NextCustomerIndex < dialogue.dialogueData.Length)
            dialogue.customer = SceneFlow.NextCustomerIndex;

        if (hookResultSubmit) HookSubmitButtons();
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
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentMethodName(i) == "Result_Kiring")
                {
                    b.onClick.AddListener(ReturnFromMaking); // 팀원 판정(Result_Kiring) 다음에 실행됨
                    break;
                }
    }

    public void StartNewGame() => SceneFlow.StartNewGame();
    public void GoToMaking() => SceneFlow.GoToMaking(dialogue);
    public void ReturnFromMaking() => SceneFlow.ReturnFromMaking();
    public void GoToTitle() => SceneFlow.GoToTitle();
}
