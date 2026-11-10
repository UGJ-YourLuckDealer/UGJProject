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