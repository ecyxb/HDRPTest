
using UnityEngine;
using UnityEngine.UI;
namespace EventFramework
{
    public static class UIHelpers
    {
        public static void SetMargin(RectTransform rect, RectOffset margin)
        {
            if (rect == null || margin == null)
                return;
            rect.offsetMin = new Vector2(margin.left, margin.bottom);
            rect.offsetMax = new Vector2(-margin.right, -margin.top);
        }

        public static string GetGORelativePath(Transform target, Transform root, System.Text.StringBuilder sb)
        {
            sb.Clear();
            Transform current = target;
            while (current != null && current != root)
            {
                if (sb.Length == 0)
                    sb.Insert(0, current.name);
                else
                    sb.Insert(0, current.name + "/");
                current = current.parent;
            }
            return sb.ToString();
        }

    }
}