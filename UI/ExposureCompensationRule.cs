using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ExposureCompensationRule : UICommon
{
    protected static new Dictionary<string, string> __shortcuts__ = new Dictionary<string, string>();
    protected override Dictionary<string, string> ShortCutsCache => __shortcuts__;
    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "UpContent",
        "UpIcon",
        "zeroimage",
        "DownContent",
        "DownIcon",
        "leftarrow",
        "rightarrow",
    };


    private RectTransform upIconContent => this["UpContent"];
    private RectTransform upIcon => this["UpIcon"];
    private RectTransform zeroImage => this["zeroimage"];
    private RectTransform downIconContent => this["DownContent"];
    private RectTransform downIcon => this["DownIcon"];
    private RectTransform leftArrow => this["leftarrow"];
    private RectTransform rightArrow => this["rightarrow"];

    private ScrollViewHelper m_sihUp;
    private ScrollViewHelper m_sihDown;

    private static readonly int minEvPara = -9;
    private static readonly int maxEvPara = 9;
    private uint zeroBlinkTimer = 0;

    // Start is called before the first frame update
    protected override void OnLoad()
    {
        m_sihUp = new ScrollViewHelper(this, upIconContent, upIcon, isVertical: false, isHorizontal: true);
        m_sihDown = new ScrollViewHelper(this, downIconContent, downIcon, isVertical: false, isHorizontal: true);
        LoadUp();
        LoadDown();
        leftArrow.gameObject.SetActive(false);
        rightArrow.gameObject.SetActive(false);
    }
    

    protected override void OnUnload()
    {
        if (zeroBlinkTimer != 0)
        {
            G.Timer.CancelTimer(zeroBlinkTimer);
            zeroBlinkTimer = 0;
        }
    }
    public void SetActive(bool active)
    {
        if (active)
        {
            if (zeroBlinkTimer == 0)
            {
                zeroBlinkTimer = this.AddTimer(0.8f, () =>
                {
                    zeroImage.gameObject.SetActive(!zeroImage.gameObject.activeSelf);
                }, TimerUpdateMode.Update, repeateCount: -1);
            }
            zeroImage.gameObject.SetActive(true);
            gameObject.SetActive(true);
        }
        else
        {
            if (zeroBlinkTimer != 0)
            {
                G.Timer.CancelTimer(zeroBlinkTimer);
                zeroBlinkTimer = 0;
            }
            gameObject.SetActive(false);
        }
    }

    void LoadUp()
    {
        //上方
        // upIcon.gameObject.SetActive(false);
        m_sihUp.ClearItems();

        var itemMiddle = m_sihUp.AddItem(); //较长的
        itemMiddle.offsetMax = new Vector2(0, - upIcon.rect.height / 3);
        // this.Print(itemMiddle.sizeDelta.ToString());

        var itemShort = m_sihUp.AddItem();// 较短的
        itemShort.offsetMax = new Vector2(0, - upIcon.rect.height / 3 * 2);
        m_sihUp.AddItem(item: itemShort);

        for (int i = 0; i < 2; i++)
        {
            m_sihUp.AddItem(item:itemMiddle);
            m_sihUp.AddItem(item: itemShort);
            m_sihUp.AddItem(item: itemShort);
        }
        m_sihUp.AddItem();
        for (int i = 0; i < 3; i++)
        {
            m_sihUp.AddItem(item: itemShort);
            m_sihUp.AddItem(item: itemShort);
            m_sihUp.AddItem(item: itemMiddle);
        }
        m_sihUp.UpdateLayout(modifyContentSize: false);

    }

    void LoadDown()
    {
        //下方
        downIcon.gameObject.SetActive(false);
        m_sihDown.ClearItems();
        for (int posEVPara = minEvPara; posEVPara <= maxEvPara; posEVPara++)
        {
            var item = m_sihDown.AddItem();
            item.gameObject.SetActive(false);
        }
        m_sihDown.Items[-minEvPara].gameObject.SetActive(true);
        m_sihDown.UpdateLayout(modifyContentSize: false);
    }

    public void ShowEVCompensation(int compensationEV)
    {
        for (int posEVPara = minEvPara; posEVPara <= maxEvPara; posEVPara++)
        {
            var item = m_sihDown.Items[posEVPara - minEvPara];
            item.gameObject.SetActive(compensationEV <= posEVPara && posEVPara <= 0 || compensationEV >= posEVPara && posEVPara >= 0);
        }
        rightArrow.gameObject.SetActive(compensationEV > maxEvPara);
        leftArrow.gameObject.SetActive(compensationEV < minEvPara);
    }
}
