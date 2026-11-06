# AI 코딩 지침 (CLAUDE.md)

> Cursor / Claude 등 AI 코딩 도구가 이 프로젝트에서 작업할 때 따르는 명세입니다.
> Cursor는 `.cursor/rules/project.mdc`를 통해 이 파일을 참조합니다.
> 사람이 읽어도 되도록 썼으며, 프로젝트 정보가 확정될 때마다 갱신합니다.

## 0. 응답 규칙

- 사용자에게는 **한국어**로 답합니다. 코드 식별자는 영어, 코드 주석은 한국어 가능.
- 수정 전에 어떤 파일을 왜 바꾸는지 짧게 말하고, 큰 변경(폴더 이동, 패키지 추가, 공용 코드 구조 변경)은 **먼저 제안하고 확인**받습니다.
- 확실하지 않은 Unity API는 추측하지 말고 "확인 필요"라고 표시합니다.

## 1. 프로젝트 맥락

- **게임**: 착한 마녀의 행운 가게 (가제). 판타지 손님의 텍스트 힌트를 추리해 행운 물품을 드래그앤드롭으로 담아 주는 2D 가게 운영 게임.
- **루프**: 하루(현실 5분) → 손님 방문·힌트·인내심 게이지 → 물품 배치 → 검사(팁/환불) → 하루 끝 업그레이드 → 다음 날. 7일차 도달 시 자본 ≥ 0이면 승리, 하루 끝 자본 < 0이면 패배.
- **잼**: Official Unity Game Jam 2026, 마감 **2026-11-14 21:00 KST**. 기간이 짧으므로 **단순하고 동작하는 코드**를 우선합니다. 과도한 추상화·프레임워크 도입 금지.
- **미정(TBD)**: 잼 주제, 타이틀, 손님 종류, 힌트↔물품 대응표, 가격 구조. → 이 값들은 하드코딩하지 말고 ScriptableObject로 빼서 나중에 바꾸기 쉽게.

## 2. 기술 환경

| 항목 | 값 |
|---|---|
| Unity | **6000.3.27f2 (Unity 6.3 LTS)** |
| 렌더 | URP + 2D Renderer (Universal 2D 템플릿) |
| 입력 | Input System 패키지 |
| UI | uGUI + TextMeshPro |
| 메인 메뉴 | antfarmar/Unity-Game-Jam-Menu-Template 기반, `Assets/ThirdParty/` |
| 대상 | Windows 빌드 (itch.io), WebGL은 TBD |

### Unity 6 API 주의

- `FindObjectOfType` ❌ → `FindFirstObjectByType` / `FindAnyObjectByType` ✅
- `FindObjectsOfType` ❌ → `FindObjectsByType(FindObjectsSortMode.None)` ✅
- `Rigidbody2D.velocity` ❌ → `linearVelocity` ✅ (`drag` → `linearDamping`)
- 구 Input Manager(`Input.GetKey`, `Input.mousePosition`) ❌ → Input System(`Mouse.current`, `Keyboard.current`, InputAction) ✅
- `UnityEngine.UI.Text` ❌ → `TMPro.TMP_Text` ✅
- 2D 조명/렌더러 설정은 URP 2D 기준 (Built-in 파이프라인 코드·셰이더 사용 금지)

## 3. 반드시 지킬 규칙

전체 규칙은 [Docs/CONVENTIONS.md](Docs/CONVENTIONS.md)를 따릅니다. 핵심만 요약:

1. **네임스페이스**: 모든 스크립트는 `LuckyShop.*` 안에. 공용 `LuckyShop.Core / UI / Data`, 기능 `LuckyShop.Features.<기능명>`.
2. **폴더**: 새 파일은 `Assets/_Project/` 하위 정해진 위치에만. 기능 작업은 `Assets/_Project/Features/<기능명>/{Scripts,Prefabs,Data,Scenes}/`.
3. **네이밍**: 클래스/메서드 PascalCase, private 필드 `_camelCase`, 에셋 접두사(`PF_`, `SPR_`, `SO_`, `SCN_` …).
4. **필드**: public 필드 금지 → `[SerializeField] private` + 프로퍼티.
5. **데이터**: 손님·힌트·물품·가격·업그레이드는 ScriptableObject (`[CreateAssetMenu(menuName = "LuckyShop/...")]`).
6. **연결**: C# event / UnityEvent 우선. `GameObject.Find`, 문자열 태그 탐색 금지. 싱글턴은 `Core` 매니저만.
7. **한 파일 = 한 public 클래스**, 파일명 = 클래스명.

## 4. 하지 말아야 할 것

- `.meta` 파일을 직접 만들거나 수정·삭제하지 않습니다 (Unity가 생성. GUID가 깨지면 참조가 끊어짐).
- `.unity`, `.prefab`, `.asset` YAML을 손으로 작성/편집하지 않습니다. 씬·프리팹 구성은 **에디터에서 할 작업 단계를 안내**합니다.
- `ProjectSettings/`, `Packages/manifest.json` 변경은 사용자 확인 후에만.
- `Assets/ThirdParty/` 원본 수정 금지 → `_Project/`에서 상속·래핑.
- `Assets/_Project/Scenes/`(빌드 씬)을 전제로 하는 코드를 팀원 기능에 넣지 않습니다. 기능은 프리팹 단위로 독립 동작해야 합니다.
- Tag/Layer/Sorting Layer를 새로 쓰는 코드라면 **"추가 필요한 설정"으로 명시**합니다 (`.unitypackage`에 포함되지 않음).
- 외부 패키지·에셋을 임의로 추가하지 않습니다.

## 5. 작업 방식

- 기능을 만들 때 출력 순서:
  1. 만들/바꿀 파일 목록과 위치
  2. 코드
  3. **에디터 설정 단계** (어떤 오브젝트에 어떤 컴포넌트, 인스펙터에 무엇을 연결하는지)
  4. 테스트 방법
- 디버그 로그는 `Debug.Log($"[클래스명] ...")` 형식. 제출 전 불필요한 로그 정리.
- 에러 해결 요청 시: 원인 → 수정 코드 → 재발 방지 순으로 짧게.
- 성능보다 **가독성과 수정 용이성** 우선 (잼 규모).

## 6. 팀원(패키지 작업자)용 추가 지침

팀원 프로젝트에서 AI를 쓸 때는 다음을 지킵니다.

- 작업 범위는 배정받은 `Features/<기능명>/` (또는 `Art/.../<기능명>/`) 폴더 **안쪽만**.
- `Core`, `Shared`, `UI` 등 공용 코드는 **읽고 호출만** 하고 수정하지 않습니다. 수정이 필요하면 "hermit에게 요청할 변경사항"으로 따로 정리합니다.
- 작업이 끝나면 [전달 노트 양식](Docs/CONVENTIONS.md#2-7-전달-노트-패키지와-함께-필수)을 채우는 것을 돕습니다.

## 7. 생성형 AI 사용 기록

잼 규칙상 생성형 AI 사용처를 공개해야 합니다(미기재 시 수상 제외).
AI로 의미 있는 분량의 코드·텍스트·이미지·사운드를 만들었다면, 작업 끝에 **[Docs/AI_USAGE.md](Docs/AI_USAGE.md)에 한 줄 추가할 내용을 제안**합니다.

---

_최종 수정: 2026-11-06_
