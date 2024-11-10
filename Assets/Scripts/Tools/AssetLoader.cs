using Data.GameManagement;
using Enums;
using Game;
using Game.UI.EndGameUI;
using Menu.Common.Buttons;
using Save;
using System.Linq;
using UnityEngine;

namespace Tools
{
    public static class AssetLoader
    {
        const string c_IconPrefix                           = "Ic_";
        const string c_ChestSuffix                          = "Chest";
        const string c_TemplatePrefix                       = "Template";

        // =============================================================================================================
        // DATA
        public const string c_DataPath                      = "Data/";
        public const string c_CharacterDataPath             = c_DataPath + "Characters/";
        public const string c_SpellDataPath                 = c_DataPath + "Spells/";
        public const string c_StateEffectDataPath           = c_DataPath + "StateEffects/";
        public const string c_PowerUpsPath                  = c_DataPath + "PowerUps/";
        public const string c_ItemsDataPath                 = c_DataPath + "Items/";
        public const string c_AchievementsDataPath          = c_DataPath + "Achievements/";
        public const string c_ChestsDataPath                = c_ItemsDataPath + "Chests/";
        public const string c_ManagementDataPath            = c_DataPath + "GameManagement/";
        public const string c_ArenaDataPath                 = c_ManagementDataPath + "Arenas/";
        public const string c_AIDataPath                    = c_DataPath + "AI/";

        // =============================================================================================================
        // PREFABS
        public const string c_PrefabsPath                   = "Prefabs/";
        // ---- Managers 
        public const string c_ManagersPath                  = c_PrefabsPath + "Managers/";
        // ---- Backgrounds 
        public const string c_BackgroundsPath               = c_PrefabsPath + "Backgrounds/";
        public const string c_ArenaBackgroundsPath          = c_BackgroundsPath + "Arenas/";
        // ---- Characters 
        public const string c_CharactersPreviewPath         = c_PrefabsPath + "Characters/";
        public const string c_BossesPreviewPath             = c_PrefabsPath + "Bosses/";
        // ---- Spells 
        public const string c_SpellsPrefabsPath             = c_PrefabsPath + "Spells/";
        // ---- Items 
        public const string c_ItemsPrefabPath               = c_PrefabsPath + "Items/";
        public const string c_ParticlesPrefabPath           = c_PrefabsPath + "Particles/";

        // =============================================================================================================
        // UI 
        public const string c_UIPath                        = "UI/";
        // ---- Templates
        public const string c_TemplatesUIPath               = c_UIPath  + "Templates/";
        public const string c_TemplatesShopPath             = c_TemplatesUIPath + "Shop/";
        public const string c_AchievementsTemplatesPath     = c_TemplatesUIPath + "Achievements/";
        public const string c_PowerUpsTemplatesPath         = c_TemplatesUIPath + "PowerUps/";
        // ---- Commons
        public const string c_CommonPath                    = c_UIPath + "Common/";
        public const string c_ButtonPath                    = c_CommonPath + "Buttons/";
        // ---- Menus
        public const string c_MainUIPath                    = c_UIPath + "Main/";
        public const string c_MainUIComponentsPath          = c_MainUIPath + "Components/";
        public const string c_MainUIComponentsInfosPath     = c_MainUIComponentsPath + "Infos/";
        public const string c_MainMenuPath                  = c_MainUIPath + "MainMenu/";
        public const string c_MainTabPath                   = c_MainMenuPath + "MainTab/";
        public const string c_ProfileTabPath                = c_MainMenuPath + "ProfileTab/";
        // ---- Arena Background
        public const string c_GameContentPath               = c_UIPath + "Game/";
        public const string c_GameUIContentPath             = c_GameContentPath + "GameUI/";
        public const string c_SpawnUIContentPath            = c_GameUIContentPath + "Spawns/";
        public const string c_ArenaBackgroundPath           = c_GameContentPath + "Arena/";
        public const string c_TutoGameObjectsPath           = c_GameContentPath + "Tuto/";
        // ---- solo mode ui
        public const string c_GameSectionPath               = c_MainTabPath + "GameSection/";
        public const string c_ArenaModeUIPath               = c_GameSectionPath + "ArenaMode/";
        public const string c_RankedModeUIPath              = c_GameSectionPath + "RankedMode/";
        // ---- settings data
        public const string c_SettingsPath                  = c_UIPath + "Settings/";
        // ---- PopUps & Overlays
        public const string c_OverlayPath                   = c_UIPath + "OverlayScreens/";
        public const string c_PopUpsPath                    = c_OverlayPath + "PopUps/";
        public const string c_CollectablesPopUpPath         = c_PopUpsPath + "Collectables/";

