# Result 씬 UI 수정 가이드

Result 씬(`Assets/Scenes/Result.unity`)의 UI를 고칠 때 바꿔도 되는 것과 피해야 하는 것을 정리했습니다.
Result 씬은 주로 **게임이 끝날 때**(7일 클리어 = WIN, 파산 = LOSE) 나옵니다. 하루가 끝날 때는 Result 씬 대신 하루 정산 팝업(Settlement Popup)이 뜹니다.

## 씬 구조

```
Main Camera
EventSystem
Canvas
├─ Background      (Image: SampleBG.png)   배경
├─ ResultTitle     (TMP)                   제목: 코드가 WIN / LOSE / Day N Clear로 바꿈
├─ ResultDetail    (TMP)                   본문: 코드가 정산표 내용으로 채움
└─ TitleButton     (Button → GoToTitle)    버튼: 하루 끝일 땐 코드가 Next Day 버튼으로 바꿈
    └─ Text        (TMP: "Title")
ResultScreen       (ResultScreen.cs, SceneFlowLink.cs, GameOverDialogue.cs)   로직 (화면에 안 보임)
Dialogue_Canvas (Game Over)   (Dialogue_Canvas 1 프리팹, 평소 꺼짐)          파산 시 "Bankrupt!" 대사창
```

## 마음대로 바꿔도 되는 것 ✅

- `Background`, `ResultTitle`, `ResultDetail`, `TitleButton`(+ 안의 `Text`)의 **위치·크기·색·폰트·글자 크기·정렬**
- `Background`의 **Sprite 교체**
- `Canvas` 아래에 **새 오브젝트 추가** (장식 이미지, Lose UI 그림 등)
- Hierarchy **순서 변경** (겹치는 순서)

## 피해야 하는 것 / 주의할 것 ⚠️

| 항목 | 이유 / 대신 할 것 |
|---|---|
| `TitleButton` **이름 바꾸기** | `ResultScreen.cs`가 이름으로 버튼을 찾아 하루 끝일 때 "Next Day" 버튼으로 바꿈. 바꿔야 하면 `ResultScreen` Inspector의 **Title Button** 칸에 직접 연결 |
| `ResultTitle`, `ResultDetail` **지우기** | `ResultScreen`의 **Title Text / Detail Text** 칸에 연결되어 있음. 다른 텍스트를 쓰려면 새 텍스트를 이 칸에 연결 |
| 텍스트 안의 글자 **직접 고치기** | 실행 중에 코드가 덮어씀. 문구는 `ResultScreen` Inspector의 `Day Clear Format`, `Next Day Label`, `Survive Message`, `Game Over Message`에서, 색은 `Good Color`/`Bad Color`, 값 위치는 `Value Column`에서 수정 |
| `TitleButton`의 OnClick(`GoToTitle`) **지우기** | 게임 종료 후 타이틀로 가는 동작. 하루 끝일 때는 코드가 실행 중에만 바꾸므로 씬에서는 그대로 둠 |
| `ResultScreen` 오브젝트와 **컴포넌트 3개** | 결과 표시·씬 이동·파산 연출 코드. 지우거나 끄지 않기 |
| `Dialogue_Canvas (Game Over)` | 파산 시 "Bankrupt!" 대사창. **꺼진 상태가 정상**. 안의 `Desk_Image (1)`도 오류(ImageOutline) 때문에 일부러 꺼 둔 것. 모양은 프리팹 원본이 아니라 이 씬의 인스턴스에서만 수정 |

## 문의

Result 씬 로직(`ResultScreen.cs`, `GameOverDialogue.cs`, `SceneFlowLink.cs`)은 hermit 담당입니다.

---

_작성: 2026-11-14_
