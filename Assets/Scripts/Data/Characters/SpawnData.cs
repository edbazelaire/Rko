using UnityEditor;
using UnityEngine;

namespace Data.Characters
{
    [CreateAssetMenu(fileName = "SpawnData", menuName = "Game/SpawnData")]
    public class SpawnData : CharacterData
    {
        [Header("Spawn")]
        [SerializeField, Tooltip("Default Min/Max range on X to spawn")]
        SMinMax m_SpawnOffsetX;
        [SerializeField, Tooltip("Default Min/Max range on Y to spawn")]
        SMinMax m_SpawnOffsetY;
        [SerializeField, Tooltip("Offset of the spawn in the info PopUp (to adjust)")]
        public Vector3 PopUpOffset;
        [SerializeField, Tooltip("Scaling of the spawn in the info PopUp (to adjust)")]
        public float PopUpScale = 1f;



        #region Target & Position

        public Vector3 GetSpawnOffset()
        {
            return new Vector3(
                m_SpawnOffsetX.Min >= m_SpawnOffsetX.Max ? m_SpawnOffsetX.Min : Random.Range(m_SpawnOffsetX.Min, m_SpawnOffsetX.Max),
                m_SpawnOffsetY.Min >= m_SpawnOffsetY.Max ? m_SpawnOffsetY.Min : Random.Range(m_SpawnOffsetY.Min, m_SpawnOffsetY.Max),
                0
            );
        }

        #endregion
    }
}