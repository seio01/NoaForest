using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System;

public class Utils
{
    public static T GetorAddComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }   

    public static T FindChild<T>(GameObject parent, string name, bool includeInactive) where T : Component
    {
        if(parent == null) return null;

        foreach(Transform child in parent.transform)
        {
            if(child.name == name)
            {
                T component = child.GetComponent<T>();
                if(component != null)
                    return component;
            }

            if(includeInactive || child.gameObject.activeSelf)
            {
                T component1 = FindChild<T>(child.gameObject, name, includeInactive);
                if(component1 != null)
                    return component1;
            }
        }
        return null;
    }

    public static bool IsPointerOverUi(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (!eventSystem)
        {
            return false;
        }

        var pointerEventData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };

        var uiRaycastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerEventData, uiRaycastResults);

        foreach (RaycastResult raycastResult in uiRaycastResults)
        {
            if (raycastResult.module is GraphicRaycaster)
            {
                return true;
            }
        }
        
        return false;
    }

    public static IEnumerator DelayRoutine(float seconds, Action action)
    {
        if(seconds > 0)
        {
            yield return new WaitForSeconds(seconds);
        }

        action?.Invoke();
    }

}
