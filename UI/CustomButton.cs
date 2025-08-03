using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.Events;
using Unity.VisualScripting;

public class CustomButton : Toggle
{
    [SerializeField]
    private bool m_IsToggle = false;

    public bool isToggle
    {
        get { return m_IsToggle; }
        set
        {
            if (m_IsToggle == value)
                return;
            m_IsToggle = value;
            ResetBtnData();
        }
    }
    [FormerlySerializedAs("onClick")]
    [SerializeField]
    private UnityEvent<CustomButton> m_OnClick = new UnityEvent<CustomButton>();
    public UnityEvent<CustomButton> onClick
    {
        get { return m_OnClick; }
        set { m_OnClick = value; }
    }


    protected override void Start()
    {
        if (!m_IsToggle)
        {
            ResetBtnData();
        }
        else
        {
            isOn = false;
        }
    }

    public void SetString(string text)
    {
        foreach (var textComponent in GetComponentsInChildren<TextMeshProUGUI>())
        {
            textComponent.text = text;
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        UISystemProfilerApi.AddMarker("CustomButton.onClick", this);
        m_OnClick.Invoke(this);
        if (isToggle)
        {
            base.OnPointerClick(eventData);
            return;
        }
    }

    private void ResetBtnData()
    {
        // if (isToggle)
        // {
        //     base.toggleTransition = ToggleTransition.Fade;
        // }
        // else
        // {
        //     base.onValueChanged.RemoveAllListeners();
        //     base.isOn = false;
        //     base.group = null;
        //     base.toggleTransition = ToggleTransition.None;
        // }
    }

    public virtual void SetSelected(bool selected)
    {
        if (!isToggle)
        {
            selected = false;
        }
        base.isOn = selected;
    }

    public void AddClickCallback(UnityAction<CustomButton> a)
    {
        onClick.AddListener(a);
    }
    public void AddSelectCallback(UnityAction<bool> a)
    {
        onValueChanged.AddListener(a);
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        //暂时不要这个
        return;
        // if (isToggle)
        // {
        //     base.OnSubmit(eventData);
        // }
        // Press();

        // // if we get set disabled during the press
        // // don't run the coroutine.
        // if (!IsActive() || !IsInteractable())
        //     return;

        // DoStateTransition(SelectionState.Pressed, false);
        // StartCoroutine(OnFinishSubmit());

    }
//     private IEnumerator OnFinishSubmit()
//     {
//         var fadeTime = colors.fadeDuration;
//         var elapsedTime = 0f;

//         while (elapsedTime < fadeTime)
//         {
//             elapsedTime += Time.unscaledDeltaTime;
//             yield return null;
//         }

//         DoStateTransition(currentSelectionState, false);
//     }
}
