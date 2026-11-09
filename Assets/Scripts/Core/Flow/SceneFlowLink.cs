using UnityEngine;

/// <summary>
/// 씬의 버튼(OnClick)에서 SceneFlow를 호출하기 위한 연결 컴포넌트.
/// Counter에 두면 Awake에서 DialogueManager의 시작 손님 번호를 이어받게 한다.
/// </summary>
public class SceneFlowLink : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogue;

    private void Awake()
    {
        if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        // DialogueManager.Start보다 먼저 실행되어 다음 손님부터 대화를 시작하게 한다.
        if (dialogue != null && SceneFlow.NextCustomerIndex > 0 && dialogue.dialogueData != null
            && SceneFlow.NextCustomerIndex < dialogue.dialogueData.Length)
            dialogue.customer = SceneFlow.NextCustomerIndex;
    }

    public void StartNewGame() => SceneFlow.StartNewGame();
    public void GoToMaking() => SceneFlow.GoToMaking(dialogue);
    public void ReturnFromMaking() => SceneFlow.ReturnFromMaking();
    public void GoToTitle() => SceneFlow.GoToTitle();
}
