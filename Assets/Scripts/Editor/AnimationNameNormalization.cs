using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AnimationNameNormalization : Editor
{
    [MenuItem("DesktopMascot/AnimationNameNormalization")]
    public static void DoNormalize()
    {
        List<AnimationClip> clipsList = new List<AnimationClip>();
        foreach (var item in Selection.assetGUIDs)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(item));
            if (clip)
                clipsList.Add(clip);
        }

        foreach (var clip in clipsList)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            AnimationCurve[] curves = new AnimationCurve[bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
            {
                curves[i] = AnimationUtility.GetEditorCurve(clip, bindings[i]);
                AnimationUtility.SetEditorCurve(clip, bindings[i], null);
                bindings[i].path = NormalizeStr(bindings[i].path);
                AnimationUtility.SetEditorCurve(clip, bindings[i], curves[i]);
            }

            Debug.Log(clip.name);
        }
    }

    public static string NormalizeStr(string str)
    {
        if (str == "")
            return str;
        var chars = str.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            int ascii = (int)chars[i];
            if (ascii < 47 || (ascii > 57 && ascii < 65) || (ascii > 90 && ascii < 97) || ascii > 122 && ascii != 92 && ascii != 95)
            {
                chars[i] = '_';
            }
        }
        return new string(chars);
    }
}