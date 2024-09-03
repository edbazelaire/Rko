using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Animations
{
    public class Fade : OvAnimation
    {
        #region Members

        List<(Image Image, float BaseOpacity)>          m_Images;
        List<(RawImage Image, float BaseOpacity)>       m_RawImages;
        List<(SpriteRenderer Image, float BaseOpacity)> m_Sprites;
        List<TMP_Text>                                  m_Texts;

        [SerializeField] float m_StartScale      = 1f;
        [SerializeField] float m_EndScale        = 1f;
        [SerializeField] float m_StartOpacity    = 1f;
        [SerializeField] float m_EndOpacity      = 1f;

        #endregion


        #region Init & End

        public void Initialize(string id = "", float duration = 1f, float startScale = 1f, float endScale = 1f, float startOpacity = 1f, float endOpacity = 1f)
        {
            if (duration <= 0f)
            {
                ErrorHandler.Error("Fade animation can not be set as Inifinite, duration must be > 0 : " + duration);
                return;
            }

            base.Initialize(id, duration);

            // Init sub images that might change with opacity
            FindSubImages();

            // init animation variables
            m_StartScale    = startScale;
            m_EndScale      = endScale;
            m_StartOpacity  = startOpacity;
            m_EndOpacity    = endOpacity;

            // Set initial values immediately on initialization
            transform.localScale = Vector3.one * m_StartScale;
            SetOpacity(m_StartOpacity);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            gameObject.transform.localScale = Vector3.one * m_EndScale;
            SetOpacity(m_EndOpacity);
        }

        #endregion


        #region Animation

        protected override IEnumerator AnimationFrame()
        {
            float progress = GetProgress();

            // Interpolate scale and opacity based on progress
            float currentScale = Mathf.Lerp(m_StartScale, m_EndScale, progress);
            float currentOpacity = Mathf.Lerp(m_StartOpacity, m_EndOpacity, progress);

            transform.localScale = Vector3.one * currentScale;
            SetOpacity(currentOpacity);

            m_Timer += Time.deltaTime;
            yield return null;
        }


        #endregion


        #region Helpers

        private void FindSubImages()
        {
            // add texts
            m_Texts = Finder.FindComponents<TMP_Text>(gameObject);

            // add images
            m_Images = new();
            Image[] images = Finder.FindComponents<Image>(gameObject).ToArray();
            foreach (Image image in images)
            {
                m_Images.Add((image, image.color.a));
            }

            // add Raw Images
            m_RawImages = new();
            RawImage[] rawImages = Finder.FindComponents<RawImage>(gameObject).ToArray();
            foreach (RawImage image in rawImages)
            {
                m_RawImages.Add((image, image.color.a));
            }

            // add sprites
            m_Sprites = new();
            var sprites = Finder.FindComponents<SpriteRenderer>(gameObject);
            foreach (SpriteRenderer spriteR in sprites)
            {
                m_Sprites.Add((spriteR, spriteR.color.a));
            }
        }

        /// <summary>
        /// Set Opacity of all images in this game object 
        /// </summary>
        /// <param name="opacity"></param>
        void SetOpacity(float opacity)
        {
            // Get all Image components and set alpha
            foreach (var image in m_Images)
            {
                if (image.Image == null)
                    continue;

                Color color = image.Image.color;
                color.a = image.BaseOpacity * opacity;
                image.Image.color = color;
            }

            // Get all Image components and set alpha
            foreach (var image in m_Sprites)
            {
                if (image.Image == null)
                    continue;

                Color color = image.Image.color;
                color.a = image.BaseOpacity * opacity;
                image.Image.color = color;
            }

            // Get all Texts and set opacity
            foreach (var text in m_Texts)
            {
                Color color = text.color;
                color.a = opacity;
                text.color = color;
            }

            // Get all CanvasGroup components and set their alpha
            var canvasGroups = GetComponentsInChildren<CanvasGroup>();
            foreach (var group in canvasGroups)
            {
                group.alpha = opacity;
            }
        }

        #endregion

    }
}