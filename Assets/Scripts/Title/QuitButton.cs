using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title 씬 Quit(게임 종료) 버튼. PC 빌드에서만 보이고, 웹(WebGL) 빌드에서는 스스로 숨긴다.
/// 에디터에서는 Play를 멈춘다.
/// </summary>
[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    /// <summary>게임 종료를 지원하는 플랫폼인지 (웹은 브라우저 탭이라 종료 불가).</summary>
    public static bool IsQuitSupported => Application.platform != RuntimePlatform.WebGLPlayer;

    private void Awake()
    {
        if (!IsQuitSupported)
        {
            gameObject.SetActive(false);
            return;
        }
        GetComponent<Button>().onClick.AddListener(Quit);
    }

    public void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