        // =============================================================================================================
        // SPRITES
        public const string c_SpritesPath                   = "Sprites/";
        
        // -- UI
        public const string c_UISpritesPath                 = c_SpritesPath + "UI/";
        public const string c_ButtonsPath                   = c_UISpritesPath + "Buttons/";
        public const string c_RaysPath                      = c_UISpritesPath + "Rays/";
        public const string c_TutoUIPath                    = c_UISpritesPath + "Tuto/";
        public const string c_CaptionsPath                  = c_TutoUIPath + "Captions/";
        
        // -- Backgrounds
        public const string c_BackgroundsImagePath         = c_SpritesPath + "Backgrounds/";
        public const string c_ArenaBackgroundsImagePath    = c_BackgroundsImagePath + "Arenas/";

        // -- Leagues
        public const string c_LeagueBannersPath             = c_SpritesPath + "Leagues/";

        // -- Profile
        public const string c_ProfilePath                   = "Sprites/Profile/";
        public const string c_AvatarsPath                   = c_ProfilePath + "Avatars/";
        public const string c_BordersPath                   = c_ProfilePath + "Borders/";
        public const string c_BadgesPath                    = c_ProfilePath + "Badges/";
        
        // -- Icons
        public const string c_IconPath                      = c_SpritesPath + "Icons/";
        public const string c_IconCharactersPath            = c_IconPath + "Characters/";
        public const string c_IconBossesHeadsPath           = c_IconCharactersPath + "BossesHeads/";
        public const string c_IconSpellsPath                = c_IconPath + "Spells/";
        public const string c_IconStateEffectsPath          = c_IconSpellsPath + "StateEffects/";
        public const string c_IconRunesPath                 = c_IconPath + "Runes/";
        public const string c_ItemsPath                     = c_IconPath + "Items/";
        public const string c_CurrenciesPath                = c_ItemsPath + "Currencies/";
        public const string c_ChestsIconPath                = c_ItemsPath + "Chests/";
        public const string c_ShopPath                      = c_IconPath + "Shop/";
        public const string c_IconUIElementsPath            = c_IconPath + "UIElements/";
        public const string c_IconFiltersPath               = c_IconUIElementsPath + "Filters/";

        // =============================================================================================================
        // ANIMATIONS
        public const string c_AnimationPath                 = "Animations/";
        public const string c_AnimationParticlesPath        = c_AnimationPath + "Particles/";
        public const string c_AnimationBackgroundsPath      = c_AnimationPath + "Backgrounds/";

        // SOUNDS
        public const string c_SoundsPath                = "Sounds/";


        #region Default Methods

        public static T Load<T>(string path, bool warning = true) where T : Object
        {
            if (! path.EndsWith("/"))
            {
                var ressource = Resources.Load<T>(path);
                if (ressource == null && warning)
                    ErrorHandler.Warning($"AssetLoader : Ressource {path} not found");
                return ressource;
            }

            var allRessources = LoadAll<T>(path);
            if (allRessources.Length == 0)
            {
                if (warning)
                    ErrorHandler.Warning($"AssetLoader : Unable to find any ressource with Component {typeof(T)} in {path}");
                return null;
            }

            if (allRessources.Length > 1 && warning)
                ErrorHandler.Warning($"AssetLoader : Found multiple ressources with Component {typeof(T)} in {path} - please affine your research with a name");

            return allRessources[0];
        }

