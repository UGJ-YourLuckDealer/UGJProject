using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>행운의 숫자 1개에 대응하는 고정 레시피 (행운의 숫자와 1:1).</summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "YourLuckDealer/Recipe")]
public class RecipeData : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public IngredientData ingredient;
        [Min(1)] public int count;
    }

    [Range(1, 9)] public int luckyNumber = 1;

    [Tooltip("판정 대상 재료 목록. 체인은 모든 레시피에 기본으로 필요하므로 넣지 않는다")]
    public List<Entry> entries = new List<Entry>();

    /// <summary>판매가 = 레시피 재료 판매가 합 (선지급 금액).</summary>
    public float SalePrice
    {
        get
        {
            float sum = 0f;
            foreach (var e in entries)
                if (e.ingredient != null) sum += e.ingredient.price * e.count;
            return sum;
        }
    }

    /// <summary>판정 대상 재료 총 개수.</summary>
    public int TotalCount
    {
        get
        {
            int sum = 0;
            foreach (var e in entries)
                if (e.ingredient != null) sum += e.count;
            return sum;
        }
    }

    public int CountOf(IngredientData ingredient)
    {
        int sum = 0;
        foreach (var e in entries)
            if (e.ingredient == ingredient) sum += e.count;
        return sum;
    }
}
