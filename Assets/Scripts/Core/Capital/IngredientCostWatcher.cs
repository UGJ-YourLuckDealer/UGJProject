using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 재료비 실시간 차감 (hermit). 팀원 제작판(Check_Board)에 실제로 놓인 재료 수가 늘면 그만큼 자본에서 차감한다.
/// 팀원 스크립트(ObjectDragManager)를 수정하지 않고, 마우스를 놓은 뒤 판정이 끝난 시점의 개수를 비교한다.
/// </summary>
public class IngredientCostWatcher : MonoBehaviour
{
    [System.Serializable]
    public struct Kind
    {
        public string layer;
        public string tag;
    }

    [SerializeField] private float costPerIngredient = 1f; // 기획: 모든 재료 $1
    [SerializeField] private Kind[] kinds =
    {
        new Kind { layer = "Main_Kiring", tag = "Normal" },
        new Kind { layer = "Main_Kiring", tag = "Heart" },
        new Kind { layer = "Sub_Acrill", tag = "Clover" },
        new Kind { layer = "Sub_Acrill", tag = "Note" },
        new Kind { layer = "Sub_Acrill", tag = "Number" },
        new Kind { layer = "Sub_Acrill", tag = "Rainbow" },
    };

    private Check_Board board;
    private int settledCount;
    private Coroutine pending;

    private void Start()
    {
        board = FindAnyObjectByType<Check_Board>();
        settledCount = Count();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (board == null || mouse == null) return;
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (pending != null) StopCoroutine(pending);
            pending = StartCoroutine(SettleAndCharge());
        }
    }

    // 놓기 처리(파괴 포함)와 물리 트리거 갱신이 끝난 뒤의 개수로 비교한다
    private IEnumerator SettleAndCharge()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;

        int now = Count();
        int added = now - settledCount;
        settledCount = now;
        pending = null;
        if (added <= 0) yield break; // 빼기 기능이 생기면 여기서 처리 (지금은 차감만)

        var capital = FindAnyObjectByType<CapitalSystem>();
        if (capital == null) { Debug.LogWarning("[재료비 차감] 씬에 CapitalSystem이 없습니다."); yield break; }
        float before = capital.Current;
        capital.Add(-costPerIngredient * added);
        Debug.Log($"[재료비 차감] 재료 {added}개: {before:0.00} → {capital.Current:0.00}");
    }

    private int Count()
    {
        if (board == null) return 0;
        int sum = 0;
        foreach (var k in kinds)
        {
            int layer = LayerMask.NameToLayer(k.layer);
            if (layer >= 0) sum += board.GetCount(layer, k.tag);
        }
        return sum;
    }
}
