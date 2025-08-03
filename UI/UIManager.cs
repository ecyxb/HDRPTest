using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum CanvasType
{
    PanelScreenOutput = -1,
    PanelBase = 1,
    PanelMain = 2,

}
public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update

    private Dictionary<int, Canvas> uiCanvases = new Dictionary<int, Canvas>();
    private Dictionary<string, PanelBase> __panels__ = new Dictionary<string, PanelBase>();


    public PanelBase this[string uiName]
    {
        get
        {
            return this.__panels__?.GetValueOrDefault(uiName, null);
        }
    }
    public GameObject InitPanel(string panelName)
    {
        GameObject panelObject = Resources.Load<GameObject>("UIPrefabs/" + panelName);
        if (panelObject != null)
        {
            var obj = Object.Instantiate(panelObject);
            var panelBase = obj.GetComponentInChildren<PanelBase>();
            __panels__[panelName] = panelBase;
            LoadPanel(panelBase);

        }
        return null;
    }

    public void LoadPanel(PanelBase panel)
    {
        var canvasType = panel.CanvasType;
        if (!uiCanvases.TryGetValue((int)canvasType, out var existingCanvas))
        {
            GameObject canvasObject = Instantiate(Resources.Load<GameObject>("UIPrefabs/Canvas" + canvasType));
            existingCanvas = canvasObject.GetComponent<Canvas>();
            existingCanvas.sortingOrder = (int)panel.CanvasType;
            uiCanvases.Add((int)canvasType, existingCanvas);
        }
        panel.gameObject.transform.SetParent(existingCanvas.transform, false);
        panel.__auto_attach__();
        panel.__on_load__();
        panel.Show();
    }

    public Vector2 UIWorldPos2ScreenPos(Vector2 position)
    {
        return RectTransformUtility.WorldToScreenPoint(null, position);
    }
    public Vector3 ScreenPos2UIWorldPos(Vector2 position, RectTransform rectTransform)
    {
        return RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, position, null, out Vector3 worldPos) ? worldPos : Vector3.zero;
    }
}
