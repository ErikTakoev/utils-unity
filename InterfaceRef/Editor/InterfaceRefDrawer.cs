#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Expecto.Utils.Editor
{
	[CustomPropertyDrawer(typeof(InterfaceRef<>), true)]
	public class InterfaceRefDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Type interfaceType = GetInterfaceTypeFromField();

			SerializedProperty targetProp = property.propertyType == SerializedPropertyType.ObjectReference
				? property
				: property.FindPropertyRelative("_target");

			if (targetProp == null || targetProp.propertyType != SerializedPropertyType.ObjectReference)
			{
				EditorGUI.LabelField(position, label.text, "Invalid use of InterfaceRef");
				return;
			}

			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();

			UnityEngine.Object obj = EditorGUI.ObjectField(position, label, targetProp.objectReferenceValue, interfaceType, true);

			if (EditorGUI.EndChangeCheck())
			{
				targetProp.objectReferenceValue = obj;
			}

			EditorGUI.EndProperty();
		}

		private Type GetInterfaceTypeFromField()
		{
			Type type = fieldInfo.FieldType;

			while (type != null && type != typeof(object))
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(InterfaceRef<>))
					return type.GetGenericArguments()[0];

				type = type.BaseType;
			}

			return typeof(UnityEngine.Object); // Fallback to UnityEngine.Object if we can't find it to prevent errors
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
#endif