﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Pancake.Editor
{
    public class PropertyDefinition
    {
        private readonly ValueGetterDelegate _valueGetter;
        [CanBeNull] private readonly ValueSetterDelegate _valueSetter;

        private readonly List<string> _extensionErrors = new List<string>();
        private readonly MemberInfo _memberInfo;
        private readonly List<Attribute> _attributes;

        private PropertyDefinition _arrayElementDefinitionBackingField;

        private IReadOnlyList<CustomDrawer> _drawersBackingField;
        private IReadOnlyList<Validator> _validatorsBackingField;
        private IReadOnlyList<PropertyHideProcessor> _hideProcessorsBackingField;
        private IReadOnlyList<PropertyDisableProcessor> _disableProcessorsBackingField;

        public static PropertyDefinition CreateForFieldInfo(int order, FieldInfo fi)
        {
            return CreateForMemberInfo(fi,
                order,
                fi.Name,
                fi.FieldType,
                MakeGetter(fi),
                MakeSetter(fi));
        }

        public static PropertyDefinition CreateForPropertyInfo(int order, PropertyInfo pi)
        {
            return CreateForMemberInfo(pi,
                order,
                pi.Name,
                pi.PropertyType,
                MakeGetter(pi),
                MakeSetter(pi));
        }

        public static PropertyDefinition CreateForMethodInfo(int order, MethodInfo mi)
        {
            return CreateForMemberInfo(mi,
                order,
                mi.Name,
                typeof(MethodInfo),
                MakeGetter(mi),
                MakeSetter(mi));
        }

        private static PropertyDefinition CreateForMemberInfo(
            MemberInfo memberInfo,
            int order,
            string propertyName,
            Type propertyType,
            ValueGetterDelegate valueGetter,
            ValueSetterDelegate valueSetter)
        {
            var attributes = memberInfo?.GetCustomAttributes().ToList();
            var ownerType = memberInfo?.DeclaringType ?? typeof(object);

            return new PropertyDefinition(memberInfo,
                ownerType,
                order,
                propertyName,
                propertyType,
                valueGetter,
                valueSetter,
                attributes,
                false);
        }

        internal PropertyDefinition(
            MemberInfo memberInfo,
            Type ownerType,
            int order,
            string fieldName,
            Type fieldType,
            ValueGetterDelegate valueGetter,
            ValueSetterDelegate valueSetter,
            List<Attribute> attributes,
            bool isArrayElement)
        {
            OwnerType = ownerType;
            Name = fieldName;
            FieldType = fieldType;
            IsArrayElement = isArrayElement;

            _attributes = attributes ?? new List<Attribute>();
            _memberInfo = memberInfo;
            _valueGetter = valueGetter;
            _valueSetter = valueSetter;

            Order = order;
            IsReadOnly = _valueSetter == null || Attributes.TryGet(out ReadOnlyAttribute _);

            if (ReflectionUtilities.IsArrayOrList(FieldType, out var elementType))
            {
                IsArray = true;
                ArrayElementType = elementType;
            }

            if (Attributes.TryGet(out LabelTextAttribute labelTextAttribute))
            {
                CustomLabel = ValueResolver.ResolveString(this, labelTextAttribute.Text);
            }

            if (Attributes.TryGet(out PropertyTooltipAttribute tooltipAttribute))
            {
                CustomTooltip = ValueResolver.ResolveString(this, tooltipAttribute.Tooltip);
            }
        }

        public Type OwnerType { get; }

        public Type FieldType { get; }

        public string Name { get; }

        public int Order { get; internal set; }

        public IReadOnlyList<Attribute> Attributes => _attributes;

        public bool IsReadOnly { get; }

        public bool IsArrayElement { get; }
        public Type ArrayElementType { get; }

        public bool IsArray { get; }

        [CanBeNull] public ValueResolver<string> CustomLabel { get; }
        [CanBeNull] public ValueResolver<string> CustomTooltip { get; }

        public IReadOnlyList<PropertyHideProcessor> HideProcessors => PopulateHideProcessor();
        public IReadOnlyList<PropertyDisableProcessor> DisableProcessors => PopulateDisableProcessors();
        public IReadOnlyList<CustomDrawer> Drawers => PopulateDrawers();
        public IReadOnlyList<Validator> Validators => PopulateValidators();

        internal IReadOnlyList<string> ExtensionErrors
        {
            get
            {
                PopulateHideProcessor();
                PopulateDisableProcessors();
                PopulateDrawers();
                PopulateValidators();
                return _extensionErrors;
            }
        }

        public List<Attribute> GetEditableAttributes() { return _attributes; }

        public bool TryGetMemberInfo(out MemberInfo memberInfo)
        {
            memberInfo = _memberInfo;
            return memberInfo != null;
        }

        public object GetValue(Property property, int targetIndex) { return _valueGetter(property, targetIndex); }

        public bool SetValue(Property property, object value, int targetIndex, out object parentValue)
        {
            if (IsReadOnly)
            {
                Debug.LogError("Cannot set value for readonly property");
                parentValue = default;
                return false;
            }

            parentValue = _valueSetter?.Invoke(property, targetIndex, value);
            return true;
        }

        public PropertyDefinition ArrayElementDefinition
        {
            get
            {
                if (_arrayElementDefinitionBackingField == null)
                {
                    if (!IsArray)
                    {
                        throw new InvalidOperationException($"Cannot get array element definition for non array property: {FieldType}");
                    }

                    var elementGetter = new ValueGetterDelegate((self, targetIndex) =>
                    {
                        var parentValue = (IList) self.Parent.GetValue(targetIndex);
                        return parentValue[self.IndexInArray];
                    });
                    var elementSetter = new ValueSetterDelegate((self, targetIndex, value) =>
                    {
                        var parentValue = (IList) self.Parent.GetValue(targetIndex);
                        parentValue[self.IndexInArray] = value;
                        return parentValue;
                    });

                    _arrayElementDefinitionBackingField = new PropertyDefinition(_memberInfo,
                        OwnerType,
                        0,
                        "Element",
                        ArrayElementType,
                        elementGetter,
                        elementSetter,
                        _attributes,
                        true);
                }

                return _arrayElementDefinitionBackingField;
            }
        }

        private IReadOnlyList<PropertyHideProcessor> PopulateHideProcessor()
        {
            if (_hideProcessorsBackingField != null)
            {
                return _hideProcessorsBackingField;
            }

            return _hideProcessorsBackingField = DrawersUtilities.CreateHideProcessorsFor(FieldType, Attributes).Where(CanApplyExtensionOnSelf).ToList();
        }

        private IReadOnlyList<PropertyDisableProcessor> PopulateDisableProcessors()
        {
            if (_disableProcessorsBackingField != null)
            {
                return _disableProcessorsBackingField;
            }

            return _disableProcessorsBackingField = DrawersUtilities.CreateDisableProcessorsFor(FieldType, Attributes).Where(CanApplyExtensionOnSelf).ToList();
        }

        private IReadOnlyList<Validator> PopulateValidators()
        {
            if (_validatorsBackingField != null)
            {
                return _validatorsBackingField;
            }

            return _validatorsBackingField = Enumerable.Empty<Validator>()
                .Concat(DrawersUtilities.CreateValueValidatorsFor(FieldType))
                .Concat(DrawersUtilities.CreateAttributeValidatorsFor(FieldType, Attributes))
                .Where(CanApplyExtensionOnSelf)
                .ToList();
        }

        private IReadOnlyList<CustomDrawer> PopulateDrawers()
        {
            if (_drawersBackingField != null)
            {
                return _drawersBackingField;
            }

            return _drawersBackingField = Enumerable.Empty<CustomDrawer>()
                .Concat(DrawersUtilities.CreateValueDrawersFor(FieldType))
                .Concat(DrawersUtilities.CreateAttributeDrawersFor(FieldType, Attributes))
                .Concat(new[] {new ValidatorsDrawer {Order = DrawerOrder.Validator,},})
                .Where(CanApplyExtensionOnSelf)
                .OrderBy(it => it.Order)
                .ToList();
        }

        private static ValueGetterDelegate MakeGetter(FieldInfo fi)
        {
            return (self, targetIndex) =>
            {
                var parentValue = self.Parent.GetValue(targetIndex);
                return fi.GetValue(parentValue);
            };
        }

        private static ValueSetterDelegate MakeSetter(FieldInfo fi)
        {
            return (self, targetIndex, value) =>
            {
                var parentValue = self.Parent.GetValue(targetIndex);
                fi.SetValue(parentValue, value);
                return parentValue;
            };
        }

        private static ValueGetterDelegate MakeGetter(PropertyInfo pi)
        {
            var method = pi.GetMethod;
            return (self, targetIndex) =>
            {
                var parentValue = self.Parent.GetValue(targetIndex);
                return method.Invoke(parentValue, null);
            };
        }

        private static ValueSetterDelegate MakeSetter(PropertyInfo pi)
        {
            var method = pi.SetMethod;
            if (method == null)
            {
                return null;
            }

            return (self, targetIndex, value) =>
            {
                var parentValue = self.Parent.GetValue(targetIndex);
                method.Invoke(parentValue, new[] {value,});
                return parentValue;
            };
        }

        private static ValueGetterDelegate MakeGetter(MethodInfo mi) { return (self, targetIndex) => mi; }

        private static ValueSetterDelegate MakeSetter(MethodInfo mi)
        {
            return (self, targetIndex, value) =>
            {
                var parentValue = self.Parent.GetValue(targetIndex);
                return parentValue;
            };
        }

        private bool CanApplyExtensionOnSelf(PropertyExtension propertyExtension)
        {
            if (propertyExtension.ApplyOnArrayElement.HasValue)
            {
                if (IsArrayElement && !propertyExtension.ApplyOnArrayElement.Value || IsArray && propertyExtension.ApplyOnArrayElement.Value)
                {
                    return false;
                }
            }

            var result = propertyExtension.Initialize(this);
            if (result.IsError)
            {
                _extensionErrors.Add(result.ErrorMessage);
            }

            return result.ShouldApply;
        }

        public delegate object ValueGetterDelegate(Property self, int targetIndex);

        public delegate object ValueSetterDelegate(Property self, int targetIndex, object value);
    }
}