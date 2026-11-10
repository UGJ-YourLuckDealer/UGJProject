using UnityEngine;

/// <summary>
/// 판정 연결 (v0.0.1). 주문 수락(OrderAccepted)된 손님의 서빙(KeyringServed)을 KeyringJudge로 판정하고
/// GameEvents.KeyringJudged로 결과를 보낸다. 주문 없는 서빙·같은 주문의 중복 서빙은 무시한다.
/// </summary>
public class JudgementSystem : MonoBehaviour
{
    private CustomerData pendingOrder;

    public bool HasPendingOrder => pendingOrder != null;

    private void OnEnable()
    {
        GameEvents.OrderAccepted += OnOrderAccepted;
        GameEvents.KeyringServed += OnKeyringServed;
        GameEvents.CustomerFinished += OnCustomerFinished;
    }

    private void OnDisable()
    {
        GameEvents.OrderAccepted -= OnOrderAccepted;
        GameEvents.KeyringServed -= OnKeyringServed;
        GameEvents.CustomerFinished -= OnCustomerFinished;
    }

    public void OnOrderAccepted(CustomerData customer)
    {
        if (customer == null || customer.recipe == null)
        {
            Debug.LogWarning("[Judgement] 레시피가 없는 손님의 주문입니다. 판정할 수 없습니다.");
            pendingOrder = null;
            return;
        }
        pendingOrder = customer;
    }

    public void OnKeyringServed(CustomerData customer, KeyringData keyring)
    {
        if (pendingOrder == null || customer != pendingOrder)
        {
            Debug.LogWarning("[Judgement] 주문이 없는 서빙이라 판정하지 않습니다.");
            return;
        }

        var result = KeyringJudge.Judge(customer.recipe, keyring);
        pendingOrder = null;
        Debug.Log($"[Judgement] {customer.displayName}: {result.grade} (ratio {result.ratio:0.00}, matched {result.matched}, mismatched {result.mismatched}, total {result.total})");
        GameEvents.RaiseKeyringJudged(customer, result);
    }

    public void OnCustomerFinished(CustomerData customer)
    {
        // 주문 후 서빙 없이 끝난 경우(거절 등) 대기 주문을 비운다.
        if (pendingOrder == customer) pendingOrder = null;
    }
}
