using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DataCopyer : EditorWindow
{
    GameObject src, dst;
    bool normalizeName = true;

    [MenuItem("DesktopMascot/DataCopyer")]
    private static void ShowWindow()
    {
        var window = GetWindow<DataCopyer>();
        window.titleContent = new GUIContent("DataCopyer");
        window.Show();
    }

    private void OnGUI()
    {
        src = EditorGUILayout.ObjectField("src", src, typeof(GameObject), true) as GameObject;
        dst = EditorGUILayout.ObjectField("dst", dst, typeof(GameObject), true) as GameObject;
        normalizeName = GUILayout.Toggle(normalizeName, "Normalize Name");
        if (GUILayout.Button("Copy"))
        {
            DoCopy();
        }
    }

    void DoCopy()
    {
        Dictionary<string, Transform> srcDic = new Dictionary<string, Transform>(), dstDic = new Dictionary<string, Transform>();
        var dstName = dst.name;
        dst.name = src.name;
        GetChildrenDeep(srcDic, src.transform);
        GetChildrenDeep(dstDic, dst.transform);

        Debug.Log(srcDic.Count + "   " + dstDic.Count);

        foreach (var dstPair in dstDic)
        {
            if (!srcDic.ContainsKey(dstPair.Key))
            {
                Debug.LogWarning($"{dstPair.Key} do not exist!");
                continue;
            }
            GameObject srcGo = srcDic[dstPair.Key].gameObject, dstGo = dstPair.Value.gameObject;
            dstGo.tag = srcGo.tag;
            dstGo.layer = srcGo.layer;
            CopyCompoment(srcGo, dstGo, dstDic);
        }
        dst.name = dstName;
    }

    void GetChildrenDeep(Dictionary<string, Transform> dic, Transform father)
    {
        dic.Add(normalizeName ? AnimationNameNormalization.NormalizeStr(father.name) : father.name, father);
        for (int i = 0; i < father.childCount; i++)
        {
            GetChildrenDeep(dic, father.GetChild(i));
        }
    }

    void CopyCompoment(GameObject src, GameObject dst, Dictionary<string, Transform> dstDic)
    {
        foreach (var component in src.GetComponents<UnityEngine.Component>())
        {
            Component oldComponent = dst.GetComponent(component.GetType());
            if (oldComponent)
            { }
            else
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                if (UnityEditorInternal.ComponentUtility.PasteComponentAsNew(dst))
                    Debug.Log($"Paste New Values {component.GetType()} to {dst.name} Success");
                else
                    Debug.LogWarning($"Paste New Values {component.GetType()} to {dst.name} Failed");
                if (component.GetType() == typeof(DynamicBone))
                {
                    var db = dst.GetComponent<DynamicBone>();
                    string name = (component as DynamicBone).m_Root.name;
                    db.m_Root = dstDic[normalizeName ? AnimationNameNormalization.NormalizeStr(name) : name];
                }
            }
        }
    }
}