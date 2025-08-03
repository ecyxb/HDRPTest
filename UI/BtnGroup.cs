// using System.Collections;
// using System.Collections.Generic;
// using Unity.VisualScripting;
// using UnityEngine;

// public class BtnGroup
// {
//     private List<UICustomBtn> buttons;
//     private System.Action<UICustomBtn> clickAction;
//     public void OnButtonClick(UICustomBtn btn)
//     {
//         foreach (var button in buttons)
//         {
//             button.SetSelected(btn == button);
//         }
//         clickAction?.Invoke(button);
//     }
//     public BtnGroup()
//     {

//     }

//     public void AddButton(UICustomBtn customButton)
//     {
//         buttons.Add(customButton);
//         customButton.SetCallBack(OnButtonClick);
//     }
//     public void SetCallBack(System.Action<UICustomBtn> clickAction)
//     {
//         this.clickAction = clickAction;
//         foreach (var button in buttons)
//         {
//             button.SetCallBack(OnButtonClick);
//         }
//     }
//     public int IndexButton(UICustomBtn button)
//     {
//         return buttons.IndexOf(button);
//     }
// }