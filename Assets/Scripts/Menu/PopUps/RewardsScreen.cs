using Assets.Scripts.Managers.Sound;
using Assets.Scripts.Menu.Common.Buttons.TemplateItemButtons;
using Assets.Scripts.Menu.MainMenu.MainTab.Chests;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Inventory;
using Menu.Common;
using Menu.Common.Buttons;
using Menu.Common.Displayers;
using Menu.Common.Rewards;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class RewardsScreen : OverlayScreen
    {
        #region Members

        const string        c_ChestContainer            = "ChestContainer";
        const string        c_RewardDisplayContainer    = "RewardDisplayContainer";

        GameObject          m_PresentationContainer;
        TMP_Text            m_PresentationTitle;
        RewardsDisplayer    m_RewardsDisplayer;
        GameObject          m_ChestContainer;
        ChestUI             m_ChestUI;
        PowerOrbContainer   m_PowerOrbContainer;
        GameObject          m_RewardDisplayContainer;
        GameObject          m_RewardIconSection;
        GameObject          m_RewardInfosSection;
        GameObject          m_RewardInfosContent;
        TMP_Text            m_RewardTitle;
        CollectionFillBar   m_CollectionFillBar;
        TMP_Text            m_CollectionQty;

        // SPECIFIC DATA
        SRewardsData        m_RewardsData;
        string              m_Context                   = "";
        Action              m_OnRewardCollected         = null;
        string              m_Title                     = null;

        bool                m_CanSkip                   = false;
        bool                m_Skip                      = false;
        int                 m_Depth                     = 0;
        GameObject          m_CurrentTemplateItem       = null;
        ChestRewardData     m_CurrentChestRewardData    = null;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            // setup game objects
            m_ChestContainer            = Finder.Find(gameObject, c_ChestContainer);

            // Presentation
            m_PresentationContainer     = Finder.Find(gameObject, "PresentationContainer");
            m_PresentationTitle         = Finder.FindComponent<TMP_Text>(m_PresentationContainer, "PresentationTitle");
            m_RewardsDisplayer          = Finder.FindComponent<RewardsDisplayer>(m_PresentationContainer, "RewardsDisplayer");

            // Rewars Template
            m_RewardDisplayContainer    = Finder.Find(gameObject, c_RewardDisplayContainer);
            m_RewardIconSection         = Finder.Find(m_RewardDisplayContainer, "RewardIconSection");

            // Infos Section
            m_RewardInfosSection        = Finder.Find(gameObject, "RewardInfosSection");
            m_RewardInfosContent        = Finder.Find(m_RewardInfosSection, "Content");
            m_RewardTitle               = Finder.FindComponent<TMP_Text>(m_RewardInfosSection, "RewardTitle");
            m_CollectionFillBar         = Finder.FindComponent<CollectionFillBar>(m_RewardInfosSection, "CollectionFillbar");
            m_CollectionQty             = Finder.FindComponent<TMP_Text>(m_RewardInfosSection, "CollectionQty");
        }

        /// <summary>
        /// Initialize data to collect
        /// </summary>
        /// <param name="rewardsData">          every rewards to collect (golds, chests, achievements, ...) </param>
        /// <param name="context">              [ANALYTICS] context from where this rewards came from       </param>
        /// <param name="onRewardCollected">    Action() to fire after the rewards are collected            </param>
        public void Initialize(SRewardsData rewardsData, string context, Action onRewardCollected = null, string title = null)
        {
            m_Skip                      = false;
            m_RewardsData               = rewardsData;
            m_Context                   = context;
            m_CurrentChestRewardData    = null;
            m_OnRewardCollected         = onRewardCollected;
            m_Title                     = title;

            base.Initialize();
        }

        /// <summary>
        /// Re-adjust size of the elements depending on current AspectRatio
        /// </summary>
        protected override void AdjustAspectRatio()
        {
            base.AdjustAspectRatio();

            if (UIHelper.ScreenAspect == EScreenAspect.Normal)
                return;

            if (UIHelper.ScreenAspect == EScreenAspect.Large)
            {
                // adjust max layout width of the Icon
                var layoutIcon = Finder.FindComponent<LayoutElement>(m_RewardIconSection);
                if (layoutIcon != null)
                    layoutIcon.preferredWidth = 700;

                // adjust vertical alignment of the Icon
                var verticalLayoutGroupIcon = Finder.FindComponent<VerticalLayoutGroup>(m_RewardIconSection);
                if (verticalLayoutGroupIcon != null)
                {
                    verticalLayoutGroupIcon.padding.left = 50;
                    verticalLayoutGroupIcon.padding.right = 50;
                    verticalLayoutGroupIcon.padding.top = 50;
                    verticalLayoutGroupIcon.padding.bottom = 50;
                }

                // adjust max layout width of the Icon
                var layoutInfo = Finder.FindComponent<LayoutElement>(m_RewardInfosSection);
                if (layoutIcon != null)
                    layoutInfo.preferredWidth = 600;
            }
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            if (m_Title != null) 
            {
                m_PresentationTitle.text = m_Title;
                m_RewardsDisplayer.Initialize(m_RewardsData);
            }

            // hide before displaying rewards
            m_PresentationContainer.SetActive(false);
            m_RewardDisplayContainer.SetActive(false);
            m_ChestContainer.SetActive(false);
        }

        protected override void OnInitializationCompleted()
        {
            base.OnInitializationCompleted();
            StartCoroutine(StartDisplay());

            SoundFXManager.MusicAudioSource.volume /= 2;
        }

        protected override void OnExit()
        {
            m_OnRewardCollected?.Invoke();    

            base.OnExit();

            SoundFXManager.MusicAudioSource.volume *= 2;
        }

        protected override IEnumerable ExitAnimation()
        {
            var fadeOut = gameObject.AddComponent<Fade>();
            fadeOut.Initialize("", duration: 0.2f, endOpacity: 0);

            yield return new WaitUntil(() => fadeOut.IsOver);
        }

        #endregion


        #region Update

        protected override void Update()
        {
            base.Update();

            if (m_CanSkip && Input.touchCount > 0 || Input.GetMouseButtonDown(0))
            {
                m_Skip = true;
            }
        }

        IEnumerator WaitForCoroutineOrSkip(IEnumerator coroutine)
        {
            bool coroutineFinished = false;

            // Wrap the coroutine to set a flag when it finishes
            IEnumerator WrappedCoroutine()
            {
                yield return coroutine;
                coroutineFinished = true;
            }

            // Start the wrapped coroutine
            var currentCoroutine = StartCoroutine(WrappedCoroutine());

            // Wait until either the coroutine is done or m_Skip is true
            yield return new WaitUntil(() => coroutineFinished || m_Skip);

            StopCoroutine(currentCoroutine);
        }

        IEnumerator WaitAnimationOrSkip(OvAnimation animation)
        {
            yield return new WaitUntil(() => animation.IsOver || m_Skip);

            if (! animation.IsOver)
            {
                animation.End();
            }
        }

        #endregion


        #region Rewards

        IEnumerator StartDisplay()
        {
            yield return DisplayPresentation();
            yield return DisplayRewards(m_RewardsData.Rewards);

            Exit();
        }

        IEnumerator DisplayPresentation()
        {
            if (m_Title == null)
                yield break;

            m_PresentationContainer.SetActive(true);

            yield return new WaitUntil(() => m_Skip);

            m_Skip = false;
            m_PresentationContainer.SetActive(false);
        }

        IEnumerator DisplayRewards(List<SReward> rewards, bool setGoldsAsBonus = false)
        {
            // depth of the current coroutine 
            int myDepth = m_Depth;

            for (int i = 0; i < rewards.Count; i++)
            {
                ErrorHandler.Log("Reward : " + (i+1) + "/" + rewards.Count, ELogTag.Rewards);

                // get the reward
                SReward reward = rewards[i];

                // check if reward should be named "bonus"
                bool isBonus = setGoldsAsBonus
                    && i == rewards.Count - 1
                    && reward.RewardName == "Golds";

                yield return DisplayReward(reward, isBonus: isBonus);

                // Wait for the player to touch the screen before displaying the next reward
                yield return new WaitUntil(() => myDepth == m_Depth && m_Skip);
                yield return null;                      // Ensure the coroutine yields at least once to avoid blocking the main thread
            }

            // list of rewards done beeing displayed, reduce level of depth
            m_Depth--;
        }

        IEnumerator DisplayReward(SReward reward, bool isBonus = false)
        {
            ErrorHandler.Log("DisplayReward : " + reward.RewardName, ELogTag.Rewards);

            // reset skip before next reward
            m_Skip = false;

            if (reward.RewardType == typeof(EChest) && Enum.TryParse(reward.RewardName, out EChest chestType))
            {
                yield return DisplayChestReward(chestType, reward.Qty);
            } 
            else if (reward.RewardType == typeof(EPowerOrb))
            {
                yield return DisplayOrbReward(new SPowerOrb(reward.RewardName));
            } 
            else if (reward.RewardType == typeof(ECurrency) && Enum.TryParse(reward.RewardName, out ECurrency currency))
            {
                yield return DisplayCurrencyReward(currency, reward.Qty, isBonus);
            } 
            else if (ProfileCloudData.TryGetType(reward.RewardType, out EAchievementReward arType, false))
            {
                yield return DisplayAchievementReward(arType, reward.RewardName);
            }
            else if (reward.RewardType == typeof(EBoost) && Enum.TryParse(reward.RewardName, out EBoost boost))
            {
                yield return DisplayBoostReward(boost, reward.Qty);
            }
            else
            {
                var collectable = CollectablesManagementData.Cast(reward.RewardName, reward.RewardType);
                if (collectable == null)
                {
                    ErrorHandler.Error("Unable to display reward " + reward.RewardName + " with type " + reward.RewardType);
                    yield break;
                }

                yield return DisplayCollectableReward(CollectablesManagementData.Cast(reward.RewardName, reward.RewardType), reward.Qty);
            }
        }

        #endregion


        #region Chests

        IEnumerator DisplayChestReward(EChest chestType, int qty)
        {
            ErrorHandler.Log("DisplayChestReward() : ", ELogTag.Rewards);
            ErrorHandler.Log("      + chestType : " + chestType, ELogTag.Rewards);
            ErrorHandler.Log("      + qty : " + qty, ELogTag.Rewards);

            m_Skip = false;

            if (qty > 1)
                ErrorHandler.Warning("Multiple Chests not handled yet");

            // displaying a list of rewards add a new depth in the coroutine management
            m_Depth++;

            // deactivate rewards display container
            m_RewardDisplayContainer.SetActive(false);
            // activate chest container
            m_ChestContainer.SetActive(true);

            // clean chest container
            UIHelper.CleanContent(m_ChestContainer);

            // set rewards of the chest as current chest rewards
            m_CurrentChestRewardData = ItemLoader.GetChestRewardData(chestType);

            // instantiate chest prefab
            m_ChestUI = m_CurrentChestRewardData.Instantiate(m_ChestContainer);
            m_ChestUI.ActivateIdle(true, true);

            // wait until touch to display reward
            yield return new WaitUntil(() => m_Skip);

            m_Skip = false;

            yield return WaitForCoroutineOrSkip(OpenChest());

            m_Skip = false;

            yield return DisplayRewards(m_CurrentChestRewardData.GenerateRewards());
        }

        IEnumerator OpenChest()
        {
            yield return m_ChestUI.PlayOpenAnimation();
        }

        #endregion


        #region Power Orb

        IEnumerator DisplayOrbReward(SPowerOrb powerOrb)
        {
            m_Skip = false;

            // displaying a list of rewards add a new depth in the coroutine management
            m_Depth++;

            // deactivate rewards display container
            m_RewardDisplayContainer.SetActive(false);
            // activate chest container
            m_ChestContainer.SetActive(true);

            // clean chest container
            UIHelper.CleanContent(m_ChestContainer);

            // instantiate chest prefab
            m_PowerOrbContainer = Instantiate(AssetLoader.LoadPowerOrbContainer(), m_ChestContainer.transform);
            m_PowerOrbContainer.Initialize(powerOrb);
            m_PowerOrbContainer.transform.localScale *= 3;

            // wait until touch to display reward
            yield return new WaitUntil(() => m_Skip);

            m_Skip = false;

            // wait until touch to display reward
            yield return TryUpgradeRarety();

            m_Skip = false;

            yield return WaitForCoroutineOrSkip(OpenOrb());

            m_Skip = false;

            yield return DisplayRewards(m_PowerOrbContainer.PowerOrbData.GenerateRewards(), setGoldsAsBonus: true);
        }

        IEnumerator TryUpgradeRarety()
        {
            for(int i = 0; i < 5; i++)
            {
                m_PowerOrbContainer.PowerOrbUI.PlayOnClickAnimation();

                if (! m_PowerOrbContainer.PowerOrbData.TryUpgradeRarety())
                    yield return WaitForCoroutineOrSkip(m_PowerOrbContainer.PowerOrbUI.UpgradeFailedAnimation());
                else
                    yield return WaitForCoroutineOrSkip(m_PowerOrbContainer.UpgradeSuccessAnimation());

                yield return new WaitUntil(() => m_Skip);

                m_Skip = false;
            }
        }

        IEnumerator OpenOrb()
        {
            yield return m_PowerOrbContainer.PowerOrbUI.PlayOpenAnimation();
        }

        #endregion


        #region Single 

        IEnumerator DisplayCurrencyReward(ECurrency currency, int qty, bool isBonus = false)
        {
            ErrorHandler.Log("DisplayCurrencyReward() : ", ELogTag.Rewards);
            ErrorHandler.Log("      + "+ (isBonus ? "(bonus) " : "") + "currency : " + currency, ELogTag.Rewards);

            m_Skip = false;

            // play sound effect
            SoundFXManager.PlayOnce(SoundFXManager.GoldsCollectedSoundFX);

            // activate rewards display container
            m_RewardDisplayContainer.SetActive(true);
            m_RewardInfosSection.SetActive(true);
            // deactivate chest container
            m_ChestContainer.SetActive(false);

            // set title
            string title = (isBonus ? "(Bonus) " : "") + currency.ToString();

            // Xp -> converted to TotalXp
            if (currency == ECurrency.Xp)
                currency = ECurrency.TotalXp;

            int currentlyOwnValue = InventoryManager.GetCurrency(currency);
            int maxValue = currency == ECurrency.Xp || currency == ECurrency.TotalXp ? CollectablesManagementData.GetCurrentAccountLevelData().RequiredXp : currentlyOwnValue + qty;

            // init default template and clean previous content
            UIHelper.CleanContent(m_RewardIconSection);

            TemplateCurrencyItem template = Instantiate(AssetLoader.LoadTemplateItem("CurrencyItem"), m_RewardIconSection.transform).GetComponent<TemplateCurrencyItem>();
            template.Initialize(currency, qty);

            // remove button and collection fillbar from item
            template.AsIconOnly();

            m_CollectionQty.text = "+ " + qty.ToString();
            m_RewardTitle.text = title;

            // -- setup collection fill bar
            m_CollectionFillBar.Initialize(currentlyOwnValue, maxValue);
            yield return WaitForCoroutineOrSkip(m_CollectionFillBar.CollectionAnimationCoroutine(qty));

            // make sure that audio source is destroyed (in case of skip)
            if (! m_CollectionFillBar.AudioSource.IsDestroyed())
                Destroy(m_CollectionFillBar.AudioSource.gameObject);    

            // add reward to collection of rewards
            InventoryManager.UpdateCurrency(currency == ECurrency.TotalXp ? ECurrency.Xp : currency, qty, m_Context);
        }

        IEnumerator DisplayCollectableReward(Enum collectable, int qty)
        {
            SoundFXManager.PlayOnce(SoundFXManager.RewardCollectedSoundFX);

            // activate rewards display container
            m_RewardDisplayContainer.SetActive(true);
            // deactivate chest containers
            m_ChestContainer.SetActive(false);

            // clean content before next display
            UIHelper.CleanContent(m_RewardIconSection);

            // if is character but has already been unlocked
            bool isConverted = false;
            if (collectable.GetType() == typeof(ECharacter) && InventoryCloudData.Instance.GetCollectable(collectable).Level > 0)
            {
                qty = CollectablesManagementData.ConvertCharacterToXp((ECharacter)collectable);
                isConverted = true;
            }

            // setup ui of the new collectable
            SetUpTemplateItem(collectable, qty);
            SetUpRewardInfos(collectable, qty, false);

            // skip one frame to be sure that the layout components are adjusted properly
            yield return null;

            // IF CONVERTED - play the conversion animation and display the new reward
            if (isConverted)
            {
                // -- play collectable animation with Collectable beeing replaced with XP
                yield return PlayRewardAnimation(ECurrency.Xp, qty);
            } else
            {
                // -- play collectable animation
                yield return PlayRewardAnimation();
            }

            // -- play collection fill bar animation
            m_Skip = false;
            yield return WaitForCoroutineOrSkip(m_CollectionFillBar.CollectionAnimationCoroutine(qty));

            // make sure that audio source is destroyed(in case of skip)
            if (! m_CollectionFillBar.AudioSource.IsDestroyed())
                Destroy(m_CollectionFillBar.AudioSource.gameObject);    

            // add reward to collection of rewards
            InventoryManager.AddCollectable(collectable, qty);
        }

        IEnumerator DisplayAchievementReward(EAchievementReward arType, string value)
        {
            ErrorHandler.Log("DisplayAchievementReward : ", ELogTag.Rewards);
            ErrorHandler.Log("      + EAchievementReward : " + arType, ELogTag.Rewards);
            ErrorHandler.Log("      + value : " + value, ELogTag.Rewards);

            // play sound effect
            SoundFXManager.PlayOnce(SoundFXManager.AchievementRewardCollectedSoundFX);

            // activate rewards display container
            m_RewardDisplayContainer.SetActive(true);
            m_RewardInfosSection.SetActive(false);
            // deactivate chest containers
            m_ChestContainer.SetActive(false);

            // clean content before next display
            UIHelper.CleanContent(m_RewardIconSection);

            // setup ui of the new template
            SetUpAchievementRewardTemplate(arType, value);
            if (m_CurrentTemplateItem == null)
                yield break;

            yield return PlayAchievementRewardAnimation();

            AnimationHandler.AddRaycast(m_RewardIconSection, size: 2f, color: new Color(1f, 1f, 1f, 0.3f));

            // add reward to collection of rewards
            ProfileCloudData.AddAchievementReward(arType, value);

            // wait for click to display next
            yield return new WaitUntil(() => m_Skip);
        }

        IEnumerator DisplayBoostReward(EBoost boost, int duration)
        {
            ErrorHandler.Log("DisplayBoostReward : ", ELogTag.Rewards);
            ErrorHandler.Log("      + EBoost : " + boost,   ELogTag.Rewards);
            ErrorHandler.Log("      + duration : " + duration,    ELogTag.Rewards);

            // play sound effect
            SoundFXManager.PlayOnce(SoundFXManager.AchievementRewardCollectedSoundFX);

            // activate rewards display container
            m_RewardDisplayContainer.SetActive(true);
            m_RewardInfosSection.SetActive(false);
            // deactivate chest containers
            m_ChestContainer.SetActive(false);

            // clean content before next display
            UIHelper.CleanContent(m_RewardIconSection);

            // setup ui of the new template
            SetUpBoostRewardTemplate(boost);
            if (m_CurrentTemplateItem == null)
                yield break;

            yield return PlayAchievementRewardAnimation();

            AnimationHandler.AddRaycast(m_RewardIconSection, size: 2f, color: new Color(1f, 1f, 1f, 0.3f));

            // add reward to collection of rewards
            TimeCloudData.AddBoost(boost, duration);

            // wait for click to display next
            yield return new WaitUntil(() => m_Skip);
        }

        void SetUpTemplateItem(Enum collectable, int qty)
        {
            m_CurrentTemplateItem = Instantiate(AssetLoader.LoadTemplateItem(collectable), m_RewardIconSection.transform);
            var template = m_CurrentTemplateItem.GetComponent<TemplateCollectableItemUI>();
            template.Initialize(collectable, true);
            template.SetMysteryIcon(true);
            template.ForceState(EButtonState.Normal);
        }

        void SetUpAchievementRewardTemplate(EAchievementReward ar, string value)
        {
            var template = Instantiate(AssetLoader.LoadAchievementRewardTemplate(ar), m_RewardIconSection.transform);
            if (template == null)
                return;

            m_CurrentTemplateItem = template.gameObject;
            template.Initialize(value, ar);
        }

        void SetUpRewardInfos(Enum collectable, int qty, bool showContent = true)
        {
            // get data from cloud manager
            SCollectableCloudData cloudData = InventoryCloudData.Instance.GetCollectable(collectable);

            m_RewardTitle.text = TextLocalizer.SplitCamelCase(collectable.ToString());
            m_CollectionQty.text = "+ " + qty.ToString();

            // -- setup collection fill bar
            m_CollectionFillBar.Initialize(cloudData.GetQty(), CollectablesManagementData.GetLevelData(collectable, cloudData.Level).RequiredQty);

            // set the content hidden or not
            DisplayRewardInfosContent(showContent);
        }

        #endregion


        #region Reward Animation

        /// <summary>
        /// Play animation of a new reward
        /// </summary>
        /// <returns></returns>
        IEnumerator PlayRewardAnimation(ECurrency? replaceWithCurrency = null, int? qty = null)
        {
            // deactivate infos content && remove layout of TemplateIcon
            DisplayRewardInfosContent(false);
            var layoutElement = m_RewardIconSection.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // animation of removing the mystery icon (if any)
            yield return RemoveMysteryIcon();

            if (replaceWithCurrency.HasValue)
                yield return ReplaceWithCurrency(replaceWithCurrency.Value, qty.Value);

            // move on the side
            var move = m_RewardIconSection.AddComponent<MoveAnimation>();
            move.Initialize(duration: 0.2f, endPos: new Vector3(transform.position.x - 2, transform.position.y, transform.position.z));

            // wait until animation done playing or player skips it
            yield return WaitAnimationOrSkip(move);

            // re-activate infos content && set back layout of TemplateIcon
            DisplayRewardInfosContent(true);
            layoutElement.ignoreLayout = false;

            // fade-in animation on the Informations content
            var fadeIn = m_RewardInfosSection.AddComponent<Fade>();
            fadeIn.Initialize(duration: 0.2f, startOpacity: 0f, startScale: 0.8f);

            yield return WaitAnimationOrSkip(fadeIn);
        }

        IEnumerator RemoveMysteryIcon()
        {
            if (m_CurrentTemplateItem == null || m_CurrentTemplateItem.IsDestroyed())
            {
                ErrorHandler.Warning("Calling RemoveMysteryIcon() on item that no longer exists");
                yield break;
            }

            var componentUI = m_CurrentTemplateItem.GetComponent<TemplateCollectableItemUI>();
            if (componentUI == null)
                yield break;

            var rotation = m_CurrentTemplateItem.AddComponent<RotateAnimation>();
            rotation.Initialize(duration: 0.7f, rotation: new Vector3(0, 720, 0));

            yield return WaitAnimationOrSkip(rotation);

            if (componentUI.CollectableCloudData.Level == 0)
                yield return UnlockAnimation();
            
            componentUI.SetMysteryIcon(false);

            var raretyColor = CollectablesManagementData.GetRaretyData(componentUI.CollectableCloudData.GetCollectable()).Color;
            raretyColor.a = 0.6f;
            AnimationHandler.AddRaycast(m_RewardIconSection, color: raretyColor);

            if (! m_Skip)
                yield return new WaitForSeconds(0.45f);
        }

        IEnumerator UnlockAnimation()
        {
            if (m_CurrentTemplateItem == null || m_CurrentTemplateItem.IsDestroyed())
            {
                ErrorHandler.Warning("Calling UnlockAnimation() on item that no longer exists");
                yield break;
            }

        }

        IEnumerator ReplaceWithCurrency(ECurrency currency, int qty)
        {
            m_Skip = false;
            var previousTemplate = m_CurrentTemplateItem;

            // init currency template
            TemplateCurrencyItem template = Instantiate(AssetLoader.LoadTemplateItem("CurrencyItem"), m_RewardIconSection.transform).GetComponent<TemplateCurrencyItem>();
            // -- ignore layout to not mess with Layout
            var layoutElement = template.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            // -- init new template and save as current template
            template.Initialize(currency, qty);
            m_CurrentTemplateItem = template.gameObject;
            // -- Put new template behind the old one
            template.transform.SetSiblingIndex(previousTemplate.transform.GetSiblingIndex());
            // -- setup size and pos
            var newRectT = template.GetComponent<RectTransform>();
            var oldRectT = previousTemplate.GetComponent<RectTransform>();
            newRectT.sizeDelta = oldRectT.sizeDelta;
            newRectT.anchoredPosition = oldRectT.anchoredPosition;

            // wait until current reward template is vanished
            var fade = previousTemplate.AddComponent<Fade>();
            fade.Initialize(endOpacity: 0f);
            yield return WaitAnimationOrSkip(fade);
        }

        #endregion


        #region Achievement Rewards Animation

        IEnumerator PlayAchievementRewardAnimation()
        {
            var fadeIn = m_CurrentTemplateItem.AddComponent<Fade>();
            fadeIn.Initialize(duration: 0.35f, startScale: 0.8f, endScale:2f);

            yield return WaitAnimationOrSkip(fadeIn);
        }

        #endregion


        #region Boosts

        void SetUpBoostRewardTemplate(EBoost boost)
        {
            m_CurrentTemplateItem = Instantiate(AssetLoader.LoadBoostTemplate(boost), m_RewardIconSection.transform);
        }

        #endregion


        #region GUI Manipulators

        void DisplayRewardInfosContent(bool b)
        {
            m_RewardInfosContent.SetActive(b);
        }

        #endregion
    }
}