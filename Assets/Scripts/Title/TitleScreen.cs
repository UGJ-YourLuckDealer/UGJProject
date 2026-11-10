using TMPro;
using UnityEngine;

/// <summary>
/// v0.0.1 Title 씬: 버전 텍스트 표시 + 게임 시작 버튼으로 새 게임 시작 (Counter 씬).
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;

    private void Start()
    {
        if (versionText != null)
            versionText.text = "v" + Application.version;
    }

    public void OnStartButtonClicked()
    {
        if (ScreenFader.IsBusy) return;
        DayCycle.BeginNewGame();
        SceneFlow.StartNewGame();
    }
}
