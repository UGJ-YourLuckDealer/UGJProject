using UnityEngine;

/// <summary>키링 재료 카테고리 (행운의 키링 제작 시스템 기획 2026-11-09 기준).</summary>
public enum IngredientCategory
{
    Chain,  // 체인: 키링당 1개, 현재 1종 고정
    Clasp,  // 잠금장치: 키링당 1개 (D고리, O고리)
    Charm,  // 부적: 키링당 1개 이상 (클로버, 무지개)
}

/// <summary>키링 재료 1종의 데이터. 재료비는 사용할 때마다 차감, 판매가는 레시피 판매가 계산에 쓰인다.</summary>
[CreateAssetMenu(fileName = "NewIngredient", menuName = "YourLuckDealer/Ingredient")]
public class IngredientData : ScriptableObject
{
    public string displayName;
    public IngredientCategory category;
    public Sprite icon;

    [Tooltip("재료비 ($). 키링 제작 중 사용할 때마다 자본에서 차감")]
    public float cost = 1f;

    [Tooltip("판매가 ($). 레시피 판매가 = 레시피 재료 판매가의 합")]
    public float price = 3f;

    /// <summary>카테고리별 키링당 최대 개수. 0 이하는 제한 없음.</summary>
    public static int MaxPerKeyring(IngredientCategory category)
    {
        switch (category)
        {
            case IngredientCategory.Chain: return 1;
            case IngredientCategory.Clasp: return 1;
            default: return 0;
        }
    }
}
