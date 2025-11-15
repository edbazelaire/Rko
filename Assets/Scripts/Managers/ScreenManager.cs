using Data.DataStructures;
using Data.GameManagement;
using Data;
using Enums;
using Menu.PopUps;
using Menu.PopUps.OverlayScreens;
using Menu.PopUps.PopUps.MessagePopUps;
using Scripts.Menu.PopUps;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;
using Menu.MainMenu;
using Unity.VisualScripting;
using MyBox;
using System.Collections;
using Save.Data.Progression.Structs;
using Data.DataStructures.PowerEffects;
using Data.DataStructures.CharacterSubStructures;

namespace Assets.Scripts.Managers
{
    public static class ScreenManager
    {
        #region Members

        const int ADDING_ORDER_IN_LAYER = 1000;

        public static MainMenuManager m_MainMenuManager;
        public static List<OverlayScreen> Screens = new();
        public static OverlayScreen CurrentScreen => Screens.Count > 0 ? Screens.Last() : null;
        public static int OrderInLayer = 0;
        public static Coroutine m_WaitFocusCoroutine;

        static Dictionary<EPopUpState, List<Action>> StoredEvents = new();

        public static MainMenuManager MainMenuManager
        {
            get
            {
                if (m_MainMenuManager == null || m_MainMenuManager.IsDestroyed())
                    m_MainMenuManager = Main.FindAnyObjectByType<MainMenuManager>();

                return m_MainMenuManager;
            }
        }

        #endregion


        #region PopUp & Screen Display

