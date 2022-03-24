using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DraggablePoint : PropertyAttribute { }

#if UNITY_EDITOR
[CustomEditor(typeof(MonoBehaviour), true)]
public class DraggablePointDrawer : Editor
{
    readonly GUIStyle style = new GUIStyle();

    void OnEnable()
    {
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
    }

    public void OnSceneGUI()
    {
        SerializedProperty property = serializedObject.GetIterator();
        if (property.Next(true)) {
            HandleVector3Recursive(property, serializedObject.targetObject);
        }
    }

    public void HandleVector3Recursive(SerializedProperty property, object obj) {
        while (true) {
            if (property.propertyType == SerializedPropertyType.Vector3) {
                HandleVector3(property, obj);
            } else if (property.isArray && property.isExpanded) {
                var field = obj.GetType().GetField(property.name);
                if (field != null) {
                    var objArray = field.GetValue(obj) as IEnumerable<object>;
                    if (objArray != null) {
                        int i = 0;
                        foreach (object newObj in objArray) {
                            HandleVector3Recursive(property.GetArrayElementAtIndex(i), newObj);
                            i += 1;
                        }
                    }
                }
            }
            if (!property.Next(property.isExpanded)) {
                return;
            }
        }
    }

    public void HandleVector3(SerializedProperty property, object obj)
    {
        var field = obj.GetType().GetField(property.name);
        if (field == null) {
            return;
        }
        var draggablePoints = field.GetCustomAttributes(typeof(DraggablePoint), false);
        if (draggablePoints.Length > 0) {
            Handles.Label(property.vector3Value, property.name);
            property.vector3Value = Handles.PositionHandle(property.vector3Value, Quaternion.identity);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif