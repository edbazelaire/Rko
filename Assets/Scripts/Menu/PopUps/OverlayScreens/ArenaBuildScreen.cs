using Assets;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Sound;
using Assets.Scripts.UI;
using Data;
using Enums;
using Game.Loaders;
using Game.Spells;
using Menu.Common.Buttons;
using Menu.PopUps.Components;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace Menu.PopUps.OverlayScreens
{
    public class ArenaBuildScreen : OverlayScreen
    {
        #region Members

        TMP_Text                    m_RefreshTokenCounter;
        List<PowerUpSmallDisplay>   m_PowerUpsSmallDisplayers;
        GameObject                  m_CharacterSection;
        GameObject                  m_SpellsContainer;
        GameObject                  m_RunesContainer;
        SelectionScreen             m_SelectionScreen;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_RefreshTokenCounter = Finder.FindComponent<TMP_Text>(gameObject, "RefreshTokenCounter");
            m_PowerUpsSmallDisplayers = Finder.FindComponents<PowerUpSmallDisplay>(gameObject);
            m_CharacterSection = Finder.Find(gameObject, "CharacterSection");
            m_SpellsContainer = Finder.Find(gameObject, "SpellsContainer");
            m_RunesContainer = Finder.Find(gameObject, "RunesContainer");
            m_SelectionScreen = Finder.FindComponent<SelectionScreen>(gameObject, "SelectionScreen");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            RefreshTokens();
            SetupPowerUps();
            SetupCharacter();
            SetupSpells();
            SetupRunes();

            m_SelectionScreen.Initialize();
        }

        #endregion


        #region GUI Manipulators

        void RefreshTokens()
        {
            m_RefreshTokenCounter.text = ProgressionCloudData.CurrentArena.RefreshTokens.ToString();
            m_RefreshTokenCounter.color = ProgressionCloudData.CurrentArena.RefreshTokens <= 0 ? Color.red : Color.green;
        }     

        void SetupPowerUps()
        {
            for (int i = 0; i < m_PowerUpsSmallDisplayers.Count; i++)
            {
                if (ProgressionCloudData.CurrentArena.Level > i)
                {
                    string powerUpName = "";
                    if (ProgressionCloudData.CurrentArena.GetPowerUps().Length <= i)
                    {
                        ErrorHandler.Error("Number of max PowerUps " + ProgressionCloudData.CurrentArena.GetPowerUps().Length + " is <= to expeted index " + i);
                    }
                    else
                    {
                        powerUpName = ProgressionCloudData.CurrentArena.GetPowerUps()[i];
                    }

                    var powerUpData = powerUpName.IsNullOrEmpty() ? null : SpellLoader.GetPowerUp(powerUpName, InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter).Level);
                    m_PowerUpsSmallDisplayers[i].Initialize(powerUpData, i);
                }
                else
                {
                    m_PowerUpsSmallDisplayers[i].Initialize(null, i);
                }
            }
        }

        void SetupCharacter()
        {
            CoroutineManager.DelayMethod(() => UIHelper.SpawnCharacter(ProgressionCloudData.CurrentArena.BuildData.Character, m_CharacterSection, padding: new Vector2(0.1f, 0)));
        }

        void SetupSpells()
        {
            UIHelper.CleanContent(m_SpellsContainer);
            TemplateCollectableItemUI template = AssetLoader.LoadTemplateCollectableItem(ECollectableType.Spell);

            // display list of values
            for (int i = 0; i < ProgressionCloudData.CurrentArena.BuildData.Spells.Count(); i++)
            {
                // create template
                var templateItem = Instantiate(template, m_SpellsContainer.transform);

                // get value
                ESpell spell = ProgressionCloudData.CurrentArena.BuildData.Spells[i];

                // init template with values
                templateItem.Initialize(spell, true);
                templateItem.SetBottomOverlay(spell.ToString());

                int buildIndex = i;
                templateItem.AddSubButtons(new List<SubButton>()
                {   
                    new SubButton("Info", "yellow", () => OnInfoButtonClicked(templateItem, buildIndex)),
                    new SubButton("Refresh", "red", () => OnRefresh(ECollectableType.Spell, buildIndex)),
                });
            }
        }

        void SetupRunes()
        {
            UIHelper.CleanContent(m_RunesContainer);
            TemplateRuneItemUI template = AssetLoader.LoadTemplateItem<TemplateRuneItemUI>();
            AmovibleRuneContainer amovibleRuneContainerTemplate = AssetLoader.Load<AmovibleRuneContainer>(AssetLoader.c_OverlayPath + "DisplayScreens/ArenaBuildScreen/");

            // display list of values
            for (int i = 0; i < ProgressionCloudData.CurrentArena.BuildData.Runes.Count(); i++)
            {
                // create template
                var templateItem = Instantiate(template);

                // get value
                ERune rune = ProgressionCloudData.CurrentArena.BuildData.Runes[i];

                // init template with values
                templateItem.Initialize(rune, true);
                templateItem.SetBottomOverlay(rune.ToString());

                int buildIndex = i;
                templateItem.AddSubButtons(new List<SubButton>()
                {
                    new SubButton("Info", "yellow", () => OnInfoButtonClicked(templateItem, buildIndex)),
                    new SubButton("Refresh", "red", () => OnRefresh(ECollectableType.Rune, buildIndex)),
                });

                // add template in container
                AmovibleRuneContainer amovibleRuneContainer = Instantiate(amovibleRuneContainerTemplate, m_RunesContainer.transform);
                amovibleRuneContainer.Initialize(templateItem, buildIndex);
                amovibleRuneContainer.ButtonClickedEvent += OnRuneContainerButtonClicked;
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_SelectionScreen.OnEndEvent += OnSelectionScreenEnded;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_SelectionScreen.OnEndEvent -= OnSelectionScreenEnded;
        }

        public void OnInfoButtonClicked(TemplateCollectableItemUI template, int buildIndex)
        {
            template.ToggleSubButtons();

            if (template.Collectable.GetType() == typeof(ERune))
            {
                // display info
                ERuneActivation runeActivation = ERuneActivation.Minor;
                if (buildIndex == 0)
                    runeActivation = ERuneActivation.Primal;
                if (buildIndex == 1)
                    runeActivation = ERuneActivation.Major;

                Main.SetPopUp(EPopUpState.RuneInfoPopUp, (ERune)template.Collectable, ProgressionCloudData.CurrentArena.BuildData.RuneLevels[buildIndex], runeActivation);
                return;
            }

            CollectableData data;
            if (template.Collectable.GetType() == typeof(ESpell))
            {
                data = SpellLoader.GetSpellData((ESpell)template.Collectable, ProgressionCloudData.CurrentArena.BuildData.SpellLevels[buildIndex]);
            }
            else if (template.Collectable.GetType() == typeof(ECharacter))
            {
                data = CharacterLoader.GetCharacterData((ECharacter)template.Collectable, ProfileCloudData.AccountLevel);
            } else
            {
                ErrorHandler.Error("Unhandled collectable type : " + template.Collectable.GetType());
                return;
            }

            ScreenManager.CollectableInfoPopUp(data, infoOnly: true);

        }

        void OnRefresh(ECollectableType collectableType, int buildIndex)
        {
            // CHECK : is refresh button clickable
            if(ProgressionCloudData.CurrentArena.RefreshTokens <= 0)
            {
                if (! m_RefreshTokenCounter.HasComponent<ShakeAnimation>())
                {
                    var shakeAnimation = m_RefreshTokenCounter.AddComponent<ShakeAnimation>();
                    SoundFXManager.PlayOnce(SoundFXManager.UpgradeFailSoundFX);
                    shakeAnimation.Initialize("", 1f);
                }

                ScreenManager.QuickMessage("You do not have any <b>Refresh</b> available.\nFight in the Arena to collect more.");
                return;
            }

            // remove one token from the cloud data
            ProgressionCloudData.AddCurrentArenaRefreshToken(-1);

            // refresh UI of the tokens count
            RefreshTokens();

            m_SelectionScreen.Activate(collectableType, buildIndex, unAllowedValues: ProgressionCloudData.CurrentArena.BuildData.Get(collectableType));
        }

        void OnSelectionScreenEnded()
        {
            SetupSpells();
            SetupRunes();
        }

        public void OnRuneContainerButtonClicked(int buildIndex, int increment)
        {
            int toIndex = buildIndex + increment;
            if (toIndex < 0 || toIndex >= 3)
            {
                ErrorHandler.Error("Bad rune index requested : " + toIndex + " (from rune at index : "+buildIndex+")");
                return;
            }

            var build = ProgressionCloudData.CurrentArena.BuildData;
            var baseRune = build.Runes[buildIndex];
            build.Runes[buildIndex] = build.Runes[toIndex];
            build.Runes[toIndex] = baseRune;
            ProgressionCloudData.SetCurrentArenaBuild(build);

            // Refresh runes display
            SetupRunes();
        }

        #endregion
    }
}

