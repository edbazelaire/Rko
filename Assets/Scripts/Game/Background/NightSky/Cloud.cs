using UnityEngine;

namespace Game.Background.NightSky
{
    public class Cloud : MonoBehaviour
    {
        private CloudSpawner m_Spawner;
        private float m_Speed;

        public void Initialize(CloudSpawner spawner, float speed)
        {
            m_Spawner = spawner;
            m_Speed = speed;
        }

        private void Update()
        {
            transform.Translate(Vector3.left * m_Speed * Time.deltaTime);

            // Example: recycle when leaving the right side of the camera
            if (transform.position.x > -10f)
            {
                m_Spawner.RecycleCloud(this);
            }
        }

        /// <summary>
        /// Reset cloud position and speed for reuse instead of destroying/instantiating.
        /// </summary>
        public void ResetCloud(Vector3 newPos, float newSpeed)
        {
            transform.position = newPos;
            m_Speed = newSpeed;
        }
    }

}