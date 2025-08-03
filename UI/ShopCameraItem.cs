using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;

public class ShopCameraItem : MonoBehaviour
{
    public RectTransform cameraName;
    public RectTransform cameraPrice;
    public RectTransform cameraImage;

    public void SetData(CameraData data)
    {
        cameraName.GetComponent<TextMeshProUGUI>().text = data.name;
        cameraPrice.GetComponent<TextMeshProUGUI>().text = data.priceText;
        cameraImage.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>(data.uiPath);
        //cameraImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(data.uiPath);
    }
    public void SetData(LensData data)
    {
        cameraName.GetComponent<TextMeshProUGUI>().text = data.name;
        cameraPrice.GetComponent<TextMeshProUGUI>().text = data.priceText;
        cameraImage.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>(data.uiPath);
        //cameraImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(data.uiPath);
    }
    public void SetData(PlugData data)
    {
        cameraName.GetComponent<TextMeshProUGUI>().text = data.name;
        cameraPrice.GetComponent<TextMeshProUGUI>().text = data.priceText;
        cameraImage.GetComponent<UnityEngine.UI.Image>().sprite = Resources.Load<Sprite>(data.uiPath);
        //cameraImage.GetComponent<Image>().sprite = Resources.Load<Sprite>(data.uiPath);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
