using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 하루 시계 (임시 UI, 프로토타입 v0.0.3 "시계 처리"). 휴식 타이머가 없으므로 손님 처리 비율로 진행한다.
/// 하루 손님 N명 중 처리한 수 / N 만큼 시계 안쪽(Timer_Inside)이 시계 방향으로 채워진다 (예: 3명 → 1명마다 1/3).
/// GameSystems 프리팹(하루 동안 유지)에 붙고, UI는 실행 중에 만든다. Counter·Making에서만 보인다.
/// </summary>
public class DayClock : MonoBehaviour
{
    [Header("Motion")]
    [Tooltip("진행이 바뀔 때 채워지는 속도 (초당 비율)")]
    [SerializeField, Min(0.1f)] private float fillSpeed = 0.6f;
    [SerializeField] Image _fillImage;

    /// <summary>오늘 진행 비율 0~1 (처리한 손님 / 하루 손님 수).</summary>
    public float Progress
    {
        get
        {
            var flow = GetFlowManager();
            return flow != null && flow.CustomersPerGame > 0
                ? Mathf.Clamp01((float)flow.CustomersFinished / flow.CustomersPerGame)
                : 0f;
        }
    }

    bool _isCounterScene;

    void Start()
    {
        _isCounterScene = SceneManager.GetActiveScene().name == SceneFlow.CounterScene;
    }

    GameFlowManager GetFlowManager()
    {
        return FindAnyObjectByType<GameFlowManager>();
    }

    private void Update()
    {
        if (!_isCounterScene)
        {
            return;
        }

        if (_fillImage == null)
        {
            return;
        }

        _fillImage.fillAmount = Mathf.Clamp01(1f - Progress);
    }
}
