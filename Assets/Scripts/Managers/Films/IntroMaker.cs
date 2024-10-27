using Game.Background;
using System.Collections;
using System.Collections.Generic;
using Tools.Animations;
using UnityEngine;

public class IntroMaker : MObject
{
    #region Members

    [SerializeField] Camera             m_Camera;
    [SerializeField] GameObject         m_Logo;
    [SerializeField] NightSkyBackground m_Background;
    [SerializeField] float              m_ScrollSpeed = 1f;
    [SerializeField] float              m_YendPos = -10f;
    [SerializeField] float              m_ZoomSpeed = 1f;
    [SerializeField] float              m_EndZoom = 8f;

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

        // BACKGROUND : animate
        m_Background.Initialize();

        // FADE IN : Logo
        var fadeIn = m_Logo.AddComponent<Fade>();
        fadeIn.Initialize(duration: 3f, startOpacity: 0f, startScale: 0.7f, endScale: 1f);
        yield return new WaitForSeconds(0.8f);

        yield return CameraPanDown();

        yield return CameraZoomOut();
    }

    #endregion


    #region Animation

    IEnumerator CameraPanDown()
    {
        while (m_Camera.transform.position.y > m_YendPos)
        {
            var position = m_Camera.transform.position;
            position.y -= Time.deltaTime * m_ScrollSpeed;
            m_Camera.transform.position = position;

            yield return null;
        }
    }

    IEnumerator CameraZoomOut()
    {
        while (m_Camera.orthographicSize < m_EndZoom)
        {
            var position = m_Camera.transform.position;
            position.y += Time.deltaTime * m_ScrollSpeed / 2;

            m_Camera.transform.position = position;
            m_Camera.orthographicSize += Time.deltaTime * m_ZoomSpeed;
            yield return null;
        }
    }

    #endregion
}
