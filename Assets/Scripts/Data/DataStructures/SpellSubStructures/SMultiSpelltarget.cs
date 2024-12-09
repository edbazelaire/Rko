using Data;
using Data.GameManagement;
using Enums;
using Game;
using Game.SpellGFXs;
using Game.Spells;
using System;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public class SSpellTargetDim
    {
        [SerializeField]
        public EDimension       m_Dimension         = EDimension.X;
        [SerializeField]
        public EMultiSpellZone  m_MultiSpellTarget  = EMultiSpellZone.Random;
        [SerializeField]
        protected float         m_ZoneSize          = -1f;
        [SerializeField]
        protected int           m_NBreakPoints      = 1;
        [SerializeField]
        protected SMinMax       m_OffsetMinMax;

        public EMultiSpellZone  MultiSpellSpawn     => m_MultiSpellTarget;
        public float            ZoneSize            => m_ZoneSize;
        public int              NBreakPoints        => m_NBreakPoints;
        public SMinMax          OffsetMinMax        => m_OffsetMinMax;

        public virtual float Recalculate(float value, int index, int team, int nProjectiles)
        {
            if (ArenaManager.Instance == null)
            {
                ErrorHandler.Error("Unable to find ArenaManager to calculate target position - exiting");
                return default;
            }

            // get bounds (depending on dims)
            float min; float max;
            if (m_Dimension == EDimension.X)
                (min, max) = ArenaManager.GetAreaBounds(value);
            else
            {
                min = 0; 
                max = 50f;
            }

            var zoneSize = m_ZoneSize;
            // if projectile size < 0 : use all the size of the arena
            if (zoneSize < 0f)
            {
                switch (m_Dimension)
                {
                    case EDimension.X:
                        zoneSize = ArenaManager.Instance.TargettableAreaSize;
                        break;

                    case EDimension.Y:
                        zoneSize = Settings.SPELL_DIAGONAL_POS_Y;
                        break;
                }
            }

            int nBreakPoints = m_NBreakPoints;
            int breakpointIndex;
            int direction = (team == 0 || m_Dimension == EDimension.Y) ? 1 : -1;
            switch (MultiSpellSpawn)
            {
                case EMultiSpellZone.None:
                    break;

                case (EMultiSpellZone.Random):
                    if (m_NBreakPoints > 0)
                    {
                        breakpointIndex = UnityEngine.Random.Range(1, nBreakPoints + 1);
                        value += (-direction * zoneSize / 2) + breakpointIndex * direction * zoneSize / (nBreakPoints + 1);
                    }

                    else
                    {
                        value += UnityEngine.Random.Range(-zoneSize / 2, zoneSize / 2);
                    }

                    break;

                case (EMultiSpellZone.Line):
                    if (m_NBreakPoints <= 0)
                    {
                        nBreakPoints = nProjectiles + 1;
                    }

                    breakpointIndex = (index % nBreakPoints) + 1;
                    value += (-direction * zoneSize / 2) + breakpointIndex * direction * zoneSize / (nBreakPoints + 1);

                    break;

                default:
                    ErrorHandler.Warning("Unhandled MultiSpellSpawn : " + MultiSpellSpawn);
                    break;
            }

            var offset = UnityEngine.Random.Range(m_OffsetMinMax.Min, m_OffsetMinMax.Max);
            return Mathf.Clamp(value + offset, min, max);
        }
    }
    
    [Serializable]
    public class SMultiSpellTarget
    {
        [SerializeField]
        protected EMultiSpellZone m_MultiProjectileSpawn = EMultiSpellZone.Random;
        [SerializeField]
        protected float m_ZoneSize = -1f;
        [SerializeField]
        protected int m_NBreakPoints = 0;
        [SerializeField]
        protected SOffset m_OffsetMin;
        [SerializeField]
        protected SOffset m_OffsetMax;

        public EMultiSpellZone          MultiSpellSpawn         => m_MultiProjectileSpawn;
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
                case EMultiSpellZone.None:
                    break;

                case (EMultiSpellZone.Random):
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

                case (EMultiSpellZone.Line):
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


    [Serializable]
    public class SMultiSpellSpawn
    {
        [SerializeField]
        public SSpellTargetDim m_SpellTargetX;
        public SSpellTargetDim m_SpellTargetY;

        public virtual Vector3 Recalculate(Vector3 position, int index, int team, int nProjectiles)
        {
            return new Vector3(
                m_SpellTargetX.Recalculate(position.x, index, team, nProjectiles),
                m_SpellTargetY.Recalculate(position.y, index, team, nProjectiles),
                0f
            );
        }
    }
}