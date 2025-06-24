using System;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    [Serializable]
    public class SubButton
    {
        public string Name;
        public string Color;
        public Action OnClick;

        public SubButton(string name, string color, Action onClick)
        {
            Name = name;
            Color = color;
            OnClick = onClick;
        }
    }
}

