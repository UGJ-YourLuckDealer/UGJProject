# 공통 데이터 형식·연결 규칙 (v0.0.1)

> hermit ↔ macro 작업을 따로 만들고 나중에 붙이기 위한 약속입니다.
> 코드 위치: `Assets/Scripts/Core/` · 기준 기획: 행운의 키링 제작 시스템 (2026-11-09)

## 1. 데이터 (ScriptableObject)

Project 창 우클릭 → **Create → YourLuckDealer** 에서 만듭니다.

| 타입 | 필드 | 설명 |
|---|---|---|
| `IngredientData` | `displayName`, `category`, `icon`, `cost`($1), `price`($3) | 재료 1종. `category` = `Chain` / `Clasp` / `Charm` |
| `RecipeData` | `luckyNumber`(1~9), `entries`(재료 + 개수) | 행운의 숫자와 1:1. **체인은 넣지 않음** (모든 레시피에 기본으로 필요) |
| `CustomerData` | `displayName`, `recipe`, `dialogue`(`DialogueData`) | 손님 1명. 행운의 숫자 = `recipe.luckyNumber` |
| `KeyringData` (일반 클래스) | `Add` / `Remove` / `CountOf` / `CanAdd` | 제작 중인 키링. 카테고리 개수 제한을 지킴 |

카테고리별 키링당 최대 개수 (`IngredientData.MaxPerKeyring`): 체인 1, 잠금장치 1, 부적 제한 없음.

## 2. 연결 신호 (`GameEvents`)

보내는 쪽은 `GameEvents.Raise...()`를 호출하고, 받는 쪽은 이벤트를 구독합니다. 씬 참조가 필요 없습니다.

| 신호 | 보내는 쪽 → 받는 쪽 | 언제 |
|---|---|---|
| `RaiseOrderAccepted(customer)` | 손님(macro) → 자본(hermit) | 손님 주문을 받았을 때. 판매가 선지급 |
| `RaiseIngredientUsed(ingredient)` | 키링 제작(macro) → 자본(hermit) | 재료를 키링에 달 때마다. **재료비 차감 요청** |
| `RaiseKeyringServed(customer, keyring)` | 키링 제작(macro) → 판정(hermit) | 서빙 버튼. **키링 정보 전달** |
| `RaiseKeyringJudged(customer, result)` | 판정(hermit) → 손님·UI(macro) | 판정 직후. 손님 반응·팁 표시용 |
| `RaiseCustomerFinished(customer)` | 손님(macro) → 게임 흐름(hermit) | 반응 대사까지 끝났을 때. **손님 처리 완료** |

순서: `OrderAccepted` → `IngredientUsed` (여러 번) → `KeyringServed` → `KeyringJudged` → `CustomerFinished`

```csharp
// 받는 쪽 예시: 구독한 MonoBehaviour는 반드시 해제
void OnEnable()  { GameEvents.KeyringJudged += OnJudged; }
void OnDisable() { GameEvents.KeyringJudged -= OnJudged; }
void OnJudged(CustomerData customer, JudgementResult result) { /* result.HasTip */ }

// 보내는 쪽 예시
if (keyring.Add(ingredient)) GameEvents.RaiseIngredientUsed(ingredient);
GameEvents.RaiseKeyringServed(currentCustomer, keyring);
```

> 이 프로젝트는 Enter Play Mode에서 Domain Reload가 꺼져 있어, `GameEvents`는 Play 시작마다 구독을 자동으로 비웁니다.

### 게임 흐름·자본 → 화면/UI (화면 전환 담당 팀원용)

| 신호 | 내용 |
|---|---|
| `PhaseChanged(GamePhase)` | **화면 전환 요청.** `Classroom` = 교실 View, `Crafting` = 키링 작업 View, `GameOver` = 결과 화면. 게임 시작 시 `Classroom`이 한 번 옵니다 |
| `CapitalChanged(float)` | 현재 자본 (재화 텍스트용) |
| `GameEnded(GameResult)` | 손님 3명 처리 후 종료. `isWin`, `capital`, `customersFinished` |

흐름: `Classroom` →(OrderAccepted)→ `Crafting` →(KeyringServed)→ `Classroom` →(CustomerFinished)→ 다음 손님 … 3명째 `CustomerFinished` → `GameOver` + `GameEnded`
주문을 거절해 제작 중에 `CustomerFinished`가 와도 `Classroom`으로 돌아갑니다.

### 키링 제출 후 손님 반응 (hermit → macro, v0.0.3)

