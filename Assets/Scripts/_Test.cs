using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;

public class _Test : MonoBehaviour
{
    private void Start()
    {
        // PlayerPrefs.DeleteAll();
        // Debug.Log((DataModel.Instance.Data));
        // Debug.Log(JsonUtility.ToJson(DataModel.Instance.Data));
        var v = Resources.LoadAll<AnimationClip>("AnimationClip/Herugaa");
        Debug.Log(v.Length);
    }
}
