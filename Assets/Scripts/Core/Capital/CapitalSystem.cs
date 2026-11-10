using UnityEngine;

/// <summary>
/// 자본 (v0.0.1). 주문 수락 시 레시피 판매가 선지급, 재료 사용 시 재료비 차감,
/// 판정 결과의 CapitalDelta(팁 +, 환불 -)를 반영한다. 바뀔 때마다 GameEvents.CapitalChanged를 보낸다.
/// </summary>
public class CapitalSystem : MonoBehaviour
{
    [SerializeField] private float startingCapital = 10f;

    public float Current { get; private set; }
    public float StartingCapital => startingCapital;

    private void Awake()
    {
        Current = startingCapital;
    }

    private void OnEnable()
    {
        GameEvents.OrderAccepted += OnOrderAccepted;
        GameEvents.IngredientUsed += OnIngredientUsed;
        GameEvents.KeyringJudged += OnKeyringJudged;
    }

    private void OnDisable()
    {
        GameEvents.OrderAccepted -= OnOrderAccepted;
        GameEvents.IngredientUsed -= OnIngredientUsed;
        GameEvents.KeyringJudged -= OnKeyringJudged;
    }

    private void Start()
    {
        GameEvents.RaiseCapitalChanged(Current);
    }

    public void ResetCapital()
    {
        Current = startingCapital;
        GameEvents.RaiseCapitalChanged(Current);
    }

    public void Add(float amount)
    {
        if (Mathf.Approximately(amount, 0f)) return;
        Current += amount;
        GameEvents.RaiseCapitalChanged(Current);
    }

    public void OnOrderAccepted(CustomerData customer)
    {
        if (customer != null && customer.recipe != null) Add(customer.recipe.SalePrice);
    }

    public void OnIngredientUsed(IngredientData ingredient)
    {
        if (ingredient != null) Add(-ingredient.cost);
    }

    public void OnKeyringJudged(CustomerData customer, JudgementResult result)
    {
        Add(result.CapitalDelta);
    }
}
