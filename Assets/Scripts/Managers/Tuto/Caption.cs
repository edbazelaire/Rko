using Enums;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class Caption : MObject
{
    #region Members

    Image m_CaptionImage;
    TMP_Text m_Text;

    bool m_IsDoneWriting = true;

    public bool IsDoneWriting => m_IsDoneWriting;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_CaptionImage = Finder.FindComponent<Image>(gameObject, "CaptionImage");
        m_Text = Finder.FindComponent<TMP_Text>(gameObject, "Text");
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();

        m_CaptionImage.gameObject.SetActive(false);
    }

    #endregion


    #region Caption Manipulation

    public void Activate(bool activate)
    {
        gameObject.SetActive(activate);
    }

    public void Write(string text, ECaptionType captionType = ECaptionType.Normal, ECaptionColor captionColor = ECaptionColor.None)
    {
        gameObject.SetActive(true);
        StartCoroutine(StartWrittingAnimation(text, captionType, captionColor));
    }

    public IEnumerator StartWrittingAnimation(string text, ECaptionType captionType = ECaptionType.Normal, ECaptionColor captionColor = ECaptionColor.None)
    {
        m_IsDoneWriting = false;

        m_Text.gameObject.SetActive(true);
        m_Text.text = text;
        m_Text.color = captionColor == ECaptionColor.Black ? Color.white : Color.black;
        SetCaption(captionType, captionColor);

        yield return new WaitForSeconds(3f);

        m_IsDoneWriting = true;
    }

    public void SetCaption(ECaptionType captionType, ECaptionColor captionColor = ECaptionColor.None)
    {
        m_CaptionImage.gameObject.SetActive(true);

        if (captionType != ECaptionType.None)
            m_CaptionImage.sprite = AssetLoader.LoadCaption(captionType, captionColor);
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