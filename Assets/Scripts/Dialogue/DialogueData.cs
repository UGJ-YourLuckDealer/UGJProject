using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public Sprite characterImage;

    [TextArea(2, 5)]
    public string dialogueText;

    [TextArea(1, 5)]
    public string button_B_Text;


    public bool isRequest;
    public bool isEnd;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] dialogueLines;
}