        public static void SetPopUp(EPopUpState popUpState, params object[] args)
        {
            var popUpPath = "";
            if (popUpState.ToString().EndsWith("Screen"))
            {
                popUpPath = AssetLoader.c_OverlayPath;
            }
            else if (popUpState.ToString().EndsWith("PopUp"))
            {
                popUpPath = AssetLoader.c_PopUpsPath;
            }
            else
            {
                ErrorHandler.Error("Unknown PopUp type " + popUpState.ToString() + " : unable to find adequate path");
            }

            // instantiate object in the canvas
            var obj = Main.Instantiate(AssetLoader.Load<GameObject>(popUpState.ToString(), popUpPath));
            if (obj == null)
            {
                ErrorHandler.Error("Unable to find popup : " + popUpState.ToString());
                return;
            }

            // setup initalization depending on the popup state
            switch (popUpState)
            {
                // MESSAGE POP UPS -------------------------------------------------------
                case EPopUpState.QuickMessagePopUp:
                    obj.GetComponent<QuickMessagePopUp>().Initialize(message: (string)args[0], duration: args.Count() > 1 ? (float)args[1] : 3f);
                    break;

                case EPopUpState.QuickRewardMessagePopUp:
                    obj.GetComponent<QuickRewardMessagePopUp>().Initialize(message: (string)args[0], rewardsData: (SRewardsData)args[1], duration: args.Count() > 2 ? (float)args[2] : 3f);
                    break;

                case EPopUpState.MessagePopUp:
                    obj.GetComponent<MessagePopUp>().Initialize(message: (string)args[0], title: args.Count() > 1 ? (string)args[1] : "", onValidate: args.Count() > 2 ? (Action)args[2] : null, onCancel: args.Count() > 3 ? (Action)args[3] : null);
                    break;

                case EPopUpState.ConfirmPopUp:
                    obj.GetComponent<ConfirmPopUp>().Initialize(message: args.Count() > 0 ? (string)args[0] : null, title: args.Count() > 1 ? (string)args[1] : "", onValidate: args.Count() > 2 ? (Action)args[2] : null, onCancel: args.Count() > 3 ? (Action)args[3] : null);
                    break;

                case EPopUpState.ConfirmBuyPopUp:
                    obj.GetComponent<ConfirmBuyPopUp>().Initialize((string)args[0], (string)args[1], (SPriceData)args[2], (bool)args[3], (SRewardsData)args[4], (Action)args[5], (Action)args[6]);
                    break;

                case EPopUpState.ConfirmBuyItemPopUp:
                    obj.GetComponent<ConfirmBuyItemPopUp>().Initialize((SPriceData)args[0], (Enum)args[1], (bool)args[2], (int)args[3], (Action)args[4], (Action)args[5]);
                    break;

                case EPopUpState.ConfirmBuyBundlePopUp:
                    obj.GetComponent<ConfirmBuyBundlePopUp>().Initialize((string)args[0], (string)args[1], (SPriceData)args[2], (bool)args[3], (SRewardsData)args[4], (Action)args[5], (Action)args[6]);
                    break;

                case EPopUpState.ConfirmUpgradeMasteryPopUp:
                    obj.GetComponent<ConfirmUpgradeMasteryPopUp>().Initialize(collectable: (Enum)args[0], mastery: (int)args[1], priceData: (SPriceData)args[2]);
                    break;

                // SCREENS -------------------------------------------------------
                case EPopUpState.RewardsScreen:
                    obj.GetComponent<RewardsScreen>().Initialize((SRewardsData)args[0], (string)args[1], args.Length > 2 ? (Action)args[2] : null, args.Length > 3 ? (string)args[3] : null);
                    break;

                case EPopUpState.AchievementRewardScreen:
                    obj.GetComponent<AchievementRewardScreen>().Initialize((List<SAchievementReward>)args[0]);
                    break;

                case EPopUpState.ArenaPathScreen:
                    obj.GetComponent<ArenaPathScreen>().Initialize((EArenaType)args[0], (SArenaDifficulty)args[1]);
                    break;

                case EPopUpState.EternalMenageriePathScreen:
                    obj.GetComponent<EternalMenageriePathScreen>().Initialize(EArenaType.EternalMenagerie, (SArenaDifficulty)args[0]);
                    break;

                case EPopUpState.LevelUpScreen:
                    obj.GetComponent<LevelUpScreen>().Initialize(currentXp: (int)args[0], maxXp: (int)args[1], bonusXp: (int)args[2]);
                    break;

                case EPopUpState.PowerUpInfoScreen:
                    obj.GetComponent<PowerUpInfoScreen>().Initialize((SPowerEffect)args[0], (int)args[1]);
                    break;

                case EPopUpState.PowerUpSelectionScreen:
                    obj.GetComponent<PowerUpSelectionScreen>().Initialize(args.Count() > 0 ? (int)args[0] : -1);
                    break;

                // INFO POP UPS -------------------------------------------------------
                case EPopUpState.SpellInfoPopUp:
                    obj.GetComponent<SpellInfoPopUp>().Initialize((ESpell)args[0], (int)args[1], infoOnly: args.Length > 2 && (bool)args[2]);
                    break;

                case EPopUpState.RuneInfoPopUp:
                    obj.GetComponent<RuneInfoPopUp>().Initialize((ERune)args[0], (int)args[1], infoOnly: args.Length > 2 && (bool)args[2], runeActivation: args.Length > 3 ? (ERuneActivation)args[3] : ERuneActivation.None);
                    break;

                case EPopUpState.CollectableInfoPopUp:
                case EPopUpState.CharacterInfoPopUp:
                    obj.GetComponent<CollectableInfoPopUp>().Initialize((ECharacter)args[0], (int)args[1], infoOnly: args.Length > 2 && (bool)args[2]);
                    break;

                case EPopUpState.BossInfoPopUp:
                    obj.GetComponent<BossInfoPopUp>().Initialize((string)args[0], (ESkin)args[1], (int)args[2], args.Count() > 3 ? (List<string>)args[3] : new List<string>(), args.Count() > 4 ? (List<SCharacterStatScaling>)args[4] : new List<SCharacterStatScaling>());
                    break;

                case EPopUpState.StateEffectPopUp:
                    obj.GetComponent<StateEffectPopUp>().Initialize((SStateEffectData)args[0], (int)args[1], args.Count() > 2 ? (int)args[2] : null, args.Count() > 3 ? (bool)args[3] : true);
                    break;

                case EPopUpState.TriggerEffectPopUp:
                    obj.GetComponent<TriggerEffectPopUp>().Initialize((STriggerEffect)args[0]);
                    break;

                case EPopUpState.RunePowerPopUp:
                    obj.GetComponent<RunePowerPopUp>().Initialize((SRunePower)args[0]);
                    break;

                case EPopUpState.PropertyInfoPopUp:
                    obj.GetComponent<PropertyInfoPopUp>().Initialize(key: (string)args[0], value: (string)args[1], duration: args.Count() > 2 ? (float)args[2] : -1f);
                    break;

                // SETTINGS & OPTIONS -------------------------------------------------------
                case EPopUpState.ArenaOptionsPopUp:
                    obj.GetComponent<ArenaOptionsPopUp>().Initialize();
                    break;

                case EPopUpState.SettingsPopUp:
                    obj.GetComponent<SettingsPopUp>().Initialize();
                    break;

                case EPopUpState.PseudoPopUp:
                    obj.GetComponent<PseudoPopUp>().Initialize(args.Count() > 0 ? (string)args[0] : "", "Select a Pseudo");
                    break;

                case EPopUpState.PromoCodePopUp:
                    obj.GetComponent<PromoCodePopUp>().Initialize();
                    break;

                case EPopUpState.DailyRewardsPopUp:
                    obj.GetComponent<DailyRewardsPopUp>().Initialize();
                    break;

                default:
                    obj.GetComponent<OverlayScreen>().Initialize();
                    break;
            }
        }

