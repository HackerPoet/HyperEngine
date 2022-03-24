using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
public class HyperbolicEditor : ShaderGUI
{
    public readonly string[] allKeywords = new string[] {
        "BOUNDARY_BLEND", "USE_CLIP_RECT"
    };
    public enum HardCodedMode {
        NONE,
        CAFE_LIGHT,
        WATER,
        PORTAL,
        WAVY,
        PLASMA,
        GLOW,
        IGNORE_TEX_COLOR,
        CLOUD,
        GLITCH,
    };

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // If we are not visible, return.
        base.OnGUI(materialEditor, properties);
        if (!materialEditor.isVisible)
            return;

        // Get the current compile flags
        Material targetMat = materialEditor.target as Material;
        string[] keyWords = targetMat.shaderKeywords;
        List<string> newKeyWords = new List<string>();

        // If toggle has changed, add keywords to multi-compile list
        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < allKeywords.Length; i++) {
            bool hasWord = (Array.IndexOf(keyWords, allKeywords[i]) >= 0);
            hasWord = EditorGUILayout.Toggle(allKeywords[i], hasWord);
            if (hasWord) {
                newKeyWords.Add(allKeywords[i]);
            }
        }

        // If hard-coded mode has changed, add keywords to multi-compile list
        HardCodedMode curMode = HardCodedMode.NONE;
        string[] hardCodedNames = Enum.GetNames(typeof(HardCodedMode));
        for (int i = 0; i < hardCodedNames.Length; i++) {
            if (Array.IndexOf(keyWords, hardCodedNames[i]) >= 0) {
                Enum.TryParse(hardCodedNames[i], out curMode);
            }
        }
        curMode = (HardCodedMode)EditorGUILayout.EnumPopup("Hard-Coded Options:", curMode);
        if (curMode != HardCodedMode.NONE) {
            newKeyWords.Add(curMode.ToString());
        }

        // Update the material with the new values
        if (EditorGUI.EndChangeCheck()) {
            targetMat.shaderKeywords = newKeyWords.ToArray();
            EditorUtility.SetDirty(targetMat);
        }
    }
}
#endif