        public static T Load<T>(string assetName, string dirpath, bool warning = true) where T : Object
        {
            // try to find the ressource directly
            var ressource = Load<T>(dirpath + assetName, false);
            if (ressource != null)
                return ressource;

            // get all ressources in path and find one with similar name
            var allRessources = LoadAll<T>(dirpath);
            foreach(var temp in allRessources)
            {
                if (temp.name == assetName)
                {
                    return temp;
                }
            }

            if (warning)
                ErrorHandler.Error("Unable to find any ressource named " + assetName + " with type " + typeof(T) + " at " + dirpath);
            return null;
        }

        public static T[] LoadAll<T>(string path) where T : Object
        {
            return Resources.LoadAll<T>(path);
        }

        #endregion


        #region Data Loading

        public static ArenaData LoadArenaData(EArenaType arena, SArenaDifficulty? arenaDifficulty = null)
        {
            if (! arenaDifficulty.HasValue)
            {
                arenaDifficulty = ProgressionCloudData.GetUnlockedArenaDifficulty(arena);
            }

            var arenaData = Load<ArenaData>(arena.ToString() + "_" + arenaDifficulty.Value.Difficulty.ToString(), c_ArenaDataPath);

            if (arenaData == null)
                return null;

            arenaData.SetDifficultyLevel(arenaDifficulty.Value.Level);
            return arenaData;
        }


        #endregion


        #region Manager Loading

        public static T LoadManager<T>() where T : Object
        {
            T[] managers = LoadAll<T>(c_ManagersPath);
            if (managers.Length == 0)
            {
                ErrorHandler.Error("Unable to find any Manager of type " + typeof(T) + " in " + c_ManagersPath);
                return null;
            }

            if (managers.Length > 1)
            {
                ErrorHandler.Error("Found multiple Managers of type " + typeof(T) + " in " + c_ManagersPath);
            }

            return managers[0];
        }

        #endregion


        #region Character & Spells Prefabs Loading

        public static GameObject LoadCharacterPreview(string characterName)
        {
            return Load<GameObject>(characterName + "Preview", c_CharactersPreviewPath);
        }

        public static GameObject[] LoadSpellPrefabs()
        {
            return Resources.LoadAll<GameObject>(c_SpellsPrefabsPath);
        }

        #endregion


        #region Arena Loading


        public static ArenaManager LoadArena(string arenaName)
        {
            return Load<ArenaManager>(arenaName, c_ArenaBackgroundPath);
        }

        #endregion


        #region ItemUI Prefabs

        public static GameObject LoadChestPrefab(EChest chestType)
        {
            return Load<GameObject>(c_ItemsPrefabPath + chestType.ToString() + c_ChestSuffix);
        }

        public static GameObject LoadTemplateItem(string suffix)
        {
            return Load<GameObject>(c_TemplatesUIPath + c_TemplatePrefix + suffix);
        }

        public static T LoadTemplateItem<T>(string suffix = "") where T : Object
        {
            if (suffix != "")
                return Load<T>(c_TemplatesUIPath + c_TemplatePrefix + suffix);

            var allTemplates = LoadAll<T>(c_TemplatesUIPath);
            
            if (allTemplates.Length == 0)
            {
                ErrorHandler.Warning("Unable to find any template of type : " + typeof(T).ToString() + " - in " + c_TemplatesUIPath);
                return null;
            }

            if (allTemplates.Length > 1)
            {
                ErrorHandler.Warning("Found multiple templates with type : " + typeof(T).ToString());
            }

            return allTemplates[0];
        }

        public static GameObject LoadTemplateItem(System.Enum item)
        {
            return LoadTemplateItem(item.GetType().ToString().Split('.')[1][1..] + "Item");
        }

        public static TemplateShopItemUI LoadShopTemplateItem(string name = "")
        {
            if (name == "" || name == "default")
            {
                name = "Shop";
            }

            return Load<TemplateShopItemUI>(c_TemplatesShopPath + c_TemplatePrefix + name + "Item");
        }