        public static void SetPopUpFadeIn(EPopUpState popUpState, params object[] args)
        {
            Main.Instance.StartCoroutine(AddBlackScreenUntilLoaded(popUpState));
            SetPopUp(popUpState, args);
        }

        public static IEnumerator AddBlackScreenUntilLoaded(EPopUpState popUpState)
        {
            SetPopUp(EPopUpState.BlackScreen);
            while (! HasScreen(popUpState, checkInitialized: true))
            {
                yield return null;
            }

            Close(EPopUpState.BlackScreen);
        }

        #endregion


        #region Popup Message

        public static void MessagePopUp(string message, string title = "", Action onValidate = null, Action onCancel = null)
        {
            SetPopUp(EPopUpState.MessagePopUp, message, title, onValidate, onCancel);
        }

        public static void QuickMessage(string message, float duration = 3f)
        {
            SetPopUp(EPopUpState.QuickMessagePopUp, message, duration);
        }

        public static void QuickRewardMessage(string message, SRewardsData rewardsData, float duration = 3f)
        {
            SetPopUp(EPopUpState.QuickRewardMessagePopUp, message, rewardsData, duration);
        }

        #endregion


        #region Confirm PopUps

        /// <summary>
        /// Confirm purchase of an item or a bundle of items (currency, chests, collectables)
        /// </summary>
        /// <param name="priceData"></param>
        /// <param name="rewardsData"></param>
        /// <param name="OnPurchase"></param>
        public static void ConfirmPopUp(string message, string title = "", Action onValidate = null, Action onCancel = null)
        {
            SetPopUp(EPopUpState.ConfirmPopUp, message, title, onValidate, onCancel);
        }

