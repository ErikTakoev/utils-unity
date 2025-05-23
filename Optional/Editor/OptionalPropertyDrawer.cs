using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utils.Editor
{

    [CustomPropertyDrawer(typeof(Optional<>), true)]
    public class OptionalPropertyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty enabledProp = property.FindPropertyRelative("Enabled");
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            if (enabledProp == null || valueProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Missing 'Enabled' or 'Value'");
                return;
            }

            Rect toggleRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            // Handle mixed values state properly
            EditorGUI.BeginProperty(toggleRect, label, property);
            EditorGUI.showMixedValue = enabledProp.hasMultipleDifferentValues;

            // Using EditorGUI.PropertyField for better multi-object support
            EditorGUI.PropertyField(toggleRect, enabledProp, label);
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();

            // Only show fields if enabled
            if (enabledProp.boolValue)
            {
                EditorGUI.indentLevel++;
                foreach (var child in GetChildren(valueProp))
                {
                    EditorGUILayout.PropertyField(child, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        List<SerializedProperty> GetChildren(SerializedProperty property)
        {
            List<SerializedProperty> children = new List<SerializedProperty>();

            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();


            while (!SerializedProperty.EqualContents(iterator, endProperty))
            {
                if (iterator.depth == property.depth + 1)
                {
                    children.Add(iterator.Copy());
                }

                if (!iterator.NextVisible(true))
                    break;
            }

            return children;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty enabledProp = property.FindPropertyRelative("Enabled");
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            if (enabledProp != null && enabledProp.boolValue && valueProp != null)
            {
                return base.GetPropertyHeight(property, label);
            }

            return EditorGUIUtility.singleLineHeight;
        }
    }
}