        public static T LoadShopTemplateItem<T>(string name = "") where T : TemplateShopItemUI
        {
            var allTemplates = LoadAll<T>(c_TemplatesShopPath);

            if (allTemplates.Length == 0)
            {
                ErrorHandler.Warning("Unable to find any template of type : " + typeof(T).ToString() + " - in " + c_TemplatesUIPath);
                return null;
            }

            if (allTemplates.Length > 1)
            {
                if (name != "")
                {
                    foreach (var template in allTemplates)
                    {
                        if (template.name == name)
                            return template;
                    }

                    ErrorHandler.Warning("Found multiple templates with type : " + typeof(T).ToString() + " - but none was named : " + name);
                }
                else
                {
                    ErrorHandler.Warning("Found multiple templates with type : " + typeof(T).ToString() + " - but no name was provided to distinguish");
                }
            }

            return allTemplates[0];
        }

        public static AchievementRewardUI LoadAchievementRewardTemplate(EAchievementReward achievementReward)
        {
            string templateBaseName = achievementReward.ToString();
            if (achievementReward == EAchievementReward.Border)
            {
                templateBaseName = EAchievementReward.Avatar.ToString();
            }
            return Load<AchievementRewardUI>(templateBaseName + "Button", c_AchievementsTemplatesPath);
        }

        public static GameObject LoadArenaButton(EArenaType arenaType)
        {
            return Load<GameObject>(arenaType.ToString() + "Button", c_ArenaModeUIPath + "ArenaButtons/");
        }

        public static LeagueBannerButton LoadLeagueButton()
        {
            return Load<LeagueBannerButton>("LeagueBannerButton", c_RankedModeUIPath);
        }

        public static PowerUpItem LoadPowerUpItem(ERuneActivation runeActivation)
        {
            return Load<PowerUpItem>("PowerUpItem_" + runeActivation.ToString(), c_PowerUpsTemplatesPath);
        }

        #endregion


        #region Tutorial Sprites

        public static Sprite LoadCaption(ECaptionType captionType, ECaptionColor color = ECaptionColor.None)
        {
            // Load all sprites from the given path
            var allCaptions = LoadAll<Sprite>(c_CaptionsPath);

            // get start name
            string startsWith = "Caption";
            if (captionType != ECaptionType.None)
                startsWith += "_" + captionType.ToString();
            if (color != ECaptionColor.None)
                startsWith += "_" + color.ToString();

            // Filter the sprites that match the prefix
            var filteredCaptions = allCaptions
                                    .Where(sprite => sprite.name.StartsWith(startsWith))
                                    .ToArray(); // Convert the result to an array for random access

            // If no sprites match the criteria, return null to avoid errors
            if (filteredCaptions.Length == 0)
                return null;

            // Return a random sprite from the filtered list
            return filteredCaptions[Random.Range(0, filteredCaptions.Length)];
        }


        #endregion


        #region Icon Loading

        public static Sprite LoadIcon (string itemName, System.Type iconType = null)
        {
            string path;
            
            if (iconType == typeof(ECharacter))
                path = c_IconCharactersPath;

            else if (iconType == typeof(ESpell))
                path = c_IconSpellsPath;

            else if (iconType == typeof(EStateEffect))
                path = c_IconStateEffectsPath;

            else if (iconType == typeof(ERune))
                path = c_IconRunesPath;

            else if (iconType == typeof(ECurrency))
                path = c_CurrenciesPath;

            else if (iconType == typeof(EAvatar))
                return Load<Sprite>(itemName, AssetLoader.c_AvatarsPath);

            else if (iconType == typeof(EBorder))
                return Load<Sprite>(itemName, AssetLoader.c_BordersPath);

            else if (iconType == typeof(EBadge))
                return Load<Sprite>(itemName, AssetLoader.c_BadgesPath);

            else if (iconType == typeof(EChest))
            {
                path = c_ChestsIconPath;
                itemName += "Chest";
            }
            else
            {
                if (iconType != null)
                    ErrorHandler.Error("Unhandled type of enum " + iconType + " for icon " + itemName + " - skipping");
                
                // no specific found : load any icon 
                return Load<Sprite>(c_IconPrefix + itemName, c_IconPath);
            }
            return Load<Sprite>(path + c_IconPrefix + itemName);
        }