        /// <summary>
        /// Confirm purchase of an item or a bundle of items (currency, chests, collectables)
        /// </summary>
        /// <param name="priceData"></param>
        /// <param name="rewardsData"></param>
        /// <param name="OnPurchase"></param>
        public static void ConfirmWatchAd(Action callback, string title = "", string text = "")
        {
            SetPopUp(EPopUpState.ConfirmBuyBundlePopUp, callback, title, text);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ConfirmUpgradeMastery(Enum collectable, int mastery, SPriceData price)
        {
            SetPopUp(EPopUpState.ConfirmUpgradeMasteryPopUp, collectable, mastery, price);
        }

        #endregion


        #region Rewards

        public static void DisplayRewards(SRewardsData rewardsData, string context, Action OnRewardCollected = null, string title = null)
        {
            Main.SetPopUp(EPopUpState.RewardsScreen, rewardsData, context, OnRewardCollected, title);
        }

        public static void DisplayAchievementRewards(List<SAchievementReward> rewardsData)
        {
            Main.SetPopUp(EPopUpState.AchievementRewardScreen, rewardsData);
        }

        #endregion


        #region Info Popups

        public static void CollectableInfoPopUp(CollectableData collectableData, ERuneActivation runeActivation = ERuneActivation.None, bool infoOnly = true)
        {
            CollectableInfoPopUp popup;
            if (collectableData is SpellData spellData)
            {
                popup = Main.Instantiate(AssetLoader.Load<SpellInfoPopUp>("SpellInfoPopUp", AssetLoader.c_PopUpsPath));
                popup.Initialize(spellData, infoOnly);
            }

            else if (collectableData is RuneData runeData)
            {
                var runePopup = Main.Instantiate(AssetLoader.Load<RuneInfoPopUp>("RuneInfoPopUp", AssetLoader.c_PopUpsPath));
                runePopup.Initialize(runeData, runeActivation, infoOnly);
            }

            else
            {
                popup = Main.Instantiate(AssetLoader.Load<CharacterInfoPopUp>("CharacterInfoPopUp", AssetLoader.c_PopUpsPath));
                popup.Initialize(collectableData, infoOnly);
            }
        }

        public static void StateEffectPopUp(SStateEffectData stateEffectData, int level, int stacksInDescription = 0, bool displayNextLevel = true)
        {
            SetPopUp(EPopUpState.StateEffectPopUp, stateEffectData, level, stacksInDescription, displayNextLevel);
        }

        #endregion


        #region Arena Screens

        public static PowerUpSelectionScreen PowerUpSelectionScreen(EArenaType arenaType, int index, ERuneActivation? runeActivation = null)
        {
            PowerUpSelectionScreen screen;
            switch (arenaType)
            {
                case EArenaType.EternalMenagerie:
                    screen = AssetLoader.Load<PowerUpSelectionScreen_EM>("PowerUpSelectionScreen_EM", AssetLoader.c_OverlayPath);
                    if (screen == null)
                    {
                        ErrorHandler.Error("Unable to screen PowerUpSelectionScreen_EM for arena " + arenaType.ToString());
                        break;
                    }
                    screen = Main.Instantiate(screen);
                    screen.Initialize(index, runeActivation);
                    return screen;
            }

            screen = Main.Instantiate(AssetLoader.Load<PowerUpSelectionScreen>("PowerUpSelectionScreen", AssetLoader.c_OverlayPath));
            screen.Initialize(index, runeActivation);

            return screen;
        }

        #endregion


        #region Screen Order Management

        public static void AddScreen(OverlayScreen screen)
        {
            OrderInLayer += ADDING_ORDER_IN_LAYER;
            Screens.Add(screen);

            // stop current wait for screen focus
            if (m_WaitFocusCoroutine != null)
                Main.Instance.StopCoroutine(m_WaitFocusCoroutine);

            // wait to be sure that the screen current screen is focused (wait end of Init() animation, etc)
            m_WaitFocusCoroutine = Main.Instance.StartCoroutine(WaitScreenFocus(screen, true));
        }

        public static void RemoveScreen(OverlayScreen screen) 
        {
            if (screen.IsDestroyed())
                return;

            int index = Screens.IndexOf(screen);
            if (index == -1)
            {
                ErrorHandler.Error("Trying to remove screen " + screen.PopUpName + " from Screens but is not in list of screens");
                return;
            }

            bool isLast = index == Screens.Count - 1;   
            Screens.RemoveAt(index);

            if (isLast)
                RecalculateOrderInLayer();

            // stop current wait for screen focus
            if (m_WaitFocusCoroutine != null)
                Main.Instance.StopCoroutine(m_WaitFocusCoroutine);

            // wait to be sure that the screen current screen is focused (wait end of End() animation, etc)
            m_WaitFocusCoroutine = Main.Instance.StartCoroutine(WaitScreenFocus(screen, false));
        }

        public static void Close(EPopUpState popUpState)
        {
            var screen = Screens.Where(t => t.PopUpName == popUpState.ToString());
            if (! screen.Any())
            {
                ErrorHandler.Warning("Trying to close " + popUpState + " but no screen with that name was found");
                return;
            }

            // exit the screen
            screen.Last().Exit();
        }

        public static void Clear()
        {
            Screens = new List<OverlayScreen>();
            OrderInLayer = 0;
        }

        static void RecalculateOrderInLayer()
        {
            for (int i = Screens.Count - 1; i >= 0; i--)
            {
                var screen = Screens[i];

                // timing issue - wait for 1 frame to recalculate order in layer
                if (screen == null || screen.IsDestroyed() || screen.Canvas.IsDestroyed() || screen.Canvas == null)
                    continue;

                OrderInLayer = screen.Canvas.sortingOrder;
                return;
            }

            OrderInLayer = 0;
        }

        public static bool HasScreen(EPopUpState popUpState, bool checkInitialized = false)
        {
            foreach(var screen in Screens)
            {
                if (screen.PopUpState == popUpState && (!checkInitialized || screen.Initialized))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Close all screens until
        /// </summary>
        /// <param name="popupState"></param>
        public static bool GoToScreen(EPopUpState popupState)
        {
            int nScreensToClose;
            if (popupState == EPopUpState.MainMenuScreen)
                nScreensToClose = Screens.Count;
            else
            {
                nScreensToClose = GetScreenIndex(popupState);
                if (nScreensToClose == -1)
                {
                    ErrorHandler.Error("Unable to find screen " + popupState.ToString() + " in list of screens");
                    return false;
                }    
            }

            for (int i = 0; i < nScreensToClose; i++)
            {
                CurrentScreen.Exit();
            }
           
            return true;
        }

        public static int GetScreenIndex(EPopUpState popupState)
        {
            int i = 0;
            foreach (var screen in Screens)
            {
                if (screen.PopUpState == popupState)
                    return i;
                
                i++;
            }

            return -1;
        }

        #endregion


        #region Events

        public static IEnumerator WaitScreenFocus(OverlayScreen overlayScreen, bool waitFocus)
        {
            if (waitFocus)
            {
                while (! overlayScreen.IsDestroyed())
                {
                    yield return null;
                }
            }
            else
            {
                while (! overlayScreen.IsDestroyed() && ! overlayScreen.IsDisplayed)
                {
                    yield return null;
                }
            }
            
            PlayStoredEvents(CurrentScreen == null ? EPopUpState.MainMenuScreen : CurrentScreen.PopUpState);
        }

        /// <summary>
        /// Store an event to a PopUp when its going to get focused
        /// </summary>
        /// <param name=""></param>
        public static void StoreEvent(EPopUpState popup, Action callback)
        {
            if (!StoredEvents.ContainsKey(popup))
                StoredEvents.Add(popup, new List<Action> { });

            StoredEvents[popup].Add(callback);
        }

        /// <summary>
        /// Play events stored for a popup
        /// </summary>
        /// <param name="popup"></param>
        public static void PlayStoredEvents(EPopUpState popup)
        {
            if (! StoredEvents.ContainsKey(popup) || StoredEvents[popup].IsNullOrEmpty())
                return;

            foreach (Action callback in StoredEvents[popup])
            {
                callback();
            }

            StoredEvents.Remove(popup);
        }

        #endregion

    }
}