using System.Collections.Generic;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// 튜토리얼 등이 가리킬 수 있는 UI 요소 등록부(이름 → RectTransform).
    /// 이름: btn_shop, btn_upgrade, shop_box_0~4, shop_openall, shop_item_0…, shop_place, upgrade_buy_Hammer …
    /// </summary>
    public static class UiTargets
    {
        static readonly Dictionary<string, RectTransform> Map = new Dictionary<string, RectTransform>();

        public static void Register(string name, Component c)
        {
            if (c != null) Map[name] = c.transform as RectTransform;
        }

        /// <summary>등록돼 있고 화면에 보이는(활성) 요소만 돌려준다.</summary>
        public static RectTransform Get(string name) =>
            Map.TryGetValue(name, out var rt) && rt != null && rt.gameObject.activeInHierarchy ? rt : null;

        public static void Clear() => Map.Clear();
    }
}
