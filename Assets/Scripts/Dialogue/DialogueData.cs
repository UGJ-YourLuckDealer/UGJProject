using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public Sprite characterImage;
    public string character_Name;

    [TextArea(2, 5)]
    public string dialogueText;
    
    public bool isRequest;
    public bool isEnd;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] dialogueLines;
}
