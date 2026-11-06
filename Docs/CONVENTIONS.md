# 프로젝트 컨벤션

> 대상: 팀 전원 (개발·아트·기획) + AI 코딩 도구
> 원칙: **"hermit이 패키지를 임포트했을 때 아무것도 덮어쓰지 않고, 바로 동작한다."**

- [1. 프로젝트 설정](#1-프로젝트-설정)
- [2. 팀원 패키지 전달 규칙](#2-팀원-패키지-전달-규칙) ⭐ 가장 중요
- [3. 폴더 구조](#3-폴더-구조)
- [4. 에셋 네이밍](#4-에셋-네이밍)
- [5. 코드 컨벤션](#5-코드-컨벤션)
- [6. 씬·프리팹 규칙](#6-씬프리팹-규칙)
- [7. Git 규칙 (hermit 전용)](#7-git-규칙-hermit-전용)
- [8. 외부 에셋·AI 사용 기록](#8-외부-에셋ai-사용-기록)

---

## 1. 프로젝트 설정

| 항목 | 값 |
|---|---|
| Unity | **6000.3.27f2** (6.3 LTS) — 모두 동일 버전 필수 |
| 템플릿 | Universal 2D (URP 2D Renderer) |
| Input | Input System 패키지 (새 입력 시스템) |
| UI/텍스트 | uGUI + TextMeshPro |
| 기준 해상도 | 1920×1080 (16:9), Canvas Scaler: Scale With Screen Size |
| PPU (Pixels Per Unit) | TBD (아트 방향 확정 후 통일) |
| Editor 설정 | Version Control: **Visible Meta Files** / Asset Serialization: **Force Text** (Unity 6 기본값) |

- 패키지(Package Manager) 추가는 **hermit과 상의 후** 진행합니다. 팀원이 새 패키지를 쓰면 메인 프로젝트에도 설치해야 하므로, 전달 노트에 꼭 적어 주세요.

---

## 2. 팀원 패키지 전달 규칙

### 2-1. 전체 흐름

```
hermit: Base 패키지 배포 ──▶ 팀원: Base 임포트 후 작업
                                  │
팀원: 자기 폴더만 Export ◀────────┘
   │
   ▼
hermit: 백업 브랜치 → Import → 확인 → 메인 씬에 연결 → 커밋
   │
   ▼
hermit: 새 Base 패키지 배포 (변경이 클 때)
```

### 2-2. 시작 전 준비 (팀원)

1. Unity **6000.3.27f2**, **Universal 2D** 템플릿으로 새 프로젝트를 만듭니다.
2. hermit이 준 **`Base_vXX.unitypackage`** 를 임포트합니다.
   - 공용 스크립트·폰트·공용 스프라이트가 들어 있습니다.
   - Base를 통해 받은 파일은 **GUID(.meta)가 메인 프로젝트와 같기 때문에**, 이걸 참조해야 임포트 후에도 연결이 깨지지 않습니다.
3. 새 Base가 오면 **이전 Base 폴더를 지우고 새로 임포트**합니다.

### 2-3. 작업 폴더 규칙

팀원은 **자기 작업 폴더 안에서만** 파일을 만들고 수정합니다.

```
Assets/_Project/Features/<기능명>/      ← 기능 단위 작업 (개발)
Assets/_Project/Art/<카테고리>/<기능명>/  ← 아트 작업
```

- 예: `Assets/_Project/Features/CustomerHint/`, `Assets/_Project/Art/Characters/Golem/`
- 기능명은 hermit과 미리 정합니다 (작업 배정 시 폴더명을 함께 지정).
- 테스트용 씬은 자기 폴더 안에 `Scenes/` 를 만들어 둡니다. (예: `Features/CustomerHint/Scenes/SCN_Test_CustomerHint.unity`)

### 2-4. 하지 말아야 할 것 ❌

| 금지 | 이유 |
|---|---|
| `_Project/Core/`, `_Project/Shared/`, `ThirdParty/` 등 **공용 폴더 파일 수정** | 임포트 시 hermit 쪽 최신 코드를 덮어씀 |
| 메인 씬(`_Project/Scenes/`) 수정 | 씬은 병합 불가 → hermit이 직접 연결 |
| 파일을 **복사-붙여넣기로 이름만 바꿔** Base 에셋 복제 | 새 GUID가 생겨 참조가 갈림 |
| `.meta` 파일 삭제·재생성 | GUID가 바뀌어 프리팹/씬 참조가 끊어짐 |
| Tags / Layers / Sorting Layers / Physics 설정 임의 추가 | **`.unitypackage`에 포함되지 않음** — 메인에서 깨짐 |
| 같은 이름의 클래스 생성 (네임스페이스 없이) | 컴파일 에러 |

> 공용 파일 수정이나 Tag/Layer/Sorting Layer 추가가 필요하면 **hermit에게 먼저 요청**하세요. hermit이 메인에 반영하고 새 Base를 배포합니다.

### 2-5. 네임스페이스

- 루트 네임스페이스: `LuckyShop` (가제. 타이틀 확정 후에도 **변경하지 않음**)
- 팀원 기능: `LuckyShop.Features.<기능명>` (예: `LuckyShop.Features.CustomerHint`)
- 공용: `LuckyShop.Core`, `LuckyShop.UI`, `LuckyShop.Data`
- 모든 `.cs` 파일은 네임스페이스 안에 작성합니다.

### 2-6. 익스포트 방법

1. Project 창에서 **자기 작업 폴더**를 우클릭 → **Export Package...**
2. **Include dependencies 체크 해제** 상태로 목록 확인
   - 체크하면 Base·공용 파일까지 딸려 들어가 메인 파일을 덮어쓸 수 있습니다.
   - 내 폴더 밖 파일이 필요하다면, 그건 Base에 있어야 하는 파일입니다 → hermit에게 문의.
3. 목록에 **내 폴더 파일만** 있는지 확인 후 Export
4. 파일명 규칙:

```
YYMMDD_이름_기능명_v번호.unitypackage
예) 261108_minsu_CustomerHint_v01.unitypackage
```

### 2-7. 전달 노트 (패키지와 함께 필수)

패키지와 같이 아래 내용을 메시지(또는 `.txt`)로 보내 주세요. 복사해서 채우면 됩니다.

```
[패키지] 261108_minsu_CustomerHint_v01.unitypackage
[Base 버전] Base_v02
[내용] 손님 힌트 말풍선 + 힌트 데이터 SO 3개
[테스트 방법] Features/CustomerHint/Scenes/SCN_Test_CustomerHint 열고 Play → 스페이스바로 손님 호출
[메인 연결 시 필요한 것] GameManager에서 HintController.Show(hint) 호출 필요
[필요한 Tag/Layer/Sorting Layer] Sorting Layer "UI_World" 추가 필요
[추가 패키지(Package Manager)] 없음
[외부 에셋 사용] 없음 / 있다면 이름·링크·라이선스
[생성형 AI 사용] 예: Claude로 HintController.cs 초안 작성, 이미지 생성 없음
[알려진 문제] 말풍선 길이가 길면 잘림
```

> `외부 에셋 사용`, `생성형 AI 사용`은 **잼 규칙상 제출 페이지에 기재해야** 하므로 꼭 적어 주세요. hermit이 [CREDITS.md](CREDITS.md), [AI_USAGE.md](AI_USAGE.md)에 옮깁니다.

### 2-8. 통합 절차 (hermit)

1. Git 작업 트리 깨끗한지 확인 → `integrate/<기능명>` 브랜치 생성
2. **Import Package** 창에서 **NEW 표시가 없는 항목**(= 기존 파일 덮어쓰기)이 있는지 확인. 있으면 체크 해제하거나 팀원에게 문의
3. 컴파일 에러 확인 → 테스트 씬 실행
4. 전달 노트의 Tag/Layer/패키지 반영 → 메인 씬에 연결
5. CREDITS / AI_USAGE 갱신 → 커밋 → main 병합
6. 공용 변경이 있었다면 새 `Base_vXX` 배포 (`Assets/_Project/Core`, `Shared`, `ThirdParty` 등을 Include dependencies 해제 후 익스포트)

받은 원본 패키지는 repo 루트의 `_Incoming/` 에 보관합니다 (`.gitignore` 처리됨).

---

## 3. 폴더 구조

```
Assets/
├─ _Project/                     # 우리 게임 에셋 전부 (언더스코어로 맨 위 정렬)
│  ├─ Core/                      # 핵심 시스템 (GameManager, 하루 루프, 자본, 세이브) — hermit
│  ├─ Shared/                    # 공용 유틸, 공용 컴포넌트, 공용 프리팹 — hermit
│  ├─ UI/                        # 공용 UI (HUD, 메뉴 연결부)
│  ├─ Data/                      # ScriptableObject 데이터 (손님, 힌트, 물품, 업그레이드)
│  ├─ Features/<기능명>/          # 기능 단위 작업 (팀원 작업 영역)
│  │   ├─ Scripts/  Prefabs/  Data/  Scenes/
│  ├─ Art/
│  │   ├─ Characters/  Items/  Backgrounds/  UI/  VFX/
│  ├─ Audio/
│  │   ├─ BGM/  SFX/
│  ├─ Fonts/                     # 한글 TMP 폰트 에셋
│  ├─ Scenes/                    # 빌드에 들어가는 메인 씬 — hermit만 수정
│  └─ Settings/                  # URP 에셋, 2D Renderer 등
├─ ThirdParty/                   # 외부 에셋, 메뉴 템플릿 (원본 유지)
└─ (Plugins/, TextMesh Pro/ 등 자동 생성 폴더는 그대로 둠)
```

- 메인 메뉴 템플릿(antfarmar)은 `ThirdParty/GameJamMenuTemplate/` 에 두고, 수정이 필요한 부분은 가능하면 `_Project/UI/`에서 래핑/상속합니다.
- 폴더명·파일명에 **한글·공백 금지** (빌드/플랫폼 이슈 방지).

---

## 4. 에셋 네이밍

형식: `접두사_이름_변형` (PascalCase, 영문)

| 종류 | 접두사 | 예시 |
|---|---|---|
| 씬 | `SCN_` | `SCN_MainMenu`, `SCN_Shop`, `SCN_Test_CustomerHint` |
| 프리팹 | `PF_` | `PF_Customer_Golem`, `PF_Item_CandyJar` |
| 스프라이트 / 텍스처 | `SPR_` | `SPR_Golem_Idle`, `SPR_Item_Sticker_07` |
| 스프라이트 시트 | `SPR_` + `_Sheet` | `SPR_Golem_Sheet` |
| 애니메이션 클립 | `ANIM_` | `ANIM_Golem_Happy` |
| 애니메이터 컨트롤러 | `AC_` | `AC_Customer` |
| 머티리얼 | `MAT_` | `MAT_Outline` |
| ScriptableObject 인스턴스 | `SO_` | `SO_Customer_Golem`, `SO_Hint_Number7` |
| 배경음 / 효과음 | `BGM_` / `SFX_` | `BGM_Shop_Day`, `SFX_Coin_Tip` |
| 폰트 에셋 | `FNT_` | `FNT_NotoSansKR_SDF` |
| 스크립트 | 접두사 없음 | `CustomerController.cs` (클래스명 = 파일명) |

- 숫자는 두 자리로: `_01`, `_02`
- 아트 원본(`.psd`, `.aseprite`)은 repo에 넣지 않고 팀 공유 드라이브(TBD)에 보관합니다. 게임에는 `.png`만.

---

## 5. 코드 컨벤션

### 5-1. 스타일

```csharp
using UnityEngine;

namespace LuckyShop.Features.CustomerHint
{
    /// <summary>손님 머리 위에 힌트 말풍선을 띄운다.</summary>
    public class HintBubble : MonoBehaviour
    {
        private const float DefaultShowTime = 3f;      // 상수: PascalCase

        [SerializeField] private TMPro.TMP_Text _label; // 인스펙터 노출: [SerializeField] private
        [SerializeField] private float _showTime = DefaultShowTime;

        private bool _isShowing;                        // private 필드: _camelCase

        public bool IsShowing => _isShowing;            // 프로퍼티: PascalCase

        public event System.Action<HintBubble> Closed;  // 이벤트: PascalCase, 과거형/동사

        public void Show(string text)                   // 메서드: PascalCase
        {
            _label.text = text;
            _isShowing = true;
        }
    }
}
```

| 대상 | 규칙 |
|---|---|
| 클래스·메서드·프로퍼티·이벤트 | `PascalCase` |
| private 필드 | `_camelCase` |
| 지역 변수·매개변수 | `camelCase` |
| 인터페이스 | `I` + PascalCase (`IDraggable`) |
| bool | `is/has/can` 접두 (`_isShowing`, `CanDrop`) |
| 파일 | 한 파일 = 한 public 클래스, 파일명 = 클래스명 |
| 주석 | 한국어 OK. 식별자는 영어 |

### 5-2. 구조 원칙

- **public 필드 금지** → `[SerializeField] private` + 필요 시 프로퍼티.
- **게임 데이터는 ScriptableObject**로 분리 (손님, 힌트, 물품, 가격, 업그레이드). 기획이 수치를 인스펙터에서 바로 바꿀 수 있게.
- 시스템 간 연결은 **C# event / UnityEvent** 우선. `GameObject.Find`, 문자열 기반 탐색 금지.
- 싱글턴은 `Core`의 매니저(예: `GameManager`)에만 허용. 기능 코드는 싱글턴을 만들지 않습니다.
- `Update()`에서 매 프레임 `GetComponent` / `Find*` 호출 금지 → `Awake`에서 캐싱.
- 드래그앤드롭은 uGUI `IBeginDragHandler / IDragHandler / IDropHandler` 또는 Input System 기반으로 통일 (Core에서 결정 후 기재: TBD).
- 매직 넘버 대신 SO 또는 `[SerializeField]` 값으로.
- Unity 6 API 사용: `FindFirstObjectByType` (`FindObjectOfType` 사용 X), `Rigidbody2D.linearVelocity` (`velocity` X).

### 5-3. 텍스트·한글

- 모든 텍스트는 **TextMeshPro** 사용, 한글 폰트는 `_Project/Fonts/`의 공용 TMP 폰트 에셋 사용.
- 대사·힌트 문자열은 코드에 하드코딩하지 않고 SO에 둡니다.

---

## 6. 씬·프리팹 규칙

- 빌드 씬(`_Project/Scenes/`)은 **hermit만** 수정합니다.
- 팀원은 기능을 **프리팹 단위**로 만들어 전달합니다 → hermit이 씬에 배치.
- 씬에 직접 오브젝트를 많이 두기보다 프리팹 + Prefab Variant 활용.
- 2D 렌더 순서는 **Sorting Layer**로 관리 (목록은 hermit이 Base에서 정의, 확정 시 아래 표 갱신).

| Sorting Layer (순서) | 용도 |
|---|---|
| Background | 배경 |
| Shop | 가게 가구, 카운터 |
| Customer | 손님 |
| Item | 물품 (드래그 중 포함) |
| WorldUI | 말풍선, 게이지 |

---

## 7. Git 규칙 (hermit 전용)

- 기본 브랜치: `main` (항상 실행 가능한 상태 유지)
- 작업 브랜치: `feature/<기능>`, `integrate/<기능>` (팀원 패키지 통합), `fix/<내용>`
- 커밋 메시지: `타입: 내용` (한국어 OK)

| 타입 | 용도 |
|---|---|
| `feat` | 기능 추가 |
| `fix` | 버그 수정 |
| `integrate` | 팀원 패키지 통합 (`integrate: 민수 CustomerHint v01`) |
| `art` / `audio` | 아트·사운드 에셋 추가/교체 |
| `docs` | 문서 |
| `chore` | 설정, 패키지, 기타 |

- Unity 에디터를 닫거나 저장(`Ctrl+S`, `File > Save Project`) 후 커밋합니다.
- 큰 바이너리(100MB 초과)는 GitHub에 올라가지 않습니다. 필요 시 Git LFS 도입 검토.
- 빌드 결과물(`Builds/`)과 `.unitypackage`는 커밋하지 않습니다 (`.gitignore` 처리).

---

## 8. 외부 에셋·AI 사용 기록

- 외부 에셋(구매/무료/폰트/사운드/템플릿)을 넣으면 **즉시** [CREDITS.md](CREDITS.md)에 기록합니다.
- 생성형 AI(코드, 이미지, 사운드, 텍스트)를 사용하면 [AI_USAGE.md](AI_USAGE.md)에 기록합니다.
- 두 기록은 잼 제출 페이지 작성에 그대로 쓰입니다. **누락 시 수상 제외**될 수 있습니다.

---

_최종 수정: 2026-11-06 · 미정(TBD) 항목은 확정되는 대로 갱신_