        public static Sprite LoadIcon(System.Enum value)
        {
            return LoadIcon(value.ToString(), value.GetType());
        }

        public static Sprite LoadCharacterIcon(string character)
        {
            return Load<Sprite>(c_IconCharactersPath + c_IconPrefix + character);
        }

        public static Sprite LoadBossHead(string boss)
        {
            return Load<Sprite>(c_IconBossesHeadsPath + boss + "_Head");
        }

        /// <summary>
        /// Load any icon that is spell related (can be "Trigger" or "StateEffects", ...)
        /// </summary>
        /// <param name="name">Name of the effect, the spell or the trigger effect</param>
        /// <returns></returns>
        public static Sprite LoadSpellIcon(string name)
        {
            return Load<Sprite>(c_IconPrefix + name, c_IconSpellsPath);
        }

        public static Sprite LoadStateEffectIcon(string stateEffect)
        {
            // search in StateEffects file first
            var icon = Load<Sprite>(c_IconStateEffectsPath + c_IconPrefix + stateEffect, false);
            if (icon != null)
                return icon;

            // search in any "Spells" file or sub-files
            icon = Load<Sprite>(c_IconPrefix + stateEffect, c_IconSpellsPath);
            return icon;
        }

        public static Sprite LoadRuneIcon(ERune rune)
        {
            return Load<Sprite>(c_IconRunesPath + c_IconPrefix + rune.ToString());
        }

        public static Sprite LoadCurrencyIcon(ECurrency currency, int? qty = null)
        {
            if (!qty.HasValue || currency == ECurrency.Xp)
                return Load<Sprite>(c_CurrenciesPath + c_IconPrefix + currency.ToString());
           
            float factor = currency == ECurrency.Gems ? 500f : 5000f;
            int packNumber = Mathf.Clamp((int)Mathf.Round(qty.Value / factor), 1, currency == ECurrency.Golds ? 4 : 3);
            return Load<Sprite>(c_ShopPath + currency.ToString() + "Pack_0" + packNumber.ToString());
        }

        public static Sprite LoadShopIcon(string shopOfferName)
        {
            return Load<Sprite>(c_ShopPath + c_IconPrefix + shopOfferName);
        }

        public static Sprite LoadChestIcon(string chest)
        {
            return Load<Sprite>(c_ChestsIconPath + c_IconPrefix + chest.ToString() + c_ChestSuffix);
        }

        public static Sprite LoadChestIcon(EChest chest)
        {
            return LoadChestIcon(chest.ToString());
        }

        public static Sprite LoadLeagueBanner(ELeague league)
        {
            return Load<Sprite>(c_LeagueBannersPath + league.ToString() + "LeagueBanner");
        }

        public static Sprite LoadAchievementRewardIcon(string name, EAchievementReward arType)
        {
            string path;
            switch (arType)
            {
                case EAchievementReward.Avatar:
                    path = c_AvatarsPath;
                    break;
                case EAchievementReward.Border:
                    path = c_BordersPath;
                    break;
                case EAchievementReward.Badge:
                    path = c_BadgesPath;
                    break;

                default:
                    ErrorHandler.Error("Unhandled case " + arType);
                    return default;
            }

            return AssetLoader.Load<Sprite>(name, path);
        }

        public static Sprite LoadUIElementIcon(string name)
        {
            return Load<Sprite>(c_IconUIElementsPath + c_IconPrefix + name);
        }

        public static Sprite LoadFilterIcon(string name)
        {
            return Load<Sprite>(c_IconFiltersPath + c_IconPrefix + name);
        }

        #endregion


        #region Profile Loading

        public static Sprite LoadBadgeIcon(EBadge badge, ELeague league)
        {
            return LoadBadgeIcon(badge.ToString() + (league != ELeague.None ? league.ToString() : ""));
        }

        public static Sprite LoadBadgeIcon(string badge)
        {
            return Load<Sprite>(badge, c_BadgesPath);
        }

        #endregion


        #region Animations

        public static GameObject LoadBackgroundAnimation(string name)
        {
            return Load<GameObject>(name, c_AnimationBackgroundsPath);
        }

        #endregion
    }
}