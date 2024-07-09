using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Background.NightSky
{
    public class Cloud : MonoBehaviour
    {
        #region Members

        SpriteRenderer m_SpriteRenderer;
        float m_Speed = 0f;

        #endregion

        public void Initialize(Sprite sprite, float speed, float sizeFactor, int sorterOrder, string layer)
        {
            m_SpriteRenderer = Finder.FindComponent<SpriteRenderer>(gameObject);
            m_SpriteRenderer.sprite = sprite;
            m_SpriteRenderer.sortingOrder = sorterOrder;
            m_SpriteRenderer.sortingLayerName = layer;

            transform.localScale *= sizeFactor;

            m_Speed = speed;
        }

        public void Update()
        {
            transform.position += new Vector3(- Time.deltaTime * m_Speed, 0f, 0f);

            // CHECK IF IN VIEW
        }
    }
}