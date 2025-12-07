using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EventFramework;

public class CopyPath : MonoBehaviour
{
    // Start is called before the first frame update
    [MenuItem("GameObject/Copy Path", false, 0)]
    static void CopyGameObjectPath(MenuCommand menuCommand)
    {
        GameObject selectedObject = menuCommand.context as GameObject;
        if (selectedObject != null)
        {
            string path = GetGameObjectPath(selectedObject);
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log("selectedObject position: " + selectedObject.transform.position + ", path: " + path);
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            if(obj.transform.parent.GetComponent<UICommon>() != null)
            {
                // If the parent is a UICommon, we stop here
                break;
            }
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}