키링을 제출하면 Making에서 판정하고, 손님 반응 대화는 다음 Counter에서 나옵니다. 씬이 바뀌어도 읽을 수 있게 마지막 판정을 `GameSession`에 남깁니다.

| 값 | 내용 |
|---|---|
| `GameSession.ReactionIndex` | **반응 대사 번호.** `DialogueData.dialogueBifurcations` 순서(Best, Good, Bad)와 같음. Perfect(팁) = 0, Pass = 1, Refund = 2, 판정 전 = -1 |
| `GameSession.LastJudgement` | 판정 결과 전체 (`grade`, `HasTip`, `ratio` 등). 판정 전이면 null |
| `GameSession.LastJudgedCustomer` | 판정한 손님 번호 (`DialogueManager.customer`) |
| `GameSession.LastTipAmount` | 받은 팁 금액 (없으면 0) |

```csharp
int index = GameSession.ReactionIndex;            // 0 Best, 1 Good, 2 Bad
if (index >= 0) bifurcation = dialogueData[customer].dialogueBifurcations[index];
```

자본 반영(판매가·팁·환불)과 하루 집계(수입·팁·환불·지출)는 판정 직후 자동으로 처리됩니다 (`Result.cs` → `SettlementRecorder`).

### Counter 손님 방문 시퀀스 (hermit → macro, v0.0.3)

| 신호 | 내용 |
|---|---|
| `DayStarted(int day)` | 하루 첫 손님 전에 "Day N" 날짜 표시를 시작할 때 한 번 |
| `CustomerVisitReady(int customer)` | **손님 방문 시퀀스(문 소리 → 초상화 Fade → 대화)를 시작해도 됨.** 하루 첫 손님은 날짜 표시가 끝난 뒤, 이후 손님은 Counter Fade In이 끝난 뒤 한 번 옵니다. 인자 = `DialogueManager.customer` |

구독 타이밍을 신경 쓰지 않으려면 `CustomerVisit.WhenReady(...)`를 쓰면 됩니다. 이미 준비됐으면 바로, 아니면 준비되는 순간 한 번 호출됩니다.

```csharp
void Start()
{
    CustomerVisit.WhenReady(customer => StartCoroutine(VisitSequence(customer)));
}
```

## 2-1. 시스템 배치

- `Assets/Prefabs/GameSystems.prefab` (`CapitalSystem` + `GameFlowManager` + `JudgementSystem`)를 게임 씬에 하나 둡니다.
  - `JudgementSystem`: `OrderAccepted`로 받은 손님의 `KeyringServed`만 판정해 `KeyringJudged`를 보냅니다. 주문 없는 서빙·중복 서빙은 무시합니다. 서빙 전에 반드시 `RaiseOrderAccepted`를 보내 주세요.
  - `TipFeedback` (Game 씬 임시 UI): 판정 결과 중 **팁 유무만** 2초간 표시 (`Tip!` / `No Tip`)
  - `CapitalSystem.startingCapital` = $10, `GameFlowManager.customersPerGame` = 3 (Inspector에서 변경)
  - 승패 (v0.0.1): 종료 시 자본 ≥ 0 이면 승리
- 개발용 `FlowDebugKeys` (Game 씬에 붙어 있음): 1 주문 / 2 재료 / 3 정답 서빙 / 4 오답 서빙 / 5 손님 완료. 실제 시스템이 붙으면 제거

## 3. 판정 (`KeyringJudge.Judge(recipe, keyring)`)

구현비율 = (일치 개수 − 불일치 개수) / 레시피 총 개수 · 체인은 판정에서 제외

| 결과 (`JudgementGrade`) | 조건 | 자본 변화 (`CapitalDelta`) |
|---|---|---|
| `Perfect` | 100% | +판매가 × 10% (팁) |
| `Pass` | 80% 이상 | 0 (판매가는 선지급됨) |
| `Refund` | 80% 미만, 레시피가 비어 0으로 나누는 경우 | −판매가 (환불) |

- 일치 = 재료별 `min(키링 개수, 레시피 개수)`의 합, 불일치 = 재료별 레시피보다 많이 단 개수의 합. 레시피 재료가 빠진 경우는 일치 개수가 줄어듭니다.
- 상수: `KeyringJudge.TipRate = 0.1`, `KeyringJudge.PassRatio = 0.8`

## 4. 미정 (TBD)

- 체인을 판정 개수에 넣을지 (현재: 제외)
- 레시피 4·7은 재료가 2개라 `Pass`(80~99%)가 나올 수 없음 → 단계 조정 여부
- 돈 단위 (현재 `float` 달러)

_작성: 2026-11-09_
