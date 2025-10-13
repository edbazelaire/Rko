using Data.GameManagement;
using Enums;
using Game.SpellGFXs;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class Charging : SpellGFX
    {
        #region Members

        [SerializeField] protected List<SChargingObject> m_ChargingObjects = new();
        float m_Timer;

        #endregion


        #region Init & End

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            m_Timer = m_Duration;
            foreach (SChargingObject chargingObj in m_ChargingObjects)
            {
                chargingObj.Initialize();
            }
        }

        #endregion


        #region Update Manipulators

        protected virtual void Update()
        {
            if (GameManager.IsGameOver)
                return;

            if (m_Timer <= 0f)
                return;

            m_Timer = Math.Max(0, m_Timer - Time.deltaTime);
            foreach (SChargingObject chargingObj in m_ChargingObjects)
            {
                chargingObj.Update((m_Duration - m_Timer) / m_Duration);
            }
        }

        #endregion
    }


    [Serializable]
    public class SChargingObject
    {
        #region Members

        [SerializeField] protected GameObject Object;

        [SerializeField] protected float StartAt    = 0f;
        [SerializeField] protected float EndAt      = 1f;
        [SerializeField] protected float StartSize  = 0f;
        [SerializeField] protected float EndSize    = 1f;

        [SerializeField] protected bool m_ScaleX    = true;
        [SerializeField] protected bool m_ScaleY    = true;

        Vector3 m_BaseScale;
        bool m_Abort = false;

        #endregion


        public void Initialize()
        {
            // =================================================================================================
            // Safety checks
            if (Object == null)
            {
                ErrorHandler.Warning("Bad StartAt provided (" + StartAt + ") for " + Object.name + " - must be >= 0");
                m_Abort = true;
                return;
            }

            if (StartAt < 0f)
            {
                ErrorHandler.Warning("Bad StartAt provided ("+StartAt+") for " + Object.name + " - must be >= 0");
                StartAt = 0f;
            } else if (StartAt > 1f)
            {
                ErrorHandler.Warning("Bad StartAt provided ("+StartAt+") for " + Object.name + " - must be <= 1");
                StartAt = 1f;
            }

            if (EndAt < 0f)
            {
                ErrorHandler.Warning("Bad EndAt provided (" + EndAt + ") for " + Object.name + " - must be <= 1");
                EndAt = 0f;
            } else if (EndAt > 1f)
            {
                ErrorHandler.Warning("Bad EndAt provided (" + EndAt + ") for " + Object.name + " - must be <= 1");
                EndAt = 1f;
            }

            if (EndAt < StartAt)
            {
                ErrorHandler.Warning("Bad EndAt provided (" + EndAt + ") for " + Object.name + " - must be >= Startat : " + StartAt);
                m_Abort = true;
                return;
            }

            // =================================================================================================
            // Init
            // -- save local scale
            m_BaseScale = Object.transform.localScale;

            if (StartAt > 0f)
            {
                Object.SetActive(false);
                return;
            }
        }

        public void Update(float percTimer)
        {
            if (m_Abort)
                return;

            if (percTimer < StartAt || percTimer > EndAt)
                return;

            if (Object == null)
            {
                ErrorHandler.Error("Unable to find object");
                m_Abort = true;
                return;
            }

            // -- activate if not done yet
            if (! Object.activeSelf)
                Object.SetActive(true);

            float actualPerc = (percTimer - StartAt) / (EndAt - StartAt);
            SetSize(StartSize + actualPerc * (EndSize - StartSize));
        }

        public void SetSize(float size)
        {
            var scale = m_BaseScale;
            if (m_ScaleX)
                scale.x *= size;
            if (m_ScaleY)
                scale.y *= size;

            Object.transform.localScale = scale;
        }
    }
}