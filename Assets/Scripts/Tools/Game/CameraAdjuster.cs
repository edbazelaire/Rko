using System.Collections;
using System.ComponentModel;
using Tools;
using UnityEngine;


public class CameraAdjuster : MObject
{
    #region Members

    [Description("Base reference width of your design")]
    [SerializeField] protected float m_BaseReferenceWidth = 2436f;

    [Description("Base reference height of your design")]
    [SerializeField] protected float m_BaseReferenceHeight = 1125f;
    
    [Description("Base orthographic size of your camera")]
    [SerializeField] protected float m_BaseOrthographicSize = 3f;

    // Reference to the camera you want to adjust
    protected Camera m_CameraToAdjust; 

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_CameraToAdjust = Finder.FindComponent<Camera>(gameObject);
    }

    public override void Initialize()
    {
        base.Initialize();

        AdjustCameraSize();
    }

    #endregion


    #region GUI Manipulators

    void AdjustCameraSize()
    {
        if (m_CameraToAdjust == null || !m_CameraToAdjust.orthographic)
        {
            Debug.LogError("CameraAdjuster: Please assign an orthographic camera to 'm_CameraToAdjust'.");
            return;
        }

        float targetAspect = m_BaseReferenceWidth / m_BaseReferenceHeight;
        float currentAspect = UIHelper.ScreenRatio;

        // Calculate the difference in aspect ratios
        float differenceInAspectRatio = currentAspect / targetAspect;

        if (differenceInAspectRatio <= 0)
        {
            ErrorHandler.Error("Unable to resize Camera, differenceInAspectRatio is <= 0 : " + differenceInAspectRatio);
            return;
        }

        // Adjust the orthographic size based on the difference in aspect ratios
        m_CameraToAdjust.orthographicSize = m_BaseOrthographicSize / differenceInAspectRatio;

        // rescale the background to match Height or Width
        if (GameUIManager.Instance != null)
        {
            float backgroundScale = Mathf.Max(Screen.width / m_BaseReferenceWidth, Screen.height / m_BaseReferenceHeight);
            GameUIManager.Instance.RescaleBackground(backgroundScale);
        }
    }

    #endregion


    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();
    }

    #endregion
}
