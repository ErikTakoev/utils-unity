using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Expecto.Editor
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

			EditorGUI.BeginProperty(position, label, property);

			Rect toggleRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			// Handle mixed values state properly
			EditorGUI.showMixedValue = enabledProp.hasMultipleDifferentValues;
			EditorGUI.PropertyField(toggleRect, enabledProp, label);
			EditorGUI.showMixedValue = false;

			// Only show fields if enabled
			if (enabledProp.boolValue)
			{
				EditorGUI.indentLevel++;
				float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				SerializedProperty iterator = valueProp.Copy();
				SerializedProperty endProperty = iterator.GetEndProperty();

				bool enterChildren = true;
				while (iterator.NextVisible(enterChildren))
				{
					if (SerializedProperty.EqualContents(iterator, endProperty))
						break;

					float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
					Rect valueRect = new Rect(position.x, currentY, position.width, propHeight);
					EditorGUI.PropertyField(valueRect, iterator, true);

					currentY += propHeight + EditorGUIUtility.standardVerticalSpacing;
					enterChildren = false;
				}
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SerializedProperty enabledProp = property.FindPropertyRelative("Enabled");
			SerializedProperty valueProp = property.FindPropertyRelative("Value");

			float height = EditorGUIUtility.singleLineHeight;

			if (enabledProp != null && enabledProp.boolValue && valueProp != null)
			{
				SerializedProperty iterator = valueProp.Copy();
				SerializedProperty endProperty = iterator.GetEndProperty();

				bool enterChildren = true;
				while (iterator.NextVisible(enterChildren))
				{
					if (SerializedProperty.EqualContents(iterator, endProperty))
						break;

					height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
					enterChildren = false;
				}
			}

			return height;
		}
	}
}
