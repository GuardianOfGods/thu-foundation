﻿using System;
using System.Linq;
using UnityEngine;

namespace Pancake.Localization
{
    public class LocaleSprite : LocaleVariable<Sprite>
    {
        [Serializable]
        private class SpriteLocaleItem : LocaleItem<Sprite>
        {
        };

        [SerializeField] private SpriteLocaleItem[] items = new SpriteLocaleItem[1];
        public override LocaleItemBase[] LocaleItems => items.ToArray<LocaleItemBase>();
    }
}