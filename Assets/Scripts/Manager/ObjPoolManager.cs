using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjPoolManager : MonoBehaviour
{
    Config _config;
    GameObject[] _rootPool, _rolePool;
    public GameObject[] rootPool { get { return _rootPool; } }
    public GameObject[] rolePool { get { return _rolePool; } }

    void Awake()
    {
        _config = GetComponent<Config>();
        Debug.Assert(_config.roles.Length == (int)Roles.Count, "Roles枚举与Config数量不匹配");

        _rootPool = new GameObject[_config.roles.Length];
        _rolePool = new GameObject[_config.roles.Length];
    }

    public void AddRole(GameObject role, int index)
    {
        if (index < _rootPool.Length)
        {
            _rootPool[index] = role;
            _rolePool[index] = role.GetComponentInChildren<RoleCtrlBase>().gameObject;
            if (_rolePool[index] == null)
                Debug.LogError($"{role.name} missing {typeof(RoleCtrlBase)}");
        }
        else
            Debug.LogError("AddRole() 数组越界");
    }

    public void RemoveRole(int index)
    {
        if (index < _rolePool.Length && _rootPool[index] != null)
        {
            Destroy(_rootPool[index]);
            Destroy(_rolePool[index]);
            _rootPool[index] = null;
            _rolePool[index] = null;
        }
    }

    public bool IsAnyRoleEnable()
    {
        foreach (var item in _rootPool)
        {
            if (item != null)
            {
                return true;
            }
        }
        return false;
    }

    public Bounds GetMinAABB()
    {
        var bounds = new Bounds();
        foreach (var root in _rootPool)
        {
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return bounds;
    }

}
