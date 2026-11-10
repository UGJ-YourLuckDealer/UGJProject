using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    public DialogueData[] dialogueData;
    //Main_Data mainData;
    public int customer = 1;

    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI B_ButtonText;
    public UnityEngine.UI.Image characterImage;
    Color color;

    public GameObject nextButton;

    public GameObject textPanel;

    [SerializeField] private int currentIndex = 0;

    private float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public DialogueLine line;

    private bool isWaiting = false;

    private void Start()
    {
        //mainData = new Main_Data();
        StartDialogue();

        Customer_Data.Set_CustomerReceipt(dialogueData[customer]);
    }

    private void Update()
    {
        

        if(Mouse.current.leftButton.wasPressedThisFrame && !isWaiting)
        {
            if(isTyping == true)
            {
                isTyping = false;
                StopCoroutine(typingCoroutine);

                dialogueText.text = line.dialogueText;

                if(!line.isEnd)
                    nextButton.SetActive(true);
                
            }
        }
    }

    public void ReStartDialogue()
    {
        isWaiting = false;
        customer++;

        if(customer < dialogueData.Length)
        {
            line = dialogueData[customer].dialogueLines[currentIndex];
            StartDialogue();

            color.a = 255f;
            color.b = color.g = color.r = 255f;
            characterImage.color = color;
            return;
        }
        
    }
    void DisplayDialogue()
    {
        typingCoroutine = StartCoroutine(TypeDialogue(line.dialogueText));
        characterImage.sprite = line.characterImage;
    }

    public void StartDialogue()
    {
        textPanel.SetActive(true);

        line = dialogueData[customer].dialogueLines[currentIndex++];

        characterName.text = line.character_Name;
        color.a = 255f;
        color.b = color.g = color.r = 255f;
        characterImage.color = color;
        DisplayDialogue();
    }

    public void NextDialogue()
    {


        if (line.isEnd)
            return;

        if (currentIndex < dialogueData[customer].dialogueLines.Length)
        {
            line = dialogueData[customer].dialogueLines[currentIndex++];
            DisplayDialogue();
        }
        else
        {
            EndDialogue();
            Debug.Log("Dialogues is END");
            return;
        }


    }

    public void EndDialogue()
    {
        if (!line.isEnd)
            return;

        currentIndex = 0;

        color.a = 0f;
        dialogueText.text = "";
        characterImage.sprite = null;
        characterImage.color = color;

        textPanel.SetActive(false);

        nextButton.SetActive(false);

        isWaiting = true;

        Invoke(nameof(ReStartDialogue), 2f);
    }

    IEnumerator TypeDialogue(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        nextButton.SetActive(false);

        foreach (char letter in text)
        {
            dialogueText.text += letter;

            yield return new WaitForSeconds(typingSpeed);
        }

        if(!line.isEnd)
            nextButton.SetActive(true);
        isTyping = false;
        typingCoroutine = null;
    }
}


