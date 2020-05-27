using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] float _horRotSpeed = 5f;
    [SerializeField] float _verRotSpeed = 3f;
    [SerializeField] [Range(30, 90)] float _elevation = 50f;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _nearestDis = 10f;
    [SerializeField] float _farthestDis = 50f;

    float _lastScroll = 0f, _distance = 0f, _rotate = 0f, _viewAngle = 0f;
    Vector3 _screen2WorldOffset = Vector3.zero;

    TransparentWindow _window;
    ObjPoolManager _pool;
    bool _isInRoleRect = false;
    Vector2Int _lastMousePos = Vector2Int.zero;
    Transform _root, _rotCenter, _role;
    GameObject _camera;

    private void Start()
    {
        _pool = FindObjectOfType<ObjPoolManager>();
        _window = FindObjectOfType<TransparentWindow>();
        _camera = Camera.main.gameObject;
    }

    private void LateUpdate()
    {
        // 判断当前是否有桌宠在场，没有则停止计算和渲染
        if (!_pool.IsAnyRoleEnable())
        {
            if (_camera.activeSelf)
            {
                _camera.SetActive(false);
                _window.SetBottom(true);
            }
            return;
        }
        else
        {
            if (!_camera.activeSelf)
            {
                _camera.SetActive(true);
                _window.SetBottom(false);
            }
        }

        if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2))
            CursorPenetrate();

        if (_root == null)
            return;

        // 旋转中
        if (Input.GetMouseButton(1))
        {
            SetAA(false);
            Rotating();
        }
        // 缩放中
        if (_lastScroll != 0)
        {
            SetAA(false);
            Scaling();
        }

        // 拖动平移开始
        if (Input.GetMouseButtonDown(2))
        {
            SetAA(false);
            _screen2WorldOffset = GetScreen2WorldOffset();
        }
        // 拖动平移中
        if (Input.GetMouseButton(2))
            Translation();

        // 松开，保存
        if (Input.GetMouseButtonUp(1)
            || Input.GetMouseButtonUp(2)
            // 使用上一帧的滚轮计算摄像机的位移并判断此帧为松手
            || (_lastScroll != 0 && Input.mouseScrollDelta.y == 0))
        {
            SetAA();
            DataModel.Instance.UpdateTransformData(_pool);
            DataModel.Instance.SaveData();
        }
        _lastScroll = Input.mouseScrollDelta.y;


    }


    private void Scaling()
    {
        // 距离
        _distance = Vector3.Distance(Camera.main.transform.position, _role.position);
        if (_lastScroll > 0 ? _distance > _nearestDis : _distance < _farthestDis)
        {
            // 向朝向角色的方向移动
            var dir = Vector3.Normalize(_rotCenter.position - Camera.main.transform.position);
            _root.Translate(dir * _lastScroll * _moveSpeed * Time.deltaTime * Mathf.Pow(_distance, 1.15f) * -0.75f, Space.World);
        }
    }

    private void Rotating()
    {
        // 角色绕旋转中心转
        _role.RotateAround(_rotCenter.position, _role.up
        , Input.GetAxis("Mouse X") * Time.deltaTime * _horRotSpeed * -100);
        _rotate = Input.GetAxis("Mouse Y");
        // 垂直正方向与至摄像机向量角度
        _viewAngle = 180 - (Vector3.Dot(_role.up, Vector3.Normalize(Camera.main.transform.position - _role.position)) + 1) * 90;
        if (_rotate > 0 ?
            _viewAngle < (90 + _elevation) :
            _viewAngle > (90 - _elevation))
        {
            _role.RotateAround(_rotCenter.position, Vector3.right, _rotate * Time.deltaTime * _verRotSpeed * -100);
        }
    }

    // 第一帧计算鼠标点到角色位置之差
    Vector3 GetScreen2WorldOffset()
    {
        var mousePosV2 = _window.GetMousePosW2U();
        var rolePos = Camera.main.WorldToScreenPoint(_root.position);
        return rolePos - new Vector3(mousePosV2.x, mousePosV2.y, 0);
    }

    // 再根据鼠标位置加上offset算出角色位置
    void Translation()
    {
        var mousePosV2 = _window.GetMousePosW2U();
        var newPos = new Vector3(mousePosV2.x, mousePosV2.y, 0) + _screen2WorldOffset;
        // 限制上下位置
        // var rotateTargetPos = Camera.main.WorldToScreenPoint(_rotateTarget.position);
        // var rolePos = Camera.main.WorldToScreenPoint(_sakura.position);
        // var dis = (new Vector2(rotateTargetPos.x, rotateTargetPos.y) - new Vector2(rolePos.x, rolePos.y)).magnitude;
        // newPos.x = Mathf.Clamp(newPos.x, 0, System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width);
        // newPos.y = Mathf.Clamp(newPos.y, 0 - dis, System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height - dis);
        _root.position = Camera.main.ScreenToWorldPoint(newPos);

    }



    void CursorPenetrate()
    {
        // 鼠标有位移时打射线，碰到角色则不穿透，否则窗口穿透
        var pos = _window.GetMousePosW2U();
        if (GetMouseMove(_lastMousePos, pos) < 1) return;
        var posV3 = new Vector3(pos.x, pos.y, 0);
        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(posV3), out hitInfo, 1000f, LayerMask.GetMask("WindowRect")))
        {
            // 鼠标进入角色范围
            if (!_isInRoleRect)
            {
                _window.SetMousePenetrate(false);
                _isInRoleRect = true;
            }
            _role = hitInfo.transform;
            _root = hitInfo.transform.parent;
            _rotCenter = hitInfo.transform.GetComponent<RoleCtrlBase>().rotationCenter.transform;
        }
        else
        {
            // 鼠标移出
            if (_isInRoleRect)
            {
                _window.SetMousePenetrate(true);
                _isInRoleRect = false;
            }
            _role = null;
            _root = null;
            _rotCenter = null;
        }
        _lastMousePos = pos;
    }

    float GetMouseMove(Vector2 last, Vector2 current)
    {
        return Mathf.Abs(current.x - last.x) + Mathf.Abs(current.y - last.y);
    }

    void SetAA(bool isTAA = true)
    {
        // if (ppl.antialiasingMode != (isTAA ? PostProcessLayer.Antialiasing.TemporalAntialiasing : PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing))
        //     ppl.antialiasingMode = isTAA ? PostProcessLayer.Antialiasing.TemporalAntialiasing : PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        // ppl.temporalAntialiasing.motionBlending = isTAA ? 0.85f : 0.0f;
        // ppl.temporalAntialiasing.stationaryBlending = isTAA ? 0.95f : 0.0f;
    }

    public float GetMouseMove()
    {
        float f = 0;
        foreach (var item in _pool.rolePool)
        {
            if (item != null && !item.GetComponent<RoleCtrlBase>().isIdleState)
            {
                f = 10f;
                break;
            }
        }

        if (_root == null)
            return f;
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2) || Input.mouseScrollDelta.y > 0)
        {
            //有输入，计算运动
            var pos = _window.GetMousePosW2U();
            f += GetMouseMove(_lastMousePos, pos) + Input.mouseScrollDelta.y * 100f;
            return f;
        }
        else
        {
            return f;
        }

    }

}
