using UnityEngine;

/// <summary>손님 1명의 데이터. 이름·대사는 Hook이 Unity에서 작성한다.</summary>
[CreateAssetMenu(fileName = "NewCustomer", menuName = "YourLuckDealer/Customer")]
public class CustomerData : ScriptableObject
{
    public string displayName;

    [Tooltip("이 손님이 원하는 행운의 숫자의 레시피 (행운의 숫자와 1:1)")]
    public RecipeData recipe;

    [Tooltip("손님 대사 (팀원 대화 시스템의 DialogueData)")]
    public DialogueData dialogue;

    public int LuckyNumber => recipe != null ? recipe.luckyNumber : 0;
}
