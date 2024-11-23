using Data;
using Enums;
using Game;
using Game.Spells;
using System;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public class SMultiSpellTarget
    {
        [SerializeField]
        protected EMultiSpellSpawn m_MultiProjectileSpawn = EMultiSpellSpawn.Random;
        [SerializeField]
        protected float m_ZoneSize = -1f;
        [SerializeField]
        protected int m_NBreakPoints = 0;
        [SerializeField]
        protected SOffset m_OffsetMin;
        [SerializeField]
        protected SOffset m_OffsetMax;

        public EMultiSpellSpawn         MultiSpellSpawn    => m_MultiProjectileSpawn;
        public float                    ZoneSize                => m_ZoneSize;
        public int                      NBreakPoints            => m_NBreakPoints;
        public SOffset                  OffsetMin               => m_OffsetMin;
        public SOffset                  OffsetMax               => m_OffsetMax;

        public virtual Vector3 RecalculateTarget(Vector3 target, int index, int team, int nProjectiles)
        {
            if (ArenaManager.Instance == null)
            {
                ErrorHandler.Error("Unable to find ArenaManager to calculate target position - exiting");
                return default;
            }    

            (float min, float max) = ArenaManager.GetAreaBounds(target.x);

            var zoneSize = m_ZoneSize;
            // if projectile size < 0 : use all the size of the arena
            if (zoneSize < 0f)
                zoneSize = ArenaManager.Instance.TargettableAreaSize;

            int nBreakPoints = m_NBreakPoints;
            int breakpointIndex;
            switch (MultiSpellSpawn)
            {
                case EMultiSpellSpawn.None:
                    break;

                case (EMultiSpellSpawn.Random):
                    if (m_NBreakPoints > 0)
                    {
                        breakpointIndex = UnityEngine.Random.Range(1, nBreakPoints + 1);
                        target.x += ((team == 0 ? -1 : 1) * zoneSize / 2) + breakpointIndex * (team == 0 ? 1 : -1) * zoneSize / (nBreakPoints + 1);
                    }

                    else
                    {
                        target.x += UnityEngine.Random.Range(-zoneSize / 2, zoneSize / 2);
                    }

                    break;

                case (EMultiSpellSpawn.Line):
                    if (m_NBreakPoints <= 0)
                    {
                        nBreakPoints = nProjectiles + 1;
                    }

                    breakpointIndex = (index % nBreakPoints) + 1;
                    target.x += ((team == 0 ? -1 : 1) * zoneSize / 2) + breakpointIndex * (team == 0 ? 1 : -1) * zoneSize / nBreakPoints;

                    break;

                default:
                    ErrorHandler.Warning("Unhandled MultiSpellSpawn : " + MultiSpellSpawn);
                    break;
            }

            target.x = Mathf.Clamp(UnityEngine.Random.Range(target.x - m_OffsetMin.X , target.x + m_OffsetMax.X), min, max);
            target.y = UnityEngine.Random.Range(target.y - m_OffsetMin.Y, target.y + m_OffsetMax.Y);

            return target;
        }
    }
}