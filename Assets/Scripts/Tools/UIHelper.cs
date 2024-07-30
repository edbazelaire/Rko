using Assets.Scripts.Managers.Sound;
using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Tools
{
    public static class UIHelper
    {
        /// <summary>
        /// Remove all childs of a container
        /// </summary>
        /// <param name="gameObject"></param>
        public static void CleanContent(GameObject gameObject, int startAt = 0)
        {
            if (gameObject == null)
            {
                ErrorHandler.Warning("Provided game object is null");
                return;
            }

            int index = 0;
            foreach (Transform child in gameObject.transform)
            {
                if (index++ < startAt)
                    continue;

                GameObject.Destroy(child.gameObject);
            }
        }

        public static void SetUpDropdown<TEnum>(TMP_Dropdown dropdown, Enum defaultValue, Action<TEnum> onDropDownValueChanged, List<TEnum> excludeValues = default)
        {
            // Create a dropdown reward method based on that finds the value linked to the index change and call "onDropDownValueChanged" method
            void OnDropDown(int index)
            {
                SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);

                if (!Enum.TryParse(typeof(TEnum), dropdown.options[index].text, true, out object enumValue))
                {
                    ErrorHandler.Error("Unable to convert " + dropdown.options[index].text + " as " + typeof(TEnum));
                    enumValue = Enum.GetValues(typeof(TEnum)).GetValue(0);
                }

                onDropDownValueChanged.Invoke((TEnum)enumValue);
            }

            // setup option values
            List<string> options = Enum.GetNames(typeof(TEnum)).ToList();
            if (excludeValues != null && excludeValues.Count > 0)
            {
                List<string> excludeNames = excludeValues.Select(e => e.ToString()).ToList();
                options = options.Where(option => !excludeNames.Contains(option)).ToList();
            }
            dropdown.options.Clear();
            dropdown.AddOptions(options);

            // change Lobby game mode on new selection
            dropdown.onValueChanged.AddListener(OnDropDown);

            // set value to last selected value
            dropdown.value = options.IndexOf(defaultValue.ToString());
        }

        public static void SetUpDropdown(TMP_Dropdown dropdown, List<string> values, string defaultValue, Action<string> onDropDownValueChanged)
        {
            // Create a dropdown reward method based on that finds the value linked to the index change and call "onDropDownValueChanged" method
            void OnDropDown(int index)
            {
                SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);
                onDropDownValueChanged.Invoke(values[index]);
            }

            // setup option values
            dropdown.options.Clear();
            dropdown.AddOptions(values);

            // change Lobby game mode on new selection
            dropdown.onValueChanged.AddListener(OnDropDown);

            // set value to last selected value
            dropdown.value = values.IndexOf(defaultValue.ToString());
        }

        /// <summary>
        /// Set a game objects anchors and sizeDelta to 100% match the size of the parent
        /// </summary>
        /// <param name="gameObject"></param>
        public static void SetFullSize(GameObject gameObject)
        {
            // get the RectTransform component of the GameObject
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

            // set the anchors to be at full length
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;

            // remove delta size
            rectTransform.sizeDelta = Vector2.zero;
        }

        /// <summary>
        /// Get Width and Height of a GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void GetSize(GameObject gameObject, out float width, out float height)
        {
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

            width = rectTransform.rect.width;
            height = rectTransform.rect.height;
        }

        /// <summary>
        /// Check if mouse is over position of a gameobject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static bool IsMouseIn(GameObject gameObject)
        {
            // Convert mouse position to local position of the scroller container
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                ErrorHandler.Error("Unable to define if mouse is in game object " +  gameObject.name + " because game object has no RectTransform value");
                return false;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 localMousePos);

            // check if the local mouse position is within the bounds of the game object
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, Camera.main);
        }


        #region Spawning

        public static void SpawnCharacter(ECharacter character, GameObject parent, string layerName = "")
        {
            // clean container before spawning
            CleanContent(parent);

            // get selected character preview
            var characterPreview = CharacterLoader.GetCharacterData(character).InstantiateCharacterPreview(parent);

            // display character preview
            var baseScale = characterPreview.transform.localScale;
            var parentRect = Finder.FindComponent<RectTransform>(parent);
            float scaleFactor = parentRect.rect.height / characterPreview.transform.localScale.y;
            characterPreview.transform.localScale = new Vector3(baseScale.x * scaleFactor, baseScale.y * scaleFactor, 1f);

            // adjust ordering of the character preview to be above canvas
            AdjustLayout(characterPreview, layerName);
        }

        #endregion


        #region Layout Managing

        public static Canvas GetFirstCanvas(Transform child)
        {
            // Traverse up the hierarchy until a Canvas component is found
            while (child != null)
            {
                Canvas canvas = child.GetComponent<Canvas>();
                if (canvas != null)
                {
                    return canvas; // Found a Canvas component
                }

                // Move up to the parent transform
                child = child.parent;
            }

            // No Canvas component found in the hierarchy
            ErrorHandler.Error("Unable to find canvas in " + child.name);
            return null; 
        }

        /// <summary>
        /// Adjust layout ordering to put gameObject above parent
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="parent"></param>
        public static void AdjustLayout(GameObject gameObject, string layerName = "")
        {
            Canvas canvas = GetFirstCanvas(gameObject.transform);

            // Get all SpriteRenderer components attached to this GameObject and its children
            SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();

            // Set sorting layer and order in layer for each sprite renderer
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                renderer.sortingLayerName   = layerName == "" ? canvas.sortingLayerName : layerName;
                renderer.sortingOrder       += canvas.sortingOrder;
            }
        }

        #endregion
    }
}