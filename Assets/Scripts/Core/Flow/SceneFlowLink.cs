using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬과 SceneFlow를 잇는 hermit 연결 컴포넌트. 팀원 스크립트·오브젝트는 수정하지 않는다.
/// - Counter: 시작 손님 번호를 DialogueManager에 넣고, 씬을 떠날 때(팀원 코드가 직접 Making을 불러도) 손님 번호를 기억한다.
///   오른쪽 위에 돈 표시 UI(Resources/CounterMoneyBar)를 띄우고, 하루 마지막 손님이면 팁 대사를 클릭한 뒤 하루 정산 팝업을 연다.
/// - Making: 팀원 Submit 버튼(Result.Result_Kiring)에 실행 중에만 ReturnFromMaking을 붙인다.
///   지금 손님 번호에 맞는 레시피(customerReceipts)를 팀원 판정(Result.receipt_data)에 넣는다.
/// - 버튼 OnClick에서 SceneFlow 함수를 부르는 용도로도 쓴다.
/// </summary>
public class SceneFlowLink : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogue;

    [Tooltip("Making: 팀원 Submit 버튼(Result_Kiring을 부르는 버튼)을 찾아 실행 중에 ReturnFromMaking을 연결")]
    [SerializeField] private bool hookResultSubmit;

    [Tooltip("Making: 손님 번호(DialogueManager.customer) 순서대로 판정할 레시피. 비어 있으면 Result에 넣어 둔 기본 레시피를 쓴다")]
    [SerializeField] private Receipt_Data[] customerReceipts;

    [Tooltip("Counter: 하루 마지막 팁 대사를 클릭해 대사·초상화가 사라진 뒤 정산 팝업을 열기까지 기다리는 시간(초)")]
    [SerializeField, Min(0f)] private float settlementDelay = 0.8f;

    [Tooltip("Making: 보드에 아무것도 없이 Submit을 눌렀을 때 보여 줄 문구")]
    [SerializeField] private string emptyBoardMessage = "Make a keyring first!";
    [SerializeField, Min(0.1f)] private float emptyBoardHintSeconds = 1.5f;
    [Tooltip("Submit 버튼 위쪽 끝에서 안내 말풍선 중심까지의 거리 (말풍선 높이 110의 절반 + 여백). 버튼이 어디로 옮겨져도 버튼 바로 위 가운데에 뜬다")]
    [SerializeField] private float emptyBoardHintGap = 70f;

    private const string CounterMoneyBarPath = "CounterMoneyBar"; // Resources/CounterMoneyBar.prefab

    private int cachedCustomer = -1;
    private int cachedCustomerCount;
    private bool isCounter;
    private bool settlementOpened;
    private const string RestartDialogueMethod = "ReStartDialogue"; // DialogueManager가 대사를 닫은 뒤 Invoke로 예약하는 메서드

    private void Awake()
    {
        if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        // DialogueManager.Start보다 먼저 실행되어 다음 손님부터 대화를 시작하게 한다.
        if (dialogue != null && SceneFlow.NextCustomerIndex > 0 && dialogue.dialogueData != null
            && SceneFlow.NextCustomerIndex < dialogue.dialogueData.Length)
            dialogue.customer = SceneFlow.NextCustomerIndex;

        if (hookResultSubmit) HookSubmitButtons();
        ApplyCustomerReceipt();

        isCounter = gameObject.scene.name == SceneFlow.CounterScene;
        if (isCounter)
        {
            SpawnCounterMoneyBar();
            FillReactionSpeakerName();
            KeepCharacterAspect();
            CreateCharacterWhiteFade();
        }
    }

    /// <summary>
    /// 캐릭터 그림은 손님마다 가로세로 비율이 다른데(Candy 0.89, Biskuit 0.71, Licorice 0.68),
    /// Counter의 Character_Image 칸(700x725)은 Preserve Aspect가 꺼져 있어 2·3번 캐릭터가 가로로 늘어나 보였다.
    /// 씬 파일은 그대로 두고 실행 중에 비율 유지를 켠다 (칸 안에서 비율을 지키며 가운데 정렬).
    /// </summary>
    private void KeepCharacterAspect()
    {
        var image = dialogue != null ? dialogue.characterImage : null;
        if (image != null) image.preserveAspect = true;
    }

    // ---- 캐릭터 등장 연출: 하얀 실루엣 → 원래 색 ----
    // 팀원 DialogueManager.CharacterImageColor는 3초 동안 캐릭터 이미지 색 RGB를 0→1로 올려 '검은 실루엣 → 원래 색'으로 보여 준다.
    // 팀원 코드는 그대로 두고, 매 프레임 그 진행도(RGB 값)를 읽어 이미지 색은 원래대로 되돌리고 대신 하얗게 덮었다가 걷어 낸다.
    // 1) 캐릭터 머티리얼이 AllIn1 Sprite Shader(Hand Drawing Portrait: 손그림 흔들림·외곽선·그림자)면
    //    같은 설정에 Hit Effect(흰색 덮기)만 켠 머티리얼로 잠시 바꿔 그림 자체를 하얗게 → 흔들림·외곽선·그림자까지 정확히 겹친다.
    //    빌드에 해당 셰이더 조합이 포함되도록 Resources/HandDrawingPortrait_White.mat을 원본으로 쓴다.
    // 2) 그 외 머티리얼이면 위에 같은 스프라이트의 하얀 실루엣(UIWhiteSilhouette 셰이더)을 겹쳐 서서히 투명하게 한다.
    private const string WhiteSilhouetteShader = "UIWhiteSilhouette";           // Resources/UIWhiteSilhouette.shader
    private const string WhiteHitMaterialPath = "HandDrawingPortrait_White";    // Resources/HandDrawingPortrait_White.mat
    private static readonly int HitEffectBlendId = Shader.PropertyToID("_HitEffectBlend");
    private const string HitEffectKeyword = "HITEFFECT_ON";

    private Image characterWhiteFade;       // 방법 2: 겹치는 하얀 실루엣
    private Material whiteSilhouetteMaterial;
    private Material characterOriginalMaterial; // 방법 1: 원래 캐릭터 머티리얼
    private Material characterWhiteMaterial;    // 방법 1: Hit Effect를 켠 실행 중 복사본
    private bool characterWhiteApplied;

    private void CreateCharacterWhiteFade()
    {
        var target = dialogue != null ? dialogue.characterImage : null;
        if (target == null) return;

        // 방법 1: 캐릭터 머티리얼에 Hit Effect가 있으면 그림 자체를 하얗게
        var original = target.material;
        var whiteSource = Resources.Load<Material>(WhiteHitMaterialPath);
        if (original != null && original != target.defaultMaterial && original.HasProperty(HitEffectBlendId)
            && whiteSource != null && whiteSource.shader == original.shader)
        {
            characterOriginalMaterial = original;
            characterWhiteMaterial = new Material(whiteSource) { name = original.name + " (white fade, runtime)" };
            characterWhiteMaterial.CopyPropertiesFromMaterial(original); // 팀원이 원본 값을 바꿔도 따라감
            characterWhiteMaterial.EnableKeyword(HitEffectKeyword);
            characterWhiteMaterial.SetColor("_HitEffectColor", Color.white);
            characterWhiteMaterial.SetFloat("_HitEffectGlow", 1f);
            return;
        }

        // 방법 2: 겹치는 하얀 실루엣
        var shader = Resources.Load<Shader>(WhiteSilhouetteShader);
        if (shader == null || !shader.isSupported) return; // 셰이더가 없으면 팀원 연출(검은 실루엣) 그대로

        whiteSilhouetteMaterial = new Material(shader) { name = "UIWhiteSilhouette (runtime)" };
        var go = new GameObject("CharacterWhiteFade (runtime)", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(target.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        characterWhiteFade = go.GetComponent<Image>();
        characterWhiteFade.material = whiteSilhouetteMaterial;
        characterWhiteFade.raycastTarget = false;
        characterWhiteFade.enabled = false;
    }

    // 팀원 코루틴(Update 뒤 실행)이 색을 바꾼 다음에 덮어쓴다
    private void UpdateCharacterWhiteFade()
    {
        if (characterWhiteMaterial == null && characterWhiteFade == null) return;
        var target = dialogue != null ? dialogue.characterImage : null;
        if (target == null) { SetCharacterWhite(null, false, 0f); return; }

        var c = target.color;
        float alpha = Mathf.Clamp01(c.a);
        bool fading = target.sprite != null && alpha > 0f && c.r < 0.999f; // RGB 0→1 진행 중 = 등장 연출 중
        if (!fading) { SetCharacterWhite(target, false, 0f); return; }

        float progress = Mathf.Clamp01(c.r);
        target.color = new Color(1f, 1f, 1f, c.a); // 검게 칠하지 않고 원래 색 그대로
        SetCharacterWhite(target, true, 1f - progress); // 흰색 정도: 1 → 0
    }

    private void SetCharacterWhite(Image target, bool on, float whiteness)
    {
        if (characterWhiteMaterial != null)
        {
            if (target == null) return;
            if (on)
            {
                if (!characterWhiteApplied) { target.material = characterWhiteMaterial; characterWhiteApplied = true; }
                characterWhiteMaterial.SetFloat(HitEffectBlendId, whiteness);
                target.SetMaterialDirty();
            }
            else if (characterWhiteApplied)
            {
                target.material = characterOriginalMaterial;
                characterWhiteApplied = false;
            }
            return;
        }

        if (characterWhiteFade == null) return;
        if (!on || target == null) { characterWhiteFade.enabled = false; return; }
        characterWhiteFade.sprite = target.sprite;
        characterWhiteFade.type = target.type;
        characterWhiteFade.preserveAspect = target.preserveAspect;
        characterWhiteFade.color = new Color(1f, 1f, 1f, Mathf.Clamp01(target.color.a) * whiteness);
        characterWhiteFade.enabled = true;
    }

    // 손님 순서별 이름 (대화 데이터에 이름이 비어 있을 때만 사용)
    private static readonly string[] customerNames = { "Candy", "Biskuit", "Licorice" };

    /// <summary>
    /// 키링 제출 뒤 Counter의 반응(팁) 대사에 손님 이름이 나오게 한다.
    /// 팀원 DialogueManager.LastDialogue는 이름을 line.character_Name에서 읽는데, 씬이 새로 열리면 line이 비어 있어 이름이 빈칸이 된다.
    /// DialogueManager.Start(→ LastDialogue)보다 먼저 지금 손님의 이름을 line에 넣어 둔다 (팀원 코드는 수정하지 않음).
    /// </summary>
    private void FillReactionSpeakerName()
    {
        if (dialogue == null || Customer_Data.isfinish) return; // isfinish == false일 때만 반응 대사
        if (dialogue.line == null) dialogue.line = new DialogueLine();
        if (!string.IsNullOrEmpty(dialogue.line.character_Name)) return;

        int customer = Customer_Data.customer; // DialogueManager.Start가 같은 값을 쓴다
        string name = null;
        var data = dialogue.dialogueData;
        if (data != null && customer >= 0 && customer < data.Length && data[customer] != null && data[customer].dialogueLines != null)
        {
            foreach (var l in data[customer].dialogueLines)
                if (l != null && !string.IsNullOrEmpty(l.character_Name)) { name = l.character_Name; break; }
        }
        if (string.IsNullOrEmpty(name) && customer >= 0 && customer < customerNames.Length) name = customerNames[customer];
        if (!string.IsNullOrEmpty(name)) dialogue.line.character_Name = name;
    }

    // Counter 씬 파일은 건드리지 않고 실행 중에 돈 표시 UI를 붙인다 (씬 오브젝트라 씬을 떠나면 함께 사라짐)
    private void SpawnCounterMoneyBar()
    {
        var prefab = Resources.Load<GameObject>(CounterMoneyBarPath);
        if (prefab == null) return;
        var bar = Instantiate(prefab);
        bar.name = prefab.name;
    }

    private void Update()
    {
        // 하루 마지막 손님: Counter로 돌아와 나온 팁(반응) 대사를 클릭하면 하루 정산 팝업.
        // 팁 대사를 클릭하면 팀원 DialogueManager.EndDialogue가 대사를 닫고 2초 뒤 다음 대화(ReStartDialogue)를 예약한다.
        // 그 예약이 생긴 순간을 '팁 대사 클릭'으로 보고 예약은 바로 취소한 뒤,
        // 대사창·초상화가 사라진 빈 화면을 잠깐(settlementDelay) 보여 주고 팝업을 연다 (팀원 코드는 수정하지 않음).
        if (!isCounter || settlementOpened || !GameSession.SettlementPending) return;
        if (dialogue == null || dialogue.IsInvoking(RestartDialogueMethod)) OpenSettlement();
    }

    private void OpenSettlement()
    {
        settlementOpened = true;
        if (dialogue != null) dialogue.CancelInvoke(); // 대사가 끝난 뒤 예약된 다음 대화 시작(ReStartDialogue)이 팝업 뒤에서 돌지 않게
        StartCoroutine(OpenSettlementAfterDelay());
    }

    private System.Collections.IEnumerator OpenSettlementAfterDelay()
    {
        // 기다리는 동안 책상 클릭으로 Making에 들어가지 않게 잠시 끈다 (팝업이 열리면 팝업이 다시 막는다)
        var desks = FindObjectsByType<DeskInteraction>(FindObjectsSortMode.None);
        var disabled = new System.Collections.Generic.List<DeskInteraction>();
        foreach (var d in desks)
            if (d.enabled) { d.enabled = false; disabled.Add(d); }

        if (settlementDelay > 0f) yield return new WaitForSecondsRealtime(settlementDelay);

        foreach (var d in disabled)
            if (d != null) d.enabled = true;
        SettlementPopup.OpenDaySettlement();
    }

    private void ApplyCustomerReceipt()
    {
        // Counter에서 GoToMaking/NoteCounterLeft가 지금 손님 번호를 기록한다
        int current = SceneFlow.NextCustomerIndex;
        if (customerReceipts == null || current < 0 || current >= customerReceipts.Length) return;
        if (customerReceipts[current] == null) return;

        Customer_Data.receipt = customerReceipts[current];
    }

    private void LateUpdate()
    {
        // 씬을 떠날 때는 DialogueManager가 이미 파괴됐을 수 있어 값을 미리 기억해 둔다
        if (dialogue == null) return;
        UpdateCharacterWhiteFade();
        cachedCustomer = dialogue.customer;
        cachedCustomerCount = dialogue.dialogueData != null ? dialogue.dialogueData.Length : 0;
    }

    private void OnDestroy()
    {
        if (whiteSilhouetteMaterial != null) Destroy(whiteSilhouetteMaterial);
        if (characterWhiteMaterial != null) Destroy(characterWhiteMaterial);
        // 팀원 코드가 SceneManager로 직접 씬을 바꿔도 손님 진행이 이어지도록 기록
        if (cachedCustomer >= 0) SceneFlow.NoteCounterLeft(cachedCustomer, cachedCustomerCount);
    }

    private void HookSubmitButtons()
    {
        // 결산 팝업이 있으면 Submit → 결산 → (팝업 버튼) → ReturnFromMaking, 없으면 바로 ReturnFromMaking.
        // 보드에 아무것도 없으면 제출하지 않도록, 팀원 판정(Result_Kiring) 연결은 이번 실행에서만 끄고
        // 확인 후 직접 부른다 (프리팹·씬 파일은 그대로).
        var settlement = FindAnyObjectByType<SettlementPopup>();
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentMethodName(i) == "Result_Kiring")
                {
                    var result = b.onClick.GetPersistentTarget(i) as Result;
                    if (result == null) break; // 알 수 없는 대상이면 원래 동작 그대로
                    b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    var button = b;
                    b.onClick.AddListener(() => Submit(button, result, settlement));
                    break;
                }
    }

    private void Submit(Button button, Result result, SettlementPopup settlement)
    {
        if (IsBoardEmpty())
        {
            ShowEmptyBoardHint(button.transform);
            return;
        }
        result.Result_Kiring(); // 팀원 판정
        if (settlement != null) settlement.Show();
        else ReturnFromMaking();
    }

    // 보드에 키링 본체(체인)가 없으면 아무것도 만들지 않은 것으로 본다
    private static bool IsBoardEmpty()
    {
        var manager = FindAnyObjectByType<ObjectDragManager>();
        return manager == null || manager.CurrentChain == null;
    }

    private void ShowEmptyBoardHint(Transform target)
    {
        var tooltip = GuideTooltip.Instance;
        if (tooltip == null) return;
        tooltip.Show(target, emptyBoardMessage, HintOffsetAbove(target));
        StopCoroutine(nameof(HideHintLater));
        StartCoroutine(nameof(HideHintLater));
    }

    // 말풍선은 대상의 가운데를 기준으로 offset만큼 떨어져 뜬다 (GuideTooltip, Canvas 1920x1080·Match 0.5).
    // 실행 중 버튼의 실제 화면 높이를 재서 '버튼 위쪽 끝 + gap' 위치가 되도록 offset을 만든다.
    private Vector2 HintOffsetAbove(Transform target)
    {
        float halfHeight = 0f;
        if (target is RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners); // 0 왼쪽 아래, 1 왼쪽 위
            float screenHeight = RectTransformUtility.WorldToScreenPoint(cam, corners[1]).y - RectTransformUtility.WorldToScreenPoint(cam, corners[0]).y;
            float tooltipScale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(Screen.width / 1920f, 2f), Mathf.Log(Screen.height / 1080f, 2f), 0.5f));
            if (tooltipScale > 0f) halfHeight = Mathf.Abs(screenHeight) / tooltipScale * 0.5f;
        }
        return new Vector2(0f, halfHeight + emptyBoardHintGap);
    }

    private System.Collections.IEnumerator HideHintLater()
    {
        yield return new WaitForSecondsRealtime(emptyBoardHintSeconds);
        if (GuideTooltip.Instance != null) GuideTooltip.Instance.Hide();
    }

    public void StartNewGame() => SceneFlow.StartNewGame();
    public void GoToMaking() => SceneFlow.GoToMaking(dialogue);
    public void ReturnFromMaking() => SceneFlow.ReturnFromMaking();
    public void GoToTitle() => SceneFlow.GoToTitle();
}
