using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public abstract class FaceCtrlBase : MonoBehaviour
{
    protected Dictionary<string, float> _valueEye_L = new Dictionary<string, float>();
    protected Dictionary<string, float> _valueEye_R = new Dictionary<string, float>();
    protected Dictionary<string, float> _valueMouth = new Dictionary<string, float>();
    protected float _timer;
    float _eyeAutoBlinkTimer = 0;
    float[] _spectrumData = new float[128];
    protected FaceManager _faceManager;


    protected abstract void Start();
    protected abstract void Update();
    public virtual void OnValidate()
    {
        Start();
    }
    void Awake()
    {
        _faceManager = FindObjectOfType<FaceManager>();
        Debug.Assert(_faceManager != null, "FaceManager Singleton is Missing! Create the singleton first ");
    }

    /// <summary>
    /// 采样音频返回0-1的MouthOpen Value
    /// </summary>
    /// <param name="audioSource"></param>
    /// <param name="scale">控制缩放</param>
    /// <param name="band">采样范围</param>
    /// <returns>MouthOpen Value</returns>
    protected float AutoMouthCtrl(AudioSource audioSource, float scale, float band = 8)
    {
        if (audioSource == null || !audioSource.isPlaying)
            return 0;
        //获取音频数据，精度128
        audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);
        float max = 0;
        for (int i = 0; i < band; i++)
        {
            if (max < _spectrumData[i])
            {
                max = _spectrumData[i];
            }
        }
        return max * scale;
    }

    /// <summary>
    /// 自动眨眼
    /// </summary>
    /// <param name="minInterval">最小间隔</param>
    /// <param name="maxInterval">最大间隔</param>
    /// <param name="duration">眨眼过程时间</param>
    /// <returns>Blink Value</returns>
    protected float AutoBlinkCtrl(float minInterval, float maxInterval, float duration, AnimationCurve blinkCurve)
    {
        var r = Random.Range(minInterval, maxInterval);
        if (Time.time - _timer > r)
        {
            _timer = Time.time;
            DOTween.To(() => _eyeAutoBlinkTimer, v => _eyeAutoBlinkTimer = v, 1, duration).onComplete += () => _eyeAutoBlinkTimer = 0;
        }

        return blinkCurve.Evaluate(_eyeAutoBlinkTimer);
    }

    /// <summary>
    /// 混合表情
    /// </summary>
    /// <param name="expn1">表情1</param>
    /// <param name="expn2">表情2</param>
    /// <param name="value">最终值</param>
    /// <param name="blend">混合值</param>
    protected void Switch(ref float expn1, ref float expn2, float value, float blend)
    {
        expn1 = blend < 0.5f ? value : 0;
        expn2 = blend >= 0.5f ? value : 0;
    }
    protected void Switch(ref float expn1, ref float expn2, ref float expn3, float value, float blend)
    {
        expn1 = blend < 0.33f ? value : 0;
        expn2 = blend >= 0.33f && blend < 0.66f ? value : 0;
        expn3 = blend >= 0.66f ? value : 0;
    }
    protected void Switch(ref float expn1, ref float expn2, ref float expn3, ref float expn4, float value, float blend)
    {
        expn1 = blend < 0.25f ? value : 0;
        expn2 = blend >= 0.25f && blend < 0.5f ? value : 0;
        expn3 = blend >= 0.5f && blend < 0.75f ? value : 0;
        expn4 = blend >= 0.75f ? value : 0;
    }
    protected void Switch(ref float expn1, ref float expn2, ref float expn3, ref float expn4, ref float expn5, float value, float blend)
    {
        expn1 = blend < 0.2f ? value : 0;
        expn2 = blend >= 0.2f && blend < 0.4f ? value : 0;
        expn3 = blend >= 0.4f && blend < 0.6f ? value : 0;
        expn4 = blend >= 0.6f && blend < 0.8f ? value : 0;
        expn5 = blend >= 0.8f ? value : 0;
    }
}