using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Sound;
using Assets.Scripts.Menu.MainMenu.MainTab.Chests;
using Data;
using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.AspectRatioFitter;

namespace Tools
{
    public static class UIHelper
    {

        #region Helpers

        /// <summary>
        /// Remove all childs of a container
        /// </summary>
        /// <param name="gameObject"></param>
        public static void CleanContent(Transform transform, int startAt = 0)
        {
            if (transform == null)
            {
                ErrorHandler.Warning("Provided transform is null");
                return;
            }

            int index = 0;
            foreach (Transform child in transform)
            {
                if (index++ < startAt)
                    continue;

                GameObject.Destroy(child.gameObject);
            }
        }

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

            CleanContent(gameObject.transform);
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
                ErrorHandler.Error("Unable to define if mouse is in game object " + gameObject.name + " because game object has no RectTransform value");
                return false;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 localMousePos);

            // check if the local mouse position is within the bounds of the game object
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, Camera.main);
        }

        public static string FindContext(Transform transform)
        {
            string context = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                context = transform.name + "." + context;
            }

            return context;
        }

        public static void DisplayIconCount(int count, Sprite sprite, Transform container, float? ratio = null)
        {
            UIHelper.CleanContent(container);

            var template = new GameObject();
            var image = template.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            if (ratio.HasValue)
            {
                var arf = template.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                arf.aspectRatio = ratio.Value;
            }

            for (int i = 0; i < count; i++)
            {
                GameObject.Instantiate(template, container);
            }

            // destroy original template
            GameObject.Destroy(template);
        }

        #endregion


        #region Size & Ratio

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
        /// Get Width and Height of the current Scren
        /// </summary>
        /// <returns></returns>
        public static float ScreenRatio => (float)Screen.width / Screen.height;
       
        public static EScreenAspect ScreenAspect
        {
            get
            {
                if (ScreenRatio <= 1.4f)
                    return EScreenAspect.Square;

                if (ScreenRatio >= 2.5f)
                    return EScreenAspect.Large;

                return EScreenAspect.Normal;
            }
        }

        /// <summary>
        /// Get ratio Width vs Hight of the provided game obejct
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static float GetSizeRatio(GameObject gameObject)
        {
            GetSize(gameObject, out float width, out float height);
            return width / height;
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

        #endregion


        #region DropDown

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

        public static void SetUpMultiDropdown(TMP_Dropdown dropdown, List<string> values, string defaultValue, Action<List<string>> onDropDownValueChanged)
        {
            // Create a dropdown reward method based on that finds the value linked to the index change and call "onDropDownValueChanged" method
            void OnDropDown(int index)
            {
                SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);
                onDropDownValueChanged.Invoke(DecodeBytesValue(index, values));
            }

            // setup option values
            dropdown.options.Clear();
            dropdown.AddOptions(values);

            // change Lobby game mode on new selection
            dropdown.onValueChanged.AddListener(OnDropDown);

            // set value to last selected value
            if (defaultValue != "")
                dropdown.value = values.IndexOf(defaultValue.ToString());
        }

        public static List<string> DecodeBytesValue(int value, List<string> values)
        {
            List<string> selectedValues = new List<string>();
            for (int i = 0; i < values.Count; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    selectedValues.Add(values[i]);
                }
            }
            return selectedValues;
        }

        #endregion


        #region Spawning

        public static void SpawnCharacter(string character, GameObject parent, string layerName = "", Vector2 padding = default)
        {
            // clean container before spawning
            CleanContent(parent);

            // get selected character preview
            var characterPreview = CharacterLoader.GetCharacterData(character, destroy: true).InstantiateCharacterPreview(parent);

            // display character preview
            AdjustScale(ref characterPreview, parent);
            
            // remove offset
            var characterContainer = Finder.Find(characterPreview, "CharacterContainer", false);
            if (characterContainer == null)
            {
                characterContainer = characterPreview;
                var rigidBody = characterContainer.GetComponent<Rigidbody2D>();
                if (rigidBody != null)
                    rigidBody.simulated = false;
                padding.y -= characterPreview.transform.localScale.y / 2;
            }

            var basePos = characterContainer.transform.localPosition;
            basePos.x = padding.x;
            basePos.y += padding.y;
            characterContainer.transform.localPosition = basePos;

            // adjust ordering of the character preview to be above canvas
            AdjustLayout(ref characterPreview, characterContainer.transform, layerName);
        }

        public static GameObject SpawnItem(GameObject go, GameObject parent, bool cleanParent = true, bool adjustLayout = true, bool adjustScale = true)
        {
            // Clean parent
            if (cleanParent)
                CleanContent(parent);

            // Instantiate
            go = GameObject.Instantiate(go, parent.transform);

            if (adjustLayout)
                AdjustLayout(ref go, parent.transform);

            if (adjustScale)
                AdjustScale(ref go, parent);

            return go;
        }

        #endregion


        #region Icons Display

        public static GameObject AddSpawnIconDisplayer(CharacterData characterData, Transform parent, bool interraclable = true)
        {
            var template = GameObject.Instantiate(AssetLoader.Load<GameObject>("IconDisplayer", AssetLoader.c_TemplatesUIPath), parent);
            Finder.FindComponent<Image>(template, "Icon").sprite = AssetLoader.LoadCharacterIcon(characterData.Name);
            Finder.FindComponent<AspectRatioFitter>(template).aspectMode = AspectMode.HeightControlsWidth;

            if (interraclable)
            {
                Button button = template.AddComponent<Button>();
                button.onClick.AddListener(() => ScreenManager.SetPopUp(EPopUpState.BossInfoPopUp, characterData.Name, characterData.Level));
            }

            return template;
        }

        #endregion


        #region Size Management

        public static void AdjustScale(ref GameObject go, GameObject parent)
        {
            // display character preview
            var baseScale = go.transform.localScale;
            var parentRect = Finder.FindComponent<RectTransform>(parent);
            float scaleFactor = Mathf.Min(parentRect.rect.height / baseScale.y, parentRect.rect.width / baseScale.x);
            go.transform.localScale = new Vector3(baseScale.x * scaleFactor, baseScale.y * scaleFactor, baseScale.y * scaleFactor);
        }


        #endregion


        #region Layout Managing

        public static Canvas GetFirstCanvas(Transform child)
        {
            var parent = child;

            // Traverse up the hierarchy until a Canvas component is found
            while (parent != null)
            {
                Canvas canvas = parent.GetComponent<Canvas>();
                if (canvas != null)
                {
                    return canvas; // Found a Canvas component
                }

                // Move up to the parent transform
                parent = parent.parent;
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
        public static void AdjustLayout(ref GameObject gameObject, Transform parent = null, string layerName = "")
        {
            Canvas canvas = GetFirstCanvas(parent == null ? gameObject.transform : parent);
            if (canvas == null)
                return;

            // Get all SpriteRenderer components attached to this GameObject and its children
            SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();

            // Set sorting layer and order in layer for each sprite renderer
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                renderer.sortingLayerName   = layerName == "" ? canvas.sortingLayerName : layerName;
                renderer.sortingOrder       += canvas.sortingOrder;
            }

            // Get all ParticleSystem components in the hierarchy of the Chest object
            var particleSystems = Finder.FindComponents<ParticleSystem>(gameObject, throwError: false);

            // Adjust the rendering order of ParticleSystem components
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                Renderer particleRenderer = particleSystem.GetComponent<Renderer>();
                if (particleRenderer != null)
                {
                    particleRenderer.sortingLayerName = canvas.sortingLayerName;
                    particleRenderer.sortingOrder += canvas.sortingOrder + 1; // Render above the Canvas
                }
            }
        }

        #endregion
    }
}