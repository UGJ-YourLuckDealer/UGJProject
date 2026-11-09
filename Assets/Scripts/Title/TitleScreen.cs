using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// v0.0.1 Title 씬: 버전 텍스트 표시 + 게임 시작 버튼으로 씬 전환.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private Text versionText;

    private void Start()
    {
        if (versionText != null)
            versionText.text = Application.version;
    }

    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
