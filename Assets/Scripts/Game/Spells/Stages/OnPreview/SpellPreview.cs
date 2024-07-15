using Game.Character;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.Spells
{
    public class SpellPreview : MonoBehaviour
    {
        #region Members

        const string                    c_Graphics          = "Graphics";

        protected Color                 m_Color             = Color.red;
        protected Transform             m_TargettableArea;
        protected float                 m_Distance;
        protected SpriteRenderer        m_Graphics;

        #endregion


        #region Initialization

        public virtual void Initialize(Transform targettableArea, float distance, float radius = 0)
        {
            m_TargettableArea = targettableArea;
            m_Distance = distance;
            m_Graphics = Finder.FindComponent<SpriteRenderer>(gameObject, c_Graphics);
        }

        #endregion


        #region Inherited Manipulators

        protected virtual void Update()
        {
            
        }

        #endregion
    }
}