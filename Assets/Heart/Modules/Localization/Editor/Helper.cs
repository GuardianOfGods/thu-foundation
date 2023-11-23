﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pancake.Localization;
using UnityEditor;
using UnityEngine;

namespace Pancake.LocalizationEditor
{
    public static class Helper
    {
        private static GUIContent[] contents;

        public static Language LanguageField(Rect position, Language language, bool showOnlyBuiltin = false)
        {
            var languages = new List<Language>();
            languages.AddRange(Language.BuiltInLanguages);

            if (!showOnlyBuiltin)
            {
                languages.AddRange(GetCustomLanguages());
            }

            if (contents == null || contents.Length != languages.Count)
            {
                contents = new GUIContent[languages.Count];
            }

            for (var i = 0; i < languages.Count; i++)
            {
                contents[i] = new GUIContent(languages[i].Name);
            }

            var languageName = language.Name;
            var languageCode = language.Code;

            var currentValue = languages.FindIndex(x => x.Code == languageCode);
            if (currentValue < 0)
            {
                currentValue = languages.FindIndex(x => x == Language.Unknown);
                Debug.Assert(currentValue >= 0);
            }

            var newValue = EditorGUI.Popup(position, currentValue, contents);
            if (newValue != currentValue)
            {
                return languages[newValue];
            }

            return language;
        }

        public static void LanguageField(Rect position, SerializedProperty property, GUIContent label, bool showOnlyBuiltin = false)
        {
            var languages = new List<Language>();
            languages.AddRange(Language.BuiltInLanguages);

            if (!showOnlyBuiltin)
            {
                languages.AddRange(GetCustomLanguages());
            }

            if (contents == null || contents.Length != languages.Count)
            {
                contents = new GUIContent[languages.Count];
            }

            for (var i = 0; i < languages.Count; i++)
            {
                contents[i] = new GUIContent(languages[i].Name);
            }

            EditorGUI.BeginProperty(position, label, property);

            var languageName = property.FindPropertyRelative("name");
            var languageCode = property.FindPropertyRelative("code");

            var currentValue = languages.FindIndex(x => x.Code == languageCode.stringValue);
            if (currentValue < 0)
            {
                currentValue = languages.FindIndex(x => x == Language.Unknown);
                Debug.Assert(currentValue >= 0);
            }

            var newValue = EditorGUI.Popup(position, label, currentValue, contents);
            if (newValue != currentValue)
            {
                languageName.stringValue = languages[newValue].Name;
                languageCode.stringValue = languages[newValue].Code;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        private static Language[] GetCustomLanguages()
        {
            var localizationSettings = LocaleSettings.Instance;
            if (localizationSettings != null)
            {
                var customLanguages = localizationSettings.AvailableLanguages.Where(x => x.Custom);
                return customLanguages.ToArray();
            }

            return Array.Empty<Language>();
        }
    }
}