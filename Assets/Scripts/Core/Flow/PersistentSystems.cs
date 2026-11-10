using UnityEngine;

/// <summary>
/// GameSystems(자본·흐름·판정)를 씬 전환 후에도 하나만 유지한다.
/// 각 씬에 단독 테스트용 GameSystems를 둬도 되며, 이미 유지 중인 것이 있으면 새로 불러온 쪽을 즉시 제거한다.
/// 같은 오브젝트의 다른 컴포넌트보다 먼저 실행되도록 실행 순서를 가장 앞으로 둔다.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class PersistentSystems : MonoBehaviour
{
    public static PersistentSystems Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject); // 중복: 다른 컴포넌트가 Awake/OnEnable 되기 전에 제거
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>새 게임을 위해 유지 중인 시스템을 제거한다 (자본·흐름 초기화).</summary>
    public static void ResetSession()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => Instance = null;
}
