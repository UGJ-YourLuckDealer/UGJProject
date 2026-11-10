using System.Collections.Generic;
using UnityEngine;

public class Check_Board : MonoBehaviour
{
    private HashSet<GameObject> insideObjects = new HashSet<GameObject>();

    private Dictionary<int, HashSet<GameObject>> countedObjects = new Dictionary<int, HashSet<GameObject>>();

    private HashSet<GameObject> draggingObjects = new HashSet<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject obj = other.gameObject;

        if (!insideObjects.Add(obj))
            return;


        if (!draggingObjects.Contains(obj))
            AddCount(obj);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject obj = other.gameObject;
        if (!insideObjects.Remove(obj))
            return;

        RemoveCount(obj);
    }

    private void AddCount(GameObject obj)
    {
        if (obj.GetComponent<ObjectInteraction>() != null) return; // 집어 오는 원본 재료는 세지 않음

        int layer = obj.layer;
        string tag = obj.tag;


        if(!countedObjects.ContainsKey(layer))
            countedObjects[layer] = new HashSet<GameObject>();

        countedObjects[layer].Add(obj);

        Debug.Log("Add: " +LayerMask.LayerToName(layer) + ":" + GetCount(layer, tag));
    }

    private void RemoveCount(GameObject obj)
    {
        int layer = obj.layer;
        string tag = obj.tag;

        if (countedObjects.ContainsKey(layer))
        {
            countedObjects[layer].Remove(obj);

            Debug.Log("Remove: " +LayerMask.LayerToName(layer) + ":" + GetCount(layer, tag));
        }
    }

    public void StartDragging(GameObject obj)
    {
        if (!draggingObjects.Add(obj))
            return;

        RemoveCount(obj);
    }

    public void EndDragging(GameObject obj)
    {
        if(!draggingObjects.Remove(obj))
            return;

        if (insideObjects.Contains(obj))
            AddCount(obj);
    }

    public int GetCount(int layer, string tag)
    {
        if(!countedObjects.ContainsKey((int)layer))
            return 0;

        int count = 0;

        foreach(GameObject obj in countedObjects[layer])
        {
            if (obj != null && obj.CompareTag(tag))
                count++;
        }
        return count;
    }
}
