using System.Collections.Generic;

public enum JudgementGrade
{
    Perfect, // 100%: 판매가 + 팁
    Pass,    // 80% 이상: 판매가만
    Refund,  // 80% 미만 또는 0으로 나누기: 환불
}

/// <summary>판정 결과. 판매가는 선지급되므로 CapitalDelta만 자본에 반영하면 된다.</summary>
public struct JudgementResult
{
    public JudgementGrade grade;
    public int matched;      // 레시피와 일치하는 재료 개수
    public int mismatched;   // 레시피와 일치하지 않는 재료 개수
    public int total;        // 레시피 재료 총 개수
    public float ratio;      // 구현비율 (0~1, 음수 가능)
    public float salePrice;  // 레시피 판매가 (선지급 금액)
    public float tip;        // Perfect일 때 판매가 * TipRate

    public bool HasTip => grade == JudgementGrade.Perfect;

    /// <summary>판정 후 자본 변화: Perfect = +팁, Pass = 0, Refund = -판매가(환불).</summary>
    public float CapitalDelta
    {
        get
        {
            switch (grade)
            {
                case JudgementGrade.Perfect: return tip;
                case JudgementGrade.Refund: return -salePrice;
                default: return 0f;
            }
        }
    }
}

/// <summary>
/// 구현비율 = (레시피와 일치하는 요소 개수 - 일치하지 않는 요소 개수) / 레시피 요소 총 개수
/// (행운의 키링 제작 시스템 기획 2026-11-09). 체인은 판정에서 제외한다.
/// </summary>
public static class KeyringJudge
{
    public const float TipRate = 0.1f;
    public const float PassRatio = 0.8f;

    public static JudgementResult Judge(RecipeData recipe, KeyringData keyring)
    {
        var r = new JudgementResult { grade = JudgementGrade.Refund };
        if (recipe == null || keyring == null) return r;

        r.salePrice = recipe.SalePrice;
        r.total = recipe.TotalCount;

        var seen = new HashSet<IngredientData>();
        foreach (var pair in keyring.Counts)
        {
            if (pair.Key.category == IngredientCategory.Chain) continue;
            seen.Add(pair.Key);
            int want = recipe.CountOf(pair.Key);
            r.matched += System.Math.Min(pair.Value, want);
            r.mismatched += System.Math.Max(0, pair.Value - want);
        }

        if (r.total <= 0) return r; // 0으로 나누기 → 실패(환불)

        r.ratio = (float)(r.matched - r.mismatched) / r.total;
        if (r.matched == r.total && r.mismatched == 0)
        {
            r.grade = JudgementGrade.Perfect;
            r.tip = r.salePrice * TipRate;
        }
        else if (r.ratio >= PassRatio - 0.0001f) // float 오차 보정
        {
            r.grade = JudgementGrade.Pass;
        }
        return r;
    }
}
