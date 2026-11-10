using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼을 누르면 delay초 뒤에 원래 OnClick을 실행한다 (눌림 연출을 보여 주고, 연타를 막는다).
/// 다른 스크립트가 Awake에서 AddListener로 붙인 동작까지 함께 지연되도록 가장 늦게 실행한다.
/// 일시정지(timeScale = 0) 중에도 동작하도록 실제 시간 기준으로 기다린다.
/// </summary>
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Button))]
public class ButtonClickDelay : MonoBehaviour
{
    [SerializeField, Min(0f)] private float delay = 0.15f;

    private Button button;
    private Button.ButtonClickedEvent delayedClick;
    private Coroutine pending;

    private void Awake()
    {
        button = GetComponent<Button>();
        delayedClick = button.onClick;                 // 씬에서 연결한 동작 + Awake에서 붙인 동작
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        if (pending != null) return;                   // 지연 중 연타 무시
        pending = StartCoroutine(InvokeAfterDelay());
    }

    private IEnumerator InvokeAfterDelay()
    {
        button.interactable = false;
        yield return new WaitForSecondsRealtime(delay);
        Fire();
    }

    private void Fire()
    {
        pending = null;
        if (button != null) button.interactable = true;
        delayedClick.Invoke();
    }

    // 지연 중에 버튼이 꺼지면(씬 전환 등) 클릭을 취소하고 다시 누를 수 있게 되돌린다.
    // (씬이 닫히는 중에 실행되면 씬 전환이 겹칠 수 있어 실행하지 않는다)
    private void OnDisable()
    {
        if (pending == null) return;
        StopCoroutine(pending);
        pending = null;
        if (button != null) button.interactable = true;
    }
}
