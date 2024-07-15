using Data;
using Enums;
using Game.Loaders;
using Game.SpellGFXs;
using Game.Spells;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class GFXHandler : NetworkBehaviour
    {
        #region Members

        // ===========================================================================
        // DATA
        List<SPrefabSpawn> m_PrefabSpawns = new List<SPrefabSpawn>();

        /// <summary> list of visual effects proc by state effects</summary>
        Dictionary<string, List<SpellGFX>> m_StateEffectGraphics;
        /// <summary> list of colors of the state effect </summary>
        List<Color> m_Colors;

        // ===========================================================================
        // Private Components
        // original Controller
        Controller m_Controller;
        /// <summary> preview of the character </summary>
        GameObject m_CharacterPreview;
        /// <summary> sprite renderer of the Character</summary>
        List<SpriteRenderer> m_SpriteRenderers;
        /// <summary> list of body parts </summary>
        Dictionary<EBodyPart, GameObject> m_BodyParts;

        // ===========================================================================
        // PUBLIC ACCESSORS
        public GameObject CharacterPreview => m_CharacterPreview;
        public Dictionary<EBodyPart, GameObject> BodyParts => m_BodyParts;

        #endregion


        #region Init & End
         
        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);

            m_Controller.SpellHandler.OnPreSpellEvent += OnPreSpellEvent;
        }

        public void Initialize(ECharacter character)
        {
            CharacterData characterData = CharacterLoader.GetCharacterData(character);
            m_CharacterPreview = characterData.InstantiateCharacterPreview(gameObject);
            m_SpriteRenderers = Finder.FindComponents<SpriteRenderer>(m_CharacterPreview);

            FindBodyParts();

            m_StateEffectGraphics = new();
            m_Colors = new();

            m_Controller.StateHandler.StateEffectList.OnListChanged += OnStateEffectListChanged;
        }

        public void Activate(bool activate)
        {
            if (activate)
                return;

            // TODO : end all prefab spawns
        }

        #endregion


        #region CLIENT RPC

        /// <summary>
        /// change the color of this character on each clients
        /// </summary>
        /// <param name="color"></param>
        [ClientRpc]
        public void AddColorClientRPC(Color color)
        {
            AddColor(color);
        }

        /// <summary>
        /// change the color of this character on each clients
        /// </summary>
        /// <param name="color"></param>
        [ClientRpc]
        public void RemoveColorClientRPC(Color color)
        {
            RemoveColor(color);
        }

        /// <summary>
        /// Call clients to hide/display a character
        /// </summary>
        /// <param name="hidden"></param>
        [ClientRpc]
        public void HideCharacterClientRPC(bool hidden)
        {
            HideCharacter(hidden);
        }

        #endregion


        #region Body Parts

        void FindBodyParts()
        {
            m_BodyParts = new Dictionary<EBodyPart, GameObject>();
            foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
            {
                if (bodyPart == EBodyPart.None)
                    continue;

                m_BodyParts[bodyPart] = Finder.Find(m_CharacterPreview, bodyPart.ToString() + "Effector");
            }
        }

        public bool TryGetBodyPart(EBodyPart bodyPart, out GameObject bodyPartGO, bool trackError = false)
        {
            bodyPartGO = GetBodyPart(bodyPart, trackError);
            if (bodyPartGO == null)
                return false;

            return true;
        }

        public GameObject GetBodyPart(EBodyPart bodyPart, bool trackError = true)
        {
            if (!m_BodyParts.ContainsKey(bodyPart))
            {
                if (trackError)
                    ErrorHandler.Error("BodyPart " + bodyPart + " not found in character " + m_Controller.Character);
                return null;
            }

            if (m_BodyParts[bodyPart] == null)
            {
                if (trackError)
                    ErrorHandler.Error("BodyPart " + bodyPart + " is null for character " + m_Controller.Character);
                return null;
            }

            return m_BodyParts[bodyPart];
        }

        #endregion


        #region Particles

        public void SpawnVisualEffect(GameObject visualEffect, EBodyPart bodyPart)
        {

        }

        #endregion


        #region State Effects

        public List<SpellGFX> SpawnStateEffectGFX(StateEffect stateEffect, ESpellEvent spellEvent)
        {
            List<SpellGFX> listSpellGFX = new();
            if (stateEffect.VisualEffects == null || stateEffect.VisualEffects.Count == 0)
                return listSpellGFX;

            ErrorHandler.Log("SpawnSpellEffectGraphics : " + stateEffect.name, ELogTag.Animation);

            foreach (SPrefabSpawn prefabSpawn in stateEffect.VisualEffects)
            {
                if (prefabSpawn.GFXLifetime.StartSpellPart == spellEvent)
                    prefabSpawn.Spawn(null, null, null, targetController: m_Controller);
            }

            return listSpellGFX;
        }

        /// <summary>
        /// change the color of this character on each clients
        /// </summary>
        /// <param name="color"></param>
        public void RemoveSpellEffectGraphics(string effectName)
        {
            if (! m_StateEffectGraphics.ContainsKey(effectName))
                return;

            ErrorHandler.Log("Removing spell effect : " + effectName, ELogTag.Animation);

            foreach (var spellGfx in m_StateEffectGraphics[effectName])
            {
                spellGfx.End();
            }

            m_StateEffectGraphics.Remove(effectName);
        }

        public List<SpellGFX> SpawnSpellGFX(string spellName, ESpellEvent spellEvent)
        {
            var spellData = SpellLoader.GetSpellData(spellName);
            List<SpellGFX> listSpellGFX = new();
            foreach (SPrefabSpawn prefabSpawn in spellData.SpellEventActions)
            {
                if (prefabSpawn.GFXLifetime.StartSpellPart == spellEvent)
                    prefabSpawn.Spawn(m_Controller, spellData, null);
            }

            return listSpellGFX;
        }

        #endregion


        #region Colors

        /// <summary>
        /// Hide / show the character colors
        /// </summary>
        /// <param name="hidden"></param>
        public void HideCharacter(bool hidden)
        {
            Color color = hidden ? new Color(0f, 0f, 0f, 0f) : Color.white;
            SetColor(color);
        }

        void AddColor(Color color)
        {
            m_Colors.Add(color);
            SetColor(color);
        }

        void RemoveColor(Color color)
        {
            m_Colors.Remove(color);
            color = m_Colors.Count > 0 ? m_Colors.Last() : Color.white;
            SetColor(color);
        }

        void SetColor(Color color)
        {
            foreach (var spriteRenderer in m_SpriteRenderers)
                spriteRenderer.color = color;
        }

        #endregion


        #region Listeners

        void OnPreSpellEvent(string spellName, ESpellEvent spellEvent)
        {
            ErrorHandler.Log(spellName + " OnPreSpellEvent : " + spellEvent, ELogTag.SpellGFX);
            SpawnSpellGFX(spellName, spellEvent);   
        }

        /// <summary>
        /// When a state effect is added or removed
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnStateEffectListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            ErrorHandler.Log(changeEvent.Type + " " + changeEvent.Value, ELogTag.Animation);

            if (changeEvent.Type != NetworkListEvent<FixedString64Bytes>.EventType.RemoveAt && changeEvent.Type != NetworkListEvent<FixedString64Bytes>.EventType.Remove)
                OnAddStateEffect(changeEvent.Value.ToString());
            else
                OnRemoveStateEffect(changeEvent.Value.ToString());

            // ---------------------------------------------------------------------------------------
            // SPECIAL EFFECTS
            if (changeEvent.Value == EStateEffect.Invisible.ToString())
            {
                float opacity = 1f;

                if (changeEvent.Type != NetworkListEvent<FixedString64Bytes>.EventType.RemoveAt)
                    opacity = IsOwner ? 0.5f : 0f;

                SetColor(new Color(1f, 1f, 1f, opacity));
                return;
            }
        }

        void OnAddStateEffect(string stateEffectName)
        {
            StateEffect stateEffect = SpellLoader.GetStateEffect(stateEffectName);

            if (stateEffect.VisualEffects != null)
                SpawnStateEffectGFX(stateEffect, ESpellEvent.OnSpawn);

            if (stateEffect.ColorSwitch != Color.white)
                AddColor(stateEffect.ColorSwitch);
        }

        void OnRemoveStateEffect(string stateEffectName)
        {
            StateEffect data = SpellLoader.GetStateEffect(stateEffectName);

            if (data.VisualEffects != null)
                RemoveSpellEffectGraphics(stateEffectName);

            if (data.ColorSwitch != Color.white)
                RemoveColor(data.ColorSwitch);

            // spawn on end effects
            SpawnStateEffectGFX(data, ESpellEvent.OnEnd);
        }

        #endregion
    }
}