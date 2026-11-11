using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 대화 로그 기록 (hermit). 씬에 있는 팀원 DialogueManager가 지금 보여 주는 대사(line, 제출 후 반응 bifurcation)가
/// 바뀔 때마다 GameSession.DialogueLog에 한 줄씩 남긴다. 팀원 코드·씬은 수정하지 않고 값만 읽는다.
/// GameSystems 프리팹(씬 전환 후에도 유지)에 붙는다.
/// </summary>
public class DialogueLogRecorder : MonoBehaviour
{
    private DialogueManager dialogue;
    private DialogueLine lastLine;
    private DialogueBifurcation lastBifurcation;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Start() => Attach();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) Attach();
    }

    // 씬에 들어올 때 DialogueManager를 찾고, 씬에 저장돼 있던 대사(아직 보여 주기 전 값)는 기록하지 않도록 기준으로 잡는다
    private void Attach()
    {
        var found = FindAnyObjectByType<DialogueManager>();
        if (found == dialogue) return;
        dialogue = found;
        lastLine = dialogue != null ? dialogue.line : null;
        lastBifurcation = dialogue != null ? dialogue.bifurcation : null;
    }

    private void LateUpdate()
    {
        if (dialogue == null) return;

        if (dialogue.line != null && dialogue.line != lastLine)
        {
            lastLine = dialogue.line;
            string speaker = !string.IsNullOrEmpty(lastLine.character_Name) ? lastLine.character_Name : NameOnScreen();
            GameSession.AddDialogueLog(speaker, lastLine.dialogueText, lastLine.characterImage, dialogue.customer);
        }

        if (dialogue.bifurcation != null && dialogue.bifurcation != lastBifurcation)
        {
            lastBifurcation = dialogue.bifurcation;
            GameSession.AddDialogueLog(NameOnScreen(), lastBifurcation.bifuractionText, lastBifurcation.characterImage, dialogue.customer, isReaction: true);
        }
    }

    private string NameOnScreen()
    {
        TMP_Text nameText = dialogue.characterName;
        return nameText != null ? nameText.text : "";
    }
}
