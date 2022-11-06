﻿namespace Pancake.Editor.Guide
{
    using System;
    using UnityEngine;

    public class Validators_InfoBoxSample : ScriptableObject
    {
        [Title("InfoBox Message Types")] [InfoBox("Default info box")]
        public int a;

        [InfoBox("None info box", EMessageType.None)] public int b;

        [InfoBox("Warning info box", EMessageType.Warning)] public int c;

        [InfoBox("Error info box", EMessageType.Error)] public int d;

        [InfoBox("$" + nameof(DynamicInfo), visibleIf: nameof(VisibleInEditMode))]
        public Vector3 vec;

        private string DynamicInfo => "Dynamic info box: " + DateTime.Now.ToLongTimeString();

        private bool VisibleInEditMode => !Application.isPlaying;
    }
}