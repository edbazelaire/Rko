using Game.Background;
using System.Collections;
using Tools.Animations;
using UnityEngine;

public class IntroMaker2 : MObject
{
    #region Members

    [SerializeField] GameObject         m_Logo;


    #endregion


    #region Init & End

    protected override void Start()
    {
        base.Start();
        StartCoroutine(StartAnimation());
    }

    #endregion


    #region Start

    public IEnumerator StartAnimation()
    {
        m_Logo.transform.localScale = Vector3.zero;

        // WAIT (for clean screen capture)
        yield return new WaitForSeconds(1f);

        // FADE IN : Logo
        var fadeIn = m_Logo.AddComponent<Fade>();
        fadeIn.Initialize(duration: 3f, startOpacity: 0f, startScale: 0.3f, endScale: 0.6f);
    }

    #endregion
}
