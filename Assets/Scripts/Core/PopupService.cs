<<<<<<< HEAD
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Popup UI 생성 및 제거 관리
/// </summary>
public class PopupService : MonoBehaviour
{
    [SerializeField] ResourceDB _resourceDB;
    [SerializeField] Canvas _worldCanvas;

    Dictionary<string, GameObject> _activePopups = new();

    /// <summary>
    /// 열려 있는 팝업 중 가장 최근 것 (없으면 null).
    /// 팝업이 스스로 Destroy된 경우(예: SettingsPopupUI.Hide)도 닫힌 것으로 본다.
    /// </summary>
    public GameObject TopPopup
    {
        get
        {
            GameObject top = null;
            foreach (var popup in _activePopups.Values)
            {
                if (popup != null) top = popup;
            }
            return top;
        }
    }

    public bool IsAnyPopupOpen => TopPopup != null;

    public void ShowPopup(string key)
    {
        ClosePopup(key);

        if (_resourceDB.popupDict.TryGetValue(key, out var prefab))
        {
            var popup = Instantiate(
                prefab,
                _worldCanvas.transform,
                worldPositionStays: false);
            _activePopups.Add(key, popup);
        }
    }

    public void ClosePopup(string key)
    {
        if (_activePopups.TryGetValue(key, out var popup))
        {
            // popup game object가 destroy 되지 않은 경우에만 호출
            if (popup != null)
            {
                Destroy(popup);
            }

            _activePopups.Remove(key);
        }
    }
}
=======
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Popup UI 생성 및 제거 관리
/// </summary>
public class PopupService : MonoBehaviour
{
    [SerializeField] ResourceDB _resourceDB;
    [SerializeField] Canvas _popupRootCanvas;

    Dictionary<string, GameObject> _activePopups = new();

    public void ShowPopup(string key)
    {
        ClosePopup(key);

        if (_resourceDB.popupDict.TryGetValue(key, out var prefab))
        {
            var popup = Instantiate(
                prefab,
                _popupRootCanvas.transform,
                worldPositionStays: false);
            _activePopups.Add(key, popup);
        }
    }

    public void ClosePopup(string key)
    {
        if (_activePopups.TryGetValue(key, out var popup))
        {
            // popup game object가 destroy 되지 않은 경우에만 호출
            if (popup != null)
            {
                Destroy(popup);
            }

            _activePopups.Remove(key);
        }
    }
}
>>>>>>> a69d996 (dialogue history 팝업 UI 프리팹 추가)
