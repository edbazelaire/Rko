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

        public static Color INVISIBLE_COLOR => new Color(0f, 0f, 0f, 0f);

        // ===========================================================================
        // DATA
        /// <summary> list of colors of the state effect </summary>
        Dictionary<string, List<Color>> m_Colors;
        /// <summary> list of colors of the state effect </summary>
        Dictionary<string, List<Material>> m_Materials;
        /// <summary> default material of sprites </summary>
        Material m_DefaultMaterial;
        /// <summary> size of the character </summary>
        float m_CharacterSize;

        // ===========================================================================
        // Private Components
        /// <summary> base controller </summary>
        Controller                          m_Controller;
        /// <summary> preview of the character </summary>
        GameObject                          m_CharacterPreview;
        /// <summary> collider of the character </summary>
        Collider2D                          m_Collider;
        /// <summary> collider of the character </summary>
        Rigidbody2D                         m_Rigidbody;
        /// <summary> sprite renderer of the Character</summary>
        List<SpriteRenderer>                m_SpriteRenderers;
        /// <summary> list of body parts </summary>
        Dictionary<EBodyPart, GameObject>   m_BodyParts;

        // ===========================================================================
        // PUBLIC ACCESSORS
        public GameObject       CharacterPreview            => m_CharacterPreview;
        public Collider2D       Collider                    => m_Collider;
        public Rigidbody2D      Rigidbody                   => m_Rigidbody;
        public Dictionary<EBodyPart, GameObject> BodyParts  => m_BodyParts;
        public float            CharacterSize               => m_CharacterSize;

        public bool IsVisible => m_SpriteRenderers.Any(spriteRenderer => spriteRenderer.color != INVISIBLE_COLOR);

        #endregion


        #region Init & End

        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_Collider = Finder.FindComponent<Collider2D>(gameObject);
            m_Rigidbody = Finder.FindComponent<Rigidbody2D>(gameObject);
        }

        public void Initialize(string character)
        {
            CharacterData characterData = CharacterLoader.GetCharacterData(character, destroy: true);
            m_CharacterPreview = characterData.InstantiateCharacterPreview(gameObject);
            m_SpriteRenderers = Finder.FindComponents<SpriteRenderer>(m_CharacterPreview);

            FindBodyParts();
            SwapLayerMask(m_CharacterPreview);
            SwapRigidBody(m_CharacterPreview);
            SwapColliders(m_CharacterPreview);
            SetSize(characterData.Size);

            m_Colors = new();
            m_Materials = new();
            foreach (var spriteRenderer in m_SpriteRenderers)
            {
                m_Colors.Add(spriteRenderer.gameObject.name , new());
                m_Materials.Add(spriteRenderer.gameObject.name, new());
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


        #region Collider & RigidBody

        public void EnableRigidBody(bool enable = true)
        {
            m_Rigidbody.simulated = enable;
        }

        protected virtual void SwapLayerMask(GameObject graphics)
        {
            if (graphics.layer == default)
                return;

            gameObject.layer = graphics.layer;
        }

        protected virtual void SwapRigidBody(GameObject graphics)
        {
            // Check if the graphics GameObject has a Rigidbody2D component
            Rigidbody2D graphicsRb = graphics.GetComponent<Rigidbody2D>();
            if (graphicsRb == null)
                return;

            // Destroy the existing Rigidbody2D on this GameObject (if any)
            Rigidbody2D currentRb = gameObject.GetComponent<Rigidbody2D>();
            if (currentRb == null)
            {
                // Add a new Rigidbody2D to this GameObject
                currentRb = gameObject.AddComponent<Rigidbody2D>();

                // check that the component was correctly added
                if (currentRb == null)
                {
                    ErrorHandler.Error("Unable to either find a RigidBody on base prefab or add a new one");
                    return;
                }
            }

            // Copy properties from graphics Rigidbody2D to the new Rigidbody2D
            CopyRigidbodyProperties(graphicsRb, ref currentRb);

            // Set new rigid body
            m_Rigidbody = currentRb;

            // Destroy the original Rigidbody2D on the graphics GameObject
            Destroy(graphicsRb);
        }

        // Helper method to copy Rigidbody2D properties
        private void CopyRigidbodyProperties(Rigidbody2D source, ref Rigidbody2D target)
        {
            target.bodyType                 = source.bodyType;
            target.includeLayers            = source.includeLayers;
            target.excludeLayers            = source.excludeLayers;
            target.simulated                = source.simulated;

            if (source.bodyType == RigidbodyType2D.Static)
                return;
            
            target.mass                     = source.mass;
            target.drag                     = source.drag;
            target.angularDrag              = source.angularDrag;
            target.gravityScale             = source.gravityScale;
            target.collisionDetectionMode   = source.collisionDetectionMode;
            target.interpolation            = source.interpolation;
            target.constraints              = source.constraints;
            target.sleepMode                = source.sleepMode;
            target.useAutoMass              = source.useAutoMass;
            target.isKinematic              = source.isKinematic;
        }

        /// <summary>
        /// Swap default Collider with Graphics Collider if it has one
        /// </summary>
        protected virtual void SwapColliders(GameObject graphics)
        {
            // Check if the graphics GameObject has a enabled Collider2D component
            Collider2D graphicsCollider = graphics.GetComponent<Collider2D>();
            if (graphicsCollider == null || !graphicsCollider.enabled)
                return;

            // destroy the collider on the Spell before adding the new one
            Destroy(m_Collider);

            // Get the type of the original collider
            Type colliderType = graphicsCollider.GetType();

            // Add a new collider of the same type to this GameObject
            m_Collider = this.gameObject.AddComponent(colliderType) as Collider2D;

            // Copy properties from the original collider to the new one
            if (m_Collider != null)
            {
                CopyColliderProperties(graphicsCollider, m_Collider);
            }

            // Destroy the original collider on the graphics GameObject
            Destroy(graphicsCollider);
        }

        /// <summary>
        /// Copies properties from one collider to another.
        /// </summary>
        /// <param name="source">The original collider to copy from.</param>
        /// <param name="destination">The new collider to copy to.</param>
        private void CopyColliderProperties(Collider2D source, Collider2D destination)
        {
            if (source == null || destination == null)
                return;

            // General properties
            destination.isTrigger   = source.isTrigger;
            destination.offset      = source.offset;

            // Specific properties for BoxCollider2D
            if (source is BoxCollider2D sourceBoxCollider && destination is BoxCollider2D destinationBoxCollider)
            {
                destinationBoxCollider.size = sourceBoxCollider.size;
            }
            // Specific properties for CircleCollider2D
            else if (source is CircleCollider2D sourceCircleCollider && destination is CircleCollider2D destinationCircleCollider)
            {
                destinationCircleCollider.radius = sourceCircleCollider.radius;
            }
            // Specific properties for CircleCollider2D
            else if (source is CapsuleCollider2D capsuleCollider && destination is CapsuleCollider2D destinationCapsuleCollider)
            {
                destinationCapsuleCollider.size         = capsuleCollider.size;
                destinationCapsuleCollider.offset       = capsuleCollider.offset;
                destinationCapsuleCollider.direction    = capsuleCollider.direction;
            }
            // Specific properties for PolygonCollider2D
            else if (source is PolygonCollider2D sourcePolygonCollider && destination is PolygonCollider2D destinationPolygonCollider)
            {
                destinationPolygonCollider.points = sourcePolygonCollider.points;
            }
            // Specific properties for EdgeCollider2D
            else if (source is EdgeCollider2D sourceEdgeCollider && destination is EdgeCollider2D destinationEdgeCollider)
            {
                destinationEdgeCollider.points = sourceEdgeCollider.points;
            }

            // Add more collider types if necessary
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

        public Transform GetSpellSpawn()
        {
            var spellSpawn = GetBodyPart(EBodyPart.SpellSpawn);
            if (spellSpawn == null)
                return transform;

            return spellSpawn.transform;
        }

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
                if (trackError && ! CharacterLoader.IsSpawn(m_Controller.Character))
                    ErrorHandler.Error("BodyPart " + bodyPart + " not found in character " + m_Controller.Character);
                return null;
            }

            if (m_BodyParts[bodyPart] == null)
            {
                if (trackError && !CharacterLoader.IsSpawn(m_Controller.Character))
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
        public void SpawnSpellGFX(string spellName, ESpellEvent spellEvent, Vector3 targetPos = default, float? forcedDuration = null)
        {
            // FILTER : handle on event before spell spawn (post-spell spawning is handled by the spell itself)
            // except "OnEnd" that can be called when the cast is cancelled
            if (spellEvent >= ESpellEvent.OnSpawn && spellEvent != ESpellEvent.OnEnd)
                return;

            ErrorHandler.Log(spellName + " SpawnSpellGFX : " + spellEvent, ELogTag.SpellGFX);

            var spellData = SpellLoader.GetSpellData(spellName);
            foreach (SPrefabSpawn<ESpellEvent> prefabSpawn in spellData.SpellEventActions)
            {
                if (prefabSpawn.GFXLifetime.StartSpellPart != spellEvent)
                    continue;

                prefabSpawn.Spawn(
                    caster:         m_Controller, 
                    spellData:      spellData,
                    targetPos:      targetPos,
                    forcedDuration: forcedDuration
                );
            }

            return;
        }

        #endregion


        #region Material

        public void ApplyMaterial(Material material, EBodyPart bodyPart = EBodyPart.None)
        {
            if (bodyPart != EBodyPart.None)
            {
                if (m_Materials.ContainsKey(bodyPart.ToString()))
                    m_Materials[bodyPart.ToString()].Add(material);
                return;
            }

            foreach (var spriteRenderer in m_SpriteRenderers)
            {
                // att to list of materials
                m_Materials[spriteRenderer.name].Add(material);
                spriteRenderer.material = material;
            }
        }

        public void RemoveMaterial(Material material, EBodyPart bodyPart = EBodyPart.None)
        {
            if (bodyPart != EBodyPart.None)
            {
                if (m_Materials.ContainsKey(bodyPart.ToString()))
                    m_Materials[bodyPart.ToString()].Add(material);
                return;
            }

            foreach (var spriteRenderer in m_SpriteRenderers)
            {
                // check if body part has material in store
                if (! m_Materials[spriteRenderer.name].Contains(material))
                    continue;

                // remove from list of materials
                m_Materials[spriteRenderer.name].Remove(material);

                // check if is current material
                if (TextHandler.CleanMaterialName(spriteRenderer.sharedMaterial.name) != TextHandler.CleanMaterialName(material.name) )
                    continue;

                // use last available material if any or default
                spriteRenderer.material = m_Materials[spriteRenderer.name].Count > 0 ? m_Materials[spriteRenderer.name].Last() : m_DefaultMaterial;
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
            Color color = INVISIBLE_COLOR;
            if (hidden)
                AddColor(color);
            else
                RemoveColor(color);
        }

        /// <summary>
        /// Hide / show the character colors
        /// </summary>
        /// <param name="hidden"></param>
        public void Hide(bool hidden, string spriteName)
        {
            Color color = new Color(0f, 0f, 0f, 0f);
            if (hidden)
                AddColor(color, spriteName);
            else
                RemoveColor(color, spriteName);
        }

        void AddColor(Color color, string spriteName = "None")
        {
            if (spriteName == EBodyPart.None.ToString())
            {
                foreach (var part in m_Colors.Keys)
                {
                    AddColor(color, part);
                }
                return;
            }

            if (! m_Colors.ContainsKey(spriteName))
            {
                ErrorHandler.Warning("Unable to find any sprite named " + spriteName + " in graphics colors of " + m_Controller.Character);
                return;
            }

            m_Colors[spriteName].Add(color);
            SetColor(color, spriteName);
        }

        void RemoveColor(Color color, string spriteName = "None")
        {
            if (spriteName == EBodyPart.None.ToString())
            {
                foreach (var tempSpriteName in m_Colors.Keys)
                {
                    RemoveColor(color, tempSpriteName);
                }
                return;
            }

            if (!m_Colors.ContainsKey(spriteName))
            {
                ErrorHandler.Warning("Unable to find any sprite named " + spriteName + " in graphics colors of " + m_Controller.Character);
                return;
            }

            m_Colors[spriteName].Remove(color);
            color = m_Colors[spriteName].Count > 0 ? m_Colors[spriteName].Last() : Color.white;
            SetColor(color, spriteName);
        }

        void SetColor(Color color, string spriteName = "None")
        {
            foreach (var spriteRenderer in m_SpriteRenderers)
            {
                if (spriteName != EBodyPart.None.ToString() && spriteRenderer.name != spriteName)
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