using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PanelCameraShop : PanelBase
{
    public RectTransform leftCameraItem;
    public RectTransform shopScrollView;
    public RectTransform shopTitle;
    public CustomButton btnCamera;
    public CustomButton btnLens;
    public CustomButton btnOther;
    private ScrollViewHelper m_sih;

    void Start()
    {
        // m_sih = new ScrollViewHelper(this, shopScrollView.GetComponent<ScrollRect>(), leftCameraItem);
        shopTitle.GetComponent<TextMeshProUGUI>().text = DataLoader.Tr("商店");
        btnLens.SetString(DataLoader.Tr("镜头"));
        btnCamera.SetString(DataLoader.Tr("相机"));
        btnOther.SetString(DataLoader.Tr("其他"));
        ToggleGroup mainTab = gameObject.AddComponent<ToggleGroup>();
        btnCamera.group = mainTab;
        btnLens.group = mainTab;
        btnOther.group = mainTab;
        btnCamera.AddSelectCallback(OnTabCameraSelect);
        btnLens.AddSelectCallback(OnTabLensSelect);
        btnOther.AddSelectCallback(OnTabOtherSelect);

        // m_btnLens = new UICustomBtn(btnLens);
    }
    void ShowCameraItemList()
    {
        // m_sih.ClearItems();
        // foreach (var cameraData in DataLoader.Instance.cameraData)
        // {
        //     var item = m_sih.AddItem();
        //     var ShopCameraItem = item.GetComponent<ShopCameraItem>();
        //     ShopCameraItem.SetData(cameraData.Value);
        // }
        // m_sih.UpdateLayout();
    }

    void ShowLensItemList()
    {
        // m_sih.ClearItems();
        // foreach (var lensData in DataLoader.Instance.lensData)
        // {
        //     var item = m_sih.AddItem();
        //     var ShopCameraItem = item.GetComponent<ShopCameraItem>();
        //     ShopCameraItem.SetData(lensData.Value);
        // }
        // m_sih.UpdateLayout();
    }
    void ShowOtherItemList()
    {
        // m_sih.ClearItems();
        // foreach (var plugData in DataLoader.Instance.plugData)
        // {
        //     var item = m_sih.AddItem();
        //     var ShopCameraItem = item.GetComponent<ShopCameraItem>();
        //     ShopCameraItem.SetData(plugData.Value);
        // }
        // m_sih.UpdateLayout();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnTabCameraSelect(bool selected)
    {
        if (selected)
        {
            ShowCameraItemList();
        }
        
    }
    public void OnTabLensSelect(bool selected)
    {
        ShowLensItemList();
    }
    public void OnTabOtherSelect(bool selected)
    {
        ShowOtherItemList();
    }
}
