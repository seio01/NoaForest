using UnityEngine;

public class ToastController
{
    public void ShowToast(string text, Define.ToastPosition position)
    {
        Managers.Resource.LoadAsync<GameObject>("Prefabs/UI_Toast", (prefab) =>
        {
            if (prefab != null)
            {
                CreateToast(prefab, text, position);
            }
            else
            {
                Debug.LogError("[ToastController] UI_Toast prefab is null.");
            }
        });
    }

    private void CreateToast(GameObject prefab, string text, Define.ToastPosition position)
    {
        GameObject go = Object.Instantiate(prefab);
        go.transform.SetParent(Managers.UI.GetorCreateToastRoot().transform, false);
        var uiToast = go.GetComponent<UI_Toast>();
        if(uiToast)
        {
            uiToast.Show(text, position);
        }
    }
}
