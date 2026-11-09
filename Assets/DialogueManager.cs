using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    public DialogueData[] dialogueData;
    public int customer = 1;

    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI B_ButtonText;
    public UnityEngine.UI.Image characterImage;
    Color color;

    public GameObject requestButton;
    public GameObject nextButton;

    [SerializeField] private int currentIndex = 0;

    private float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    DialogueLine line;

    private bool isWaiting = false;

    private void Start()
    {
        StartDialogue();
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


                    nextButton.SetActive(true);
                if (line.isRequest)
                    requestButton.SetActive(true);
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

        SceneManager.LoadScene("Making");
    }
    void DisplayDialogue()
    {
        typingCoroutine = StartCoroutine(TypeDialogue(line.dialogueText));
        characterImage.sprite = line.characterImage;
        B_ButtonText.text = line.button_B_Text;
    }

    public void StartDialogue()
    {
        line = dialogueData[customer].dialogueLines[currentIndex++];
        color.a = 255f;
        color.b = color.g = color.r = 255f;
        characterImage.color = color;
        DisplayDialogue();
    }

    public void NextDialogue()
    {
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
        if (!line.isEnd && !line.isRequest)
            return;

        currentIndex = 0;

        color.a = 0f;
        dialogueText.text = "";
        characterImage.sprite = null;
        characterImage.color = color;

        requestButton.SetActive(false);
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

        if (line.isRequest)
            requestButton.SetActive(true);

            nextButton.SetActive(true);
        isTyping = false;
        typingCoroutine = null;
    }
}


