using UnityEngine;

public class MakingNoteController : MonoBehaviour
{
    public enum Tab
    {
        Recipe,
        DialogueHistory,
    }

    [SerializeField] GameObject _recipeTab;
    [SerializeField] GameObject _dialogueHistoryTab;

    public void OpenTab(Tab tab)
    {
        _recipeTab.SetActive(tab == Tab.Recipe);
        _dialogueHistoryTab.SetActive(tab == Tab.DialogueHistory);
    }
}