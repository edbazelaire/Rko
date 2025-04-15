using Enums;
using System;
using TMPro;
using Tools;
using TS.DoubleSlider;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class SynchronizedDoubleSlider : MObject
{
    #region Members

    TMP_Text m_Title;
    DoubleSlider m_Slider;

    protected EPlayerPref m_Option;
    protected float m_MinValue;
    protected float m_MaxValue;
    float m_BaseMinValue;
    float m_BaseMaxValue;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_Title = Finder.FindComponent<TMP_Text>(gameObject, "Title");
        m_Slider = Finder.FindComponent<DoubleSlider>(gameObject);
    }

    public void Initialize(string title, EPlayerPref option, float baseMinValue, float baseMaxValue, float minValue, float maxValue)
    {
        m_Option    = option;
        m_BaseMinValue = PlayerPrefs.GetFloat(option.ToString() + "Min", baseMinValue);
        m_BaseMaxValue = PlayerPrefs.GetFloat(option.ToString() + "Max", baseMaxValue);
        m_MinValue  = minValue;
        m_MaxValue  = maxValue;

        base.Initialize();

        m_Title.text = title;
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();

        m_Slider.Setup(m_MinValue, m_MaxValue, m_BaseMinValue, m_BaseMaxValue);
    }

    #endregion


    #region GUI Manipulators

    #endregion


    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();

        m_Slider.OnValueChanged += OnSliderValueChanged;
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();

        m_Slider.OnValueChanged -= OnSliderValueChanged;
    }

    private void OnSliderValueChanged(float minValue, float maxValue)
    {
        PlayerPrefs.SetFloat(m_Option.ToString() + "Min", minValue);
        PlayerPrefs.SetFloat(m_Option.ToString() + "Max", maxValue);
    }

    #endregion
}
