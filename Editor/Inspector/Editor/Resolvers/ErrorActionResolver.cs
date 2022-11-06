﻿using System;
using UnityEngine;

namespace Pancake.Editor
{
    internal sealed class ErrorActionResolver : ActionResolver
    {
        private readonly string _method;

        public ErrorActionResolver(PropertyDefinition propertyDefinition, string method) { _method = method; }

        public override bool TryGetErrorString(out string error)
        {
            error = $"Method '{_method}' not exists or has wrong signature";
            return true;
        }

        public override void InvokeForTarget(Property property, int targetIndex) { Debug.LogException(new InvalidOperationException("Method not exists")); }
    }
}