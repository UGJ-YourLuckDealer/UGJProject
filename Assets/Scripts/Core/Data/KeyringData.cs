using System.Collections.Generic;

/// <summary>제작 중/서빙된 키링 정보: 어떤 재료가 몇 개 달렸는지.</summary>
public class KeyringData
{
    private readonly Dictionary<IngredientData, int> counts = new Dictionary<IngredientData, int>();

    public IReadOnlyDictionary<IngredientData, int> Counts => counts;

    public int CountOf(IngredientData ingredient)
    {
        return ingredient != null && counts.TryGetValue(ingredient, out int c) ? c : 0;
    }

    public int CountOf(IngredientCategory category)
    {
        int sum = 0;
        foreach (var pair in counts)
            if (pair.Key.category == category) sum += pair.Value;
        return sum;
    }

    /// <summary>카테고리별 최대 개수(IngredientData.MaxPerKeyring) 안에서 더 달 수 있는지.</summary>
    public bool CanAdd(IngredientData ingredient)
    {
        if (ingredient == null) return false;
        int max = IngredientData.MaxPerKeyring(ingredient.category);
        return max <= 0 || CountOf(ingredient.category) < max;
    }

    /// <summary>재료를 단다. 성공하면 true. 재료비 차감은 호출한 쪽이 GameEvents.RaiseIngredientUsed로 요청한다.</summary>
    public bool Add(IngredientData ingredient)
    {
        if (!CanAdd(ingredient)) return false;
        counts[ingredient] = CountOf(ingredient) + 1;
        return true;
    }

    public bool Remove(IngredientData ingredient)
    {
        int c = CountOf(ingredient);
        if (c == 0) return false;
        if (c == 1) counts.Remove(ingredient);
        else counts[ingredient] = c - 1;
        return true;
    }

    public void Clear() => counts.Clear();
}
