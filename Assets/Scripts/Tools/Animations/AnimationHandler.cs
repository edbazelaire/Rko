using Assets.Scripts.Managers.Sound;
using MyBox;
using System.Collections.Generic;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Tools
{
    public class AnimationHandler
    {
        #region Members
        
        static AnimationHandler s_Instance;

        /// <summary>
        /// Dict containing 
        /// </summary>
        Dictionary<string, List<OvAnimation>> m_AnimationsPlaying;

        #endregion


        #region Init & Instance

        public static AnimationHandler Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new AnimationHandler();
                    s_Instance.m_AnimationsPlaying = new Dictionary<string, List<OvAnimation>>();
                }

                return s_Instance;
            }
        }

        #endregion


        #region Animations Management

        /// <summary>
        /// Add animation to list and group of animations
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="id"></param>
        public static void AddAnimation(OvAnimation animation, string id = "")
        {
            if (id == null || id == "")
                id = GenerateRandomId();

            if (!Instance.m_AnimationsPlaying.ContainsKey(id))
                Instance.m_AnimationsPlaying[id] = new List<OvAnimation>();

            Instance.m_AnimationsPlaying[id].Add(animation);
        }

        /// <summary>
        /// End a group or a specific animation
        /// </summary>
        /// <param name="id"></param>
        /// <param name="animationName"></param>
        public static void EndAnimation(string id, string animationName = "") 
        { 
            if (! IsPlaying(id, animationName))
                return;

            // FIND ANIMATIONS TO END
            List<int> indexToRemove = new List<int>();
            for (int i = 0; i < Instance.m_AnimationsPlaying[id].Count; i++)
            {
                OvAnimation animation = Instance.m_AnimationsPlaying[id][i];

                // can happen if has been destroy from another source
                if (animation.IsDestroyed())
                {
                    indexToRemove.Add(i);
                    continue;
                }

                if (animationName != "" && animationName != animation.Name)
                    continue;

                animation.End();
                indexToRemove.Add(i);
            }

            // REMOVE ANIMATIONS
            // reverse order of the list to start from highest index (so the removals wont impact the following indexes)
            indexToRemove.Reverse();
            foreach (int i in indexToRemove)
            {
                Instance.m_AnimationsPlaying[id].RemoveAt(i);
            }

            // REMOVE GROUP OF ANIMATIONS 
            // remove key from dict
            if (animationName == "" || Instance.m_AnimationsPlaying[id].Count == 0)
                Instance.m_AnimationsPlaying.Remove(id);
        }

        /// <summary>
        /// Check if provided animation is currently playing
        /// </summary>
        /// <param name="id"></param>
        /// <param name="animationName"></param>
        /// <returns></returns>
        public static bool IsPlaying(string id, string animationName = "")
        {
            if (id == null || id == "")
                return false;

            if (!Instance.m_AnimationsPlaying.ContainsKey(id))
                return false;

            if (animationName == "")
                return true;

            var animation = FindAnimation(animationName, id);
            if (animation == null)
                return false;

            return ! animation.IsOver;
        }

        public static OvAnimation FindAnimation(string animationName, string id)
        {
            if (id == null || id == "")
                return null;

            foreach (OvAnimation anim in Instance.m_AnimationsPlaying[id])
                if (anim.Name == animationName)
                    return anim;

            return null;    
        }


        #endregion


        #region Ray Casts

        public static void AddRaycast(GameObject parent, string id = "", float duration = -1f, Vector3 rotation = default, float size = 1f, Color color = default)
        {
            if (color == default)
                color = new Color(1f, 1f, 1f, 0.3f);

            // instantiate raycast object as first child of provided parent
            var raycast = GameObject.Instantiate(AssetLoader.LoadBackgroundAnimation("RayCast"), parent.transform);
            raycast.transform.SetAsFirstSibling();

            // set scale 
            raycast.transform.localScale = Vector3.one * size;

            // set raycast image & color
            var image = raycast.GetComponent<Image>();
            image.preserveAspect = true;
            image.sprite = AssetLoader.Load<Sprite>("Rays1", AssetLoader.c_RaysPath);
            image.color = color;

            // set rotation of the image
            var rotateAnim = raycast.GetComponent<RotateAnimation>();
            if (rotation != default)
                rotateAnim.Initialize(id, duration, rotation);

            // destroy object at the end of animation
            rotateAnim.OnAnimationEnded += () => GameObject.Destroy(raycast);
        }

        #endregion


        #region Error Animations

        public static void ClickErrorAnimation(GameObject button)
        {
            if (button.HasComponent<ShakeAnimation>())
            {
                return;
            }

            var shakeAnimation = button.AddComponent<ShakeAnimation>();
            SoundFXManager.PlayOnce(SoundFXManager.UpgradeFailSoundFX);
            shakeAnimation.Initialize("", 0.5f);
        }

        #endregion


        #region ID Management

        public static string GenerateRandomId()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        #endregion
    }
}