using Enums;
using System.Collections;
using Tools;
using UnityEngine;


namespace Game.UI
{
    public class EmotUI : MObject
    {
        #region Members

        [SerializeField] float DISPLAY_TIME = 2f;

        SpriteRenderer m_SpriteRenderer;
        float m_Timer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_SpriteRenderer = Finder.FindComponent<SpriteRenderer>(gameObject, "EmotIcon");
        }

        public void Initialize(EEmot emot)
        {
            base.Initialize();

            m_Timer = DISPLAY_TIME;
            SetEmot(emot);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        public void End()
        {
            Destroy(gameObject);
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!m_Initialized)
                return;

            if (m_Timer > 0)
            {
                m_Timer -= Time.deltaTime;
                return;
            }

            End();
        }

        private void LateUpdate()
        {
            // Freeze rotation (world-space)
            transform.rotation = Quaternion.identity; 
        }

        #endregion


        #region GUI Manipulators

        public void SetEmot(EEmot emot)
        {
            m_SpriteRenderer.sprite = AssetLoader.Load<Sprite>(emot.ToString(), AssetLoader.c_EmotsSpritePath);
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
}
