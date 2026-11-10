using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 개발용: 팀원 작업(손님·키링 제작·판정 연결)이 붙기 전에 흐름·자본을 키보드로 시험한다.
/// 1 주문 수락 / 2 재료 사용 / 3 정답 서빙 / 4 오답 서빙 / 5 손님 처리 완료. 화면 왼쪽 위에 상태 표시.
/// 실제 손님·제작 시스템이 붙으면 이 컴포넌트를 끄거나 지운다.
/// </summary>
public class FlowDebugKeys : MonoBehaviour
{
    private GameFlowManager flowManager;
    private CapitalSystem capital;
    private CustomerData customer;
    private IngredientData chain, oRing, dRing, clover;
    private string lastLog = "";
    private GameResult? result;

    private void Awake()
    {
        flowManager = FindAnyObjectByType<GameFlowManager>();
        capital = FindAnyObjectByType<CapitalSystem>();

        chain = Ingredient("Chain", IngredientCategory.Chain);
        oRing = Ingredient("O-Ring", IngredientCategory.Clasp);
        dRing = Ingredient("D-Ring", IngredientCategory.Clasp);
        clover = Ingredient("Clover", IngredientCategory.Charm);

        var recipe = ScriptableObject.CreateInstance<RecipeData>();
        recipe.luckyNumber = 4;
        recipe.entries.Add(new RecipeData.Entry { ingredient = oRing, count = 1 });
        recipe.entries.Add(new RecipeData.Entry { ingredient = clover, count = 1 });

        customer = ScriptableObject.CreateInstance<CustomerData>();
        customer.displayName = "Debug Customer";
        customer.recipe = recipe;
    }

    private void OnEnable()
    {
        GameEvents.GameEnded += OnGameEnded;
        GameEvents.KeyringJudged += OnJudged;
    }

    private void OnDisable()
    {
        GameEvents.GameEnded -= OnGameEnded;
        GameEvents.KeyringJudged -= OnJudged;
    }
    private void OnGameEnded(GameResult r) => result = r;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 실제 손님·제작 시스템처럼 현재 단계에 맞는 신호만 보낸다.
        if (kb.digit1Key.wasPressedThisFrame)
        {
            if (!InPhase(GamePhase.Classroom, "Order")) return;
            GameEvents.RaiseOrderAccepted(customer); lastLog = "Order accepted (+$6 prepaid)";
        }
        if (kb.digit2Key.wasPressedThisFrame)
        {
            if (!InPhase(GamePhase.Crafting, "Ingredient")) return;
            GameEvents.RaiseIngredientUsed(clover); lastLog = "Ingredient used (-$1)";
        }
        if (kb.digit3Key.wasPressedThisFrame) Serve(chain, oRing, clover);
        if (kb.digit4Key.wasPressedThisFrame) Serve(chain, dRing, clover);
        if (kb.digit5Key.wasPressedThisFrame)
        {
            if (flowManager != null && flowManager.Phase == GamePhase.GameOver) { lastLog = "Finish ignored (game over)"; return; }
            GameEvents.RaiseCustomerFinished(customer); lastLog = "Customer finished";
        }
    }

    private bool InPhase(GamePhase phase, string action)
    {
        if (flowManager == null || flowManager.Phase == phase) return true;
        lastLog = $"{action} ignored (phase: {flowManager.Phase})";
        return false;
    }

    private void Serve(params IngredientData[] items)
    {
        if (!InPhase(GamePhase.Crafting, "Serve")) return;

        var keyring = new KeyringData();
        foreach (var i in items) keyring.Add(i);
        lastLog = "Served (no judgement - JudgementSystem missing?)";
        GameEvents.RaiseKeyringServed(customer, keyring); // 판정은 JudgementSystem이 하고 KeyringJudged로 알려 준다
    }

    private void OnJudged(CustomerData c, JudgementResult judged)
    {
        lastLog = $"Served → {judged.grade} ({judged.CapitalDelta:+0.00;-0.00;0})";
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 380, 150), "Flow Debug");
        GUI.Label(new Rect(20, 35, 360, 20), $"Phase: {(flowManager ? flowManager.Phase.ToString() : "-")}   Customers: {(flowManager ? flowManager.CustomersFinished : 0)}");
        GUI.Label(new Rect(20, 55, 360, 20), $"Capital: ${(capital ? capital.Current : 0f):0.00}");
        GUI.Label(new Rect(20, 75, 360, 20), $"Last: {lastLog}");
        GUI.Label(new Rect(20, 95, 360, 20), result.HasValue ? $"RESULT: {(result.Value.isWin ? "WIN" : "LOSE")} ${result.Value.capital:0.00}" : "");
        GUI.Label(new Rect(20, 125, 360, 30), "1 Order  2 Ingredient  3 Serve OK  4 Serve NG  5 Finish");
    }

    private static IngredientData Ingredient(string name, IngredientCategory category)
    {
        var i = ScriptableObject.CreateInstance<IngredientData>();
        i.name = name;
        i.displayName = name;
        i.category = category;
        return i;
    }
}
