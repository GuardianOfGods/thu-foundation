﻿namespace Pancake.Editor.Guide
{
    using UnityEngine;

    public class Buttons_ButtonSample : ScriptableObject
    {
        [Button("Click me!")]
        private void DoButton() { Debug.Log("Button clicked!"); }
    }
}