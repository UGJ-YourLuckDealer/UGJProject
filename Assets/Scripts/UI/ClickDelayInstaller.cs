using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬의 모든 버튼에 실행 중에만 ButtonClickDelay를 붙인다 (팀원 버튼 오브젝트는 씬 파일에서 수정하지 않음).
/// 다른 스크립트가 Awake에서 연결한 동작까지 함께 지연되도록 Start에서 붙인다.
/// </summary>
public class ClickDelayInstaller : MonoBehaviour
{
    private void Start()
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.GetComponent<ButtonClickDelay>() == null)
                b.gameObject.AddComponent<ButtonClickDelay>();
    }
}
