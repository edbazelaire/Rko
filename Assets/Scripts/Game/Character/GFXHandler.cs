using Assets.Scripts.Managers.Sound;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
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
        /// <summary> list of colors of the state effect </summary>
        Dictionary<EBodyPart, List<Color>> m_Colors;
        /// <summary> list of colors of the state effect </summary>
        Dictionary<EBodyPart, List<Material>> m_Materials;
        /// <summary> default material of sprites </summary>
        Material m_DefaultMaterial;
        float m_CharacterSize;

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
        public float CharacterSize => m_CharacterSize;

        #endregion


        #region Init & End
         
        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
        }

        public void Initialize(string character)
        {
            CharacterData characterData = CharacterLoader.GetCharacterData(character, destroy: true);
            m_CharacterPreview = characterData.InstantiateCharacterPreview(gameObject);
            m_SpriteRenderers = Finder.FindComponents<SpriteRenderer>(m_CharacterPreview);

            FindBodyParts();
            SetSize(characterData.Size);

            m_Colors = new();
            m_Materials = new();
            foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
            {
                m_Colors.Add(bodyPart, new());
                m_Materials.Add(bodyPart, new());
            }
            m_DefaultMaterial = m_SpriteRenderers[0].material;

            m_Controller.SpellHandler.OnPreSpellEvent               += OnPreSpellEvent;
            m_Controller.StateHandler.StateEffectList.OnListChanged += OnStateEffectListChanged;
        }

        public override void OnDestroy()
        {
            m_Controller.StateHandler.StateEffectList.OnListChanged -= OnStateEffectListChanged;
        }

        #endregion


        #region Size

        /// <summary>
        /// Initialize size of the character
        /// </summary>
        public void SetSize(float size)
        {
            m_CharacterSize = Settings.CharacterSizeFactor * size;
            transform.localScale = m_CharacterSize * Vector3.one;
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

                m_BodyParts[bodyPart] = Finder.Find(m_CharacterPreview, bodyPart.ToString() + "Effector", false);
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


        #region Spell GFX

        /// <summary>
        /// [LOCAL CLIENT]
        /// Spawn all spell graphics linked to the provided SpellEvent
        /// </summary>
        /// <param name="spellName">    name of the spell                                   </param>
        /// <param name="spellEvent">   event called by the spell (cast, spawn, onHit, ...) </param>
        /// <param name="targetPos">    position targeted by the spell                      </param>
        public void SpawnSpellGFX(string spellName, ESpellEvent spellEvent, Vector3 targetPos = default)
        {
            // FILTER : handle on event before spell spawn (post-spell spawning is handled by the spell itself)
            // except "OnEnd" that can be called when the cast is cancelled
            if (spellEvent >= ESpellEvent.OnSpawn && spellEvent != ESpellEvent.OnEnd)
                return;

            ErrorHandler.Log(spellName + " SpawnSpellGFX : " + spellEvent, ELogTag.SpellGFX);

            var spellData = SpellLoader.GetSpellData(spellName);
            foreach (SPrefabSpawn prefabSpawn in spellData.SpellEventActions)
            {
                if (prefabSpawn.GFXLifetime.StartSpellPart != spellEvent)
                    continue;

                prefabSpawn.Spawn(
                    caster:     m_Controller, 
                    spellData:  spellData,
                    targetPos:  targetPos
                );
            }

            return;
        }

        #endregion


        #region Material

        public void ApplyMaterial(Material material, EBodyPart bodyPart = EBodyPart.None)
        {
            if (bodyPart == EBodyPart.None)
                m_Materials[bodyPart].Add(material);

            foreach (var spriteRenderer in m_SpriteRenderers)
            {
                // check if is a body part
                if (!Enum.TryParse(spriteRenderer.name, out EBodyPart tempBodyPart))
                    continue;

                // check is the right body part
                if (bodyPart != EBodyPart.None && bodyPart == tempBodyPart)
                    continue;

                // att to list of materials
                m_Materials[tempBodyPart].Add(material);
                spriteRenderer.material = material;
            }
        }

        public void RemoveMaterial(Material material, EBodyPart bodyPart = EBodyPart.None)
        {
            if (bodyPart == EBodyPart.None)
                m_Materials[bodyPart].Remove(material);
           
            foreach (var spriteRenderer in m_SpriteRenderers)
            {
                // check if is a body part
                if (!Enum.TryParse(spriteRenderer.name, out EBodyPart tempBodyPart))
                    continue;

                // check is the right body part
                if (bodyPart != EBodyPart.None && bodyPart == tempBodyPart)
                    continue;

                // check if body part has material in store
                if (!m_Materials[tempBodyPart].Contains(material))
                    continue;

                // remove from list of materials
                m_Materials[tempBodyPart].Remove(material);

                // check if is current material
                if (TextHandler.CleanMaterialName(spriteRenderer.sharedMaterial.name) != TextHandler.CleanMaterialName(material.name) )
                    continue;

                // use last available material if any or default
                spriteRenderer.material = m_Materials[tempBodyPart].Count > 0 ? m_Materials[tempBodyPart].Last() : m_DefaultMaterial;
            }
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

        void AddColor(Color color, EBodyPart bodyPart = EBodyPart.None)
        {
            if (bodyPart == EBodyPart.None)
            {
                foreach (var part in m_Colors.Keys)
                {
                    if (part == EBodyPart.None)
                        continue;
                    AddColor(color, part);
                }
                return;
            }

            m_Colors[bodyPart].Add(color);
            SetColor(color, bodyPart);
        }

        void RemoveColor(Color color, EBodyPart bodyPart = EBodyPart.None)
        {
            if (bodyPart == EBodyPart.None)
            {
                foreach (var part in m_Colors.Keys)
                {
                    if (part == EBodyPart.None)
                        continue;
                    RemoveColor(color, part);
                }
                return;
            }

            m_Colors[bodyPart].Remove(color);
            color = m_Colors[bodyPart].Count > 0 ? m_Colors[bodyPart].Last() : Color.white;
            SetColor(color, bodyPart);
        }

        void SetColor(Color color, EBodyPart bodyPart = EBodyPart.None)
        {
            foreach (var spriteRenderer in m_SpriteRenderers)
            {
                if (!Enum.TryParse(spriteRenderer.name, out EBodyPart tempBodyPart))
                    continue;

                if (bodyPart != EBodyPart.None && bodyPart == tempBodyPart)
                    continue;

                // do not apply if has a material
                if (TextHandler.CleanMaterialName(spriteRenderer.material.name) != TextHandler.CleanMaterialName(m_DefaultMaterial.name))
                   continue;

                spriteRenderer.color = color;
            }
        }

        #endregion


        #region Listeners

        void OnPreSpellEvent(string spellName, ESpellEvent spellEvent)
        {
            SpellData spellData = SpellLoader.GetSpellData(spellName, destroy: true);
            if (spellEvent == ESpellEvent.OnCast && spellData.CastSoundFX != null)
                SoundFXManager.PlayOnce(spellData.CastSoundFX);
        } 

        /// <summary>
        /// When a state effect is added or removed
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnStateEffectListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            ErrorHandler.Log(changeEvent.Type + " " + changeEvent.Value, ELogTag.Animation);

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
            
            if (changeEvent.Value == EStateEffect.Vanish.ToString())
            {
                HideCharacter(changeEvent.Type != NetworkListEvent<FixedString64Bytes>.EventType.RemoveAt);
                return;
            }
        }

        #endregion
    }
}