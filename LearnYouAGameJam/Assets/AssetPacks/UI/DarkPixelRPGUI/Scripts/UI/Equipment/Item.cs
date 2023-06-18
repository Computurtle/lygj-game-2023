using System;
using UnityEngine;
#nullable disable

namespace DarkPixelRPGUI.Scripts.UI.Equipment
{
    [Serializable]
    public class Item
    {
        [SerializeField] private Sprite sprite;
        public Sprite Sprite => sprite;
    }
}
