using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DataCopyer : EditorWindow
{
    GameObject src, dst;

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
        if (GUILayout.Button("Copy"))
        {
            DoCopy();
        }
    }

    void DoCopy()
    {
        List<Transform> srcList = new List<Transform>(), dstList = new List<Transform>();
        Dictionary<string, Transform> dstDic = new Dictionary<string, Transform>();
        GetChildrenDeep(srcList, null, src.transform);
        GetChildrenDeep(dstList, dstDic, dst.transform);

        Debug.Log(srcList.Count + "   " + dstList.Count);
        for (int i = 0; i < srcList.Count && i < dstList.Count; i++)
        {
            if (srcList[i].name != dstList[i].name)
                Debug.LogWarning($"{srcList[i].name} != {dstList[i].name}");
            GameObject srcGo = srcList[i].gameObject, dstGo = dstList[i].gameObject;
            dstGo.tag = srcGo.tag;
            dstGo.layer = srcGo.layer;
            CopyCompoment(srcGo, dstGo, dstDic);
        }
    }

    void GetChildrenDeep(List<Transform> list, Dictionary<string, Transform> dic, Transform father)
    {
        list.Add(father);
        if (dic != null)
            dic.Add(father.name, father);
        for (int i = 0; i < father.childCount; i++)
        {
            GetChildrenDeep(list, dic, father.GetChild(i));
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
                    db.m_Root = dstDic[(component as DynamicBone).m_Root.name];
                }
            }
        }
    }
}