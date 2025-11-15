using Data.DataStructures.CharacterSubStructures;
using Enums;
using Game.Character.Controllers;
using Game.Spells;
using System;
using Tools;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures.SpellModules
{
    [Serializable]
    public class MSolidBody
    {
        #region Members

        // =======================================================================
        // Serialized data
        [SerializeField, Tooltip("Prefab game object of the body to spawn")]
        GameObject m_BodyPrefab;
        [SerializeField, Tooltip("Size of the body (as percentag of the spell's size)")]
        float m_BodySizePerc    = 1f;
        [SerializeField, Tooltip("Hp of the body")]
        SCharacterStatScaling m_Hp = new SCharacterStatScaling(EStateEffectProperty.Hp, 1f, 0f, 0.1f);

        // =======================================================================
        // Public accessors
        public GameObject   BodyPrefab      => m_BodyPrefab;
        public float        BodySizePerc    => m_BodySizePerc;

        #endregion


        #region 

        public void Initialize(Spell spell, int level)
        {
            var body = GameObject.Instantiate(BodyPrefab, spell.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale *= m_BodySizePerc;

            // add life component
            var controller = body.AddComponent<BodyController>();
            controller.Initialize(spell, (int)Math.Round(m_Hp.GetValue(level)));
        }

        #endregion
    }
}