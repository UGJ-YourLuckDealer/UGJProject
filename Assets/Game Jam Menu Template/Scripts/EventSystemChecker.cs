using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class EventSystemChecker : MonoBehaviour
{
	void OnEnable ()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnDisable ()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	//Called after a new scene has finished loading (replaces obsolete OnLevelWasLoaded)
	void OnSceneLoaded (Scene scene, LoadSceneMode mode)
	{
		//If there is no EventSystem (needed for UI interactivity) present
		if(!FindAnyObjectByType<EventSystem>())
		{
			//The following code instantiates a new object called EventSystem
			GameObject obj = new GameObject("EventSystem");

			//And adds the required components
			obj.AddComponent<EventSystem>();
			obj.AddComponent<InputSystemUIInputModule>();
		}
	}
}
