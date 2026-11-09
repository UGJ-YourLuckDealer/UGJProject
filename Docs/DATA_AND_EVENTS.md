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
