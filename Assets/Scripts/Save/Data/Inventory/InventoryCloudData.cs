using Assets;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tools;
using Unity.Services.CloudSave.Models;
using Unity.VisualScripting;

namespace Save
{
    [Serializable]
    public struct SCollectableCloudData
    {
        public string   CollectableName;
        public int      Level;
        public int      Qty;

        [NonSerialized]
        private Enum m_Collectable;

        public SCollectableCloudData(Enum collectable, int level = 1, int qty = 0)
        {
            CollectableName = collectable.ToString();
            Level           = level;
            Qty             = qty;

            m_Collectable   = collectable;
        }

        public int GetQty()
        {
            if (GetCollectable().GetType() == typeof(ECharacter))
                return InventoryCloudData.Instance.GetCurrency(ECurrency.Xp);

            return Qty;
        }

        public void SetQty(int value)
        {
            if (GetCollectable().GetType() == typeof(ECharacter))
            {
                InventoryCloudData.Instance.SetCurrency(ECurrency.Xp, value);
                return;
            }

            Qty = value;
        }

        public void AddQty(int value)
        {
            SetQty(GetQty() + value);
        }

        public void SetCollectable()
        {
            if (CollectableName == "")
            {
                ErrorHandler.Error("Unable to set collectable : name is empty");
                return;
            }

            if (Enum.TryParse(CollectableName, out ECharacter temp1))
            {
                m_Collectable = temp1;
                return;
            }

            if (Enum.TryParse(CollectableName, out ESpell temp2))
            {
                m_Collectable = temp2;
                return;
            }

            if (Enum.TryParse(CollectableName, out ERune temp3))
            {
                m_Collectable = temp3;
                return;
            }

            ErrorHandler.Error("Unknown CollectableName : " + CollectableName);
        }

        public Enum GetCollectable()
        {
            if (m_Collectable == null)
                SetCollectable();
                
            return m_Collectable;
        }

        public TEnum GetCollectable<TEnum>() where TEnum : Enum
        {
            return (TEnum)GetCollectable();  
        }
        
        /// <summary>
        /// Is the collectable at max level ?
        /// </summary>
        /// <returns></returns>
        public bool IsMaxLevel()
        {
            return Level >= CollectablesManagementData.GetMaxLevel(GetCollectable());
        }

        /// <summary>
        /// Has enought ressources to upgrade the collectable ?
        /// </summary>
        /// <returns></returns>
        public bool HasEnoughQty()
        {
            return GetQty() >= CollectablesManagementData.GetLevelData(GetCollectable(), Level).RequiredQty;
        }

        /// <summary>
        /// Is collectbale upgradable
        /// </summary>
        /// <returns></returns>
        public bool IsUpgradable()
        {
            if (IsMaxLevel() || ! HasEnoughQty())
                return false;

            if (GetCollectable().GetType() == typeof(ECharacter))
            {
                if (Level >= ProfileCloudData.AccountLevel)
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SCollectableCloudData : { \n");
            sb.Append("     Collectable : " + TextHandler.ToString(GetCollectable()) + ",\n");
            sb.Append("     Level : " + TextHandler.ToString(Level) + ",\n");
            sb.Append("     Qty : " + TextHandler.ToString(GetQty()) + ",\n");
            sb.Append("}\n");

            return sb.ToString();
        }
    }


    [Serializable]
    public struct SInfoCollectable
    {
        public string Key;
        public Enum[] DefaultData;

        public SInfoCollectable(string Key, Enum[] DefaultData)
        {
            this.Key = Key;
            this.DefaultData = DefaultData;
        }
    }


    public class InventoryCloudData : CloudData
    {
        #region Members

        public new static InventoryCloudData Instance => Main.CloudSaveManager.GetCloudData(typeof(InventoryCloudData)) as InventoryCloudData;


        // ===============================================================================================
        // CONSTANTS
        // -- Values
        public Type[] COLLECTABLE_TYPES                     => new Type[] { typeof(ECharacter), typeof(ESpell), typeof(ERune) };
        public Enum[] IGNORED_COLLECTABLES                  => new Enum[] { ESpell.None, ECharacter.None , ERune.None };

        // -- Keys
        public const string KEY_GOLDS       = "Golds";
        public const string KEY_GEMS        = "Gems";
        public const string KEY_TOTAL_XP    = "TotalXp";
        public const string KEY_XP          = "Xp";
        public const string KEY_SPELLS      = "Spells";
        public const string KEY_CHARACTERS  = "Characters";
        public const string KEY_RUNES       = "Runes";

        // -- Informations
        public Dictionary<Type, SInfoCollectable> InfoCollectables = new Dictionary<Type, SInfoCollectable>
        {
            { typeof(ECharacter),   new SInfoCollectable(KEY_CHARACTERS,  new Enum[] { CharacterBuildsCloudData.DEFAULT_CHARACTER, ECharacter.Nagini, ECharacter.Kahnan, ECharacter.Srug, ECharacter.Marcus } ) },
            { typeof(ESpell),       new SInfoCollectable(KEY_SPELLS,      CharacterBuildsCloudData.DEFAULT_BUILD.Cast<Enum>().ToArray() ) },
            { typeof(ERune),        new SInfoCollectable(KEY_RUNES,       new Enum[] { } ) }
        };

        // ===============================================================================================
        // EVENTS
        /// <summary> action fired when the amount of gold changed </summary>
        public static Action<ECurrency, int>        CurrencyChangedEvent;
        /// <summary> event fired when a collectable data has changed </summary>
        public static Action<SCollectableCloudData> CollectableDataChangedEvent;
        /// <summary> event fired when a collectable data has been upgraded </summary>
        public static Action<SCollectableCloudData> CollectableUnlockedEvent;
        /// <summary> event fired when reset called in the database </summary>
        public static Action<ECollectableType>      ResetCollectableEvent;

        /// <summary> SPECIFIC EVENT : for collectable data changed of type "Character" : (remove ?) </summary>
        public static Action<SCollectableCloudData> CharacterDataChangedEvent;

        // ===============================================================================================
        // DATA
        /// <summary> default data for the Inventory </summary>
        protected override Dictionary<string, object> m_Data { get; set; } = new Dictionary<string, object>() {
            { KEY_GOLDS,        0                                   },
            { KEY_GEMS,         0                                   },
            { KEY_XP,           0                                   },
            { KEY_TOTAL_XP,     0                                   },
            { KEY_CHARACTERS,   new List<SCollectableCloudData>()   },
            { KEY_SPELLS,       new List<SCollectableCloudData>()   },
            { KEY_RUNES,        new List<SCollectableCloudData>()   },
        };

        #endregion


        #region Data Manipulators

        public override void SetData(string key, object value, bool save = true)
        {
            object previousValue = m_Data[key];

            base.SetData(key, value, save);

            if (Enum.TryParse(key, out ECurrency currency))
            {
                // fire event that currency has changed
                CurrencyChangedEvent?.Invoke(currency, System.Convert.ToInt32(value));
            }
        }

        public void SetData(ECurrency currency, object value, bool save = true)
        {
            SetData(currency.ToString(), value, save);
        }

        protected override object Convert(Item item)
        {
            if (m_Data[item.Key].GetType() == typeof(List<SCollectableCloudData>))
            {
                return item.Value.GetAs<SCollectableCloudData[]>().ToList();
            }
   
            return base.Convert(item);
        }

        #endregion


        #region Collectables 

        public SCollectableCloudData GetCollectable(Enum collectable)
        {
            if (collectable.ToString() == "None")
                return new SCollectableCloudData(collectable);

            if (collectable.GetType() == typeof(ESpell))
            {
                return GetSpell((ESpell)collectable);
            }

            string key = GetCollectableKey(collectable);
            if (key == "")
                return default;

            int index = GetCollectableIndex(collectable);
            if (index == -1)
            {
                ErrorHandler.FatalError("Unable to find collectable " + collectable + " in collectables cloud data");
                return default;
            }

            return (m_Data[key] as List<SCollectableCloudData>)[index];
        }

        /// <summary>
        /// Set the value of a SCollectableCloudData
        /// </summary>
        /// <param name="characterCloudData"></param>
        public void SetCollectable(SCollectableCloudData collectableData, bool save = true)
        {
            string key = GetCollectableKey(collectableData.GetCollectable());
            int index = GetCollectableIndex(collectableData.GetCollectable());

            if (index >= 0)
                ((List<SCollectableCloudData>)m_Data[key])[index] = collectableData;
            else
            {
                ((List<SCollectableCloudData>)m_Data[key]).Add(collectableData);
                CollectableUnlockedEvent?.Invoke(collectableData);  
            }

            // Save & Fire event of the change
            if (save)
                Instance.SaveValue(key);

            // call event of the changes
            CollectableDataChangedEvent?.Invoke(collectableData);
        }

        /// <summary>
        /// Get index of a collectable (Character, Spell, ...) in the list of SCollectableCloudData
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public int GetCollectableIndex(Enum collectable)
        {
            string key = GetCollectableKey(collectable);
            if (key == "")
                return -1;

            var data = (List<SCollectableCloudData>)m_Data[key];
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].GetCollectable().Equals(collectable))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Get key matching type of collectable 
        /// </summary>
        /// <param name="collectable"></param>
        /// <returns></returns>
        public string GetCollectableKey(Enum collectable)
        {
            if (! InfoCollectables.ContainsKey(collectable.GetType()))
            {
                ErrorHandler.Error("Unable to match collectable " + collectable + " with any key");
                return "";
            }

            return InfoCollectables[collectable.GetType()].Key;
        }

        #endregion


        #region Currencies

        public int GetCurrency(ECurrency currency)
        {
            return (int)m_Data[currency.ToString()];
        }

        public void SetCurrency(ECurrency currency, int value)
        {
            SetData(currency.ToString(), value);
        }

        public void AddCurrency(ECurrency currency, int value)
        {
            SetData(currency.ToString(), GetCurrency(currency) + value);
        }

        #endregion


        #region Spells

        /// <summary>
        /// Get SSPellCloudData of a requested ESpell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public SCollectableCloudData GetSpell(ESpell spell)
        {
            if (SpellLoader.GetSpellData(spell, destroy: true).Linked)
            {
                var character = CharacterLoader.GetCharacterWithSpell(spell);
                return new SCollectableCloudData(spell, GetCollectable(character.Value).Level, 0);
            }

            int index = GetCollectableIndex(spell);
            if (index == -1)
            {
                ErrorHandler.Error("Unable to find spell " + spell + " in spell cloud data");
                return new SCollectableCloudData();
            }

            return ((List<SCollectableCloudData>)m_Data[KEY_SPELLS])[index];
        }

        #endregion


        #region Reset & Unlock

        public override void Reset(string key)
        {
            base.Reset(key);    

            switch (key)
            {
                case KEY_GOLDS:
                    m_Data[KEY_GOLDS] = 0;
                    CurrencyChangedEvent?.Invoke(ECurrency.Golds, 0);
                    break;

                case KEY_GEMS:
                    m_Data[KEY_GEMS] = 0;
                    CurrencyChangedEvent?.Invoke(ECurrency.Gems, 0);
                    break;

                case KEY_XP:
                    m_Data[KEY_XP] = 0;
                    CurrencyChangedEvent?.Invoke(ECurrency.Xp, 0);
                    break;

                case KEY_TOTAL_XP:
                    m_Data[KEY_TOTAL_XP] = 0;
                    CurrencyChangedEvent?.Invoke(ECurrency.TotalXp, 0);
                    break;

                case KEY_CHARACTERS:
                    m_Data[KEY_CHARACTERS] = new List<SCollectableCloudData>();
                    CheckMissingCollectable(typeof(ECharacter));

                    CharacterBuildsCloudData.SetSelectedCharacter(CharacterBuildsCloudData.DEFAULT_CHARACTER);
                    break;

                case KEY_SPELLS:
                    m_Data[KEY_SPELLS] = new List<SCollectableCloudData>();
                    CheckMissingCollectable(typeof(ESpell));

                    CharacterBuildsCloudData.Instance.Reset(CharacterBuildsCloudData.KEY_BUILDS);
                    break;

                case KEY_RUNES:
                    m_Data[KEY_RUNES] = new List<SCollectableCloudData>();
                    CheckMissingCollectable(typeof(ERune));

                    CharacterBuildsCloudData.Instance.Reset(CharacterBuildsCloudData.KEY_BUILDS);
                    break;

                default:
                    ErrorHandler.Warning("Unhandled key : " + key);
                    return;
            }

            SaveValue(key);
        }

        public override bool IsUnlockable(string key)
        {
            return new List<string>() { KEY_CHARACTERS, KEY_SPELLS, KEY_RUNES }.Contains(key);
        }

        public override void Unlock(string key, bool save = true)
        {
            base.Unlock(key, save);

            if (!IsUnlockable(key))
                return;

            switch (key)
            {
                case KEY_CHARACTERS:
                    m_Data[KEY_CHARACTERS] = new List<SCollectableCloudData>();
                    foreach (Enum collectable in Enum.GetValues(typeof(ECharacter)))
                    {
                        AddCollectableData(collectable, true);
                    }
                    break;

                case KEY_SPELLS:
                    m_Data[KEY_SPELLS] = new List<SCollectableCloudData>();
                    foreach (Enum collectable in Enum.GetValues(typeof(ESpell)))
                    {
                        AddCollectableData(collectable, true);
                    }
                    break;

                case KEY_RUNES:
                    m_Data[KEY_RUNES] = new List<SCollectableCloudData>();
                    foreach (Enum collectable in Enum.GetValues(typeof(ERune)))
                    {
                        AddCollectableData(collectable, true);
                    }
                    break;

                default:
                    ErrorHandler.Warning("Unhandled key : " + key);
                    return;
            }

            if (save)
                SaveValue(key);
        }

        #endregion


        #region Error Handler

        protected override void OnLoadingError(string key, Item item)
        {
            base.OnLoadingError(key, item);
        }

        #endregion


        #region Checkers

        void CheckCurrencies()
        {
            if ((int)m_Data[KEY_GOLDS] < 0)
            {
                ErrorHandler.Error("Golds (" + (int)m_Data[KEY_GOLDS] + ") < 0 : reseting back to 0");
                Reset(KEY_GOLDS);
            }

            if ((int)m_Data[KEY_GEMS] < 0)
            {
                ErrorHandler.Error("Gems (" + (int)m_Data[KEY_GEMS] + ") < 0 : reseting back to 0");
                Reset(KEY_GEMS);
            }

            if ((int)m_Data[KEY_XP] < 0)
            {
                ErrorHandler.Error("Xp (" + (int)m_Data[KEY_GEMS] + ") < 0 : reseting back to 0");
                Reset(KEY_XP);
            }

            if ((int)m_Data[KEY_TOTAL_XP] < 0)
            {
                ErrorHandler.Error(KEY_TOTAL_XP + " (" + (int)m_Data[KEY_GEMS] + ") < 0 : reseting back to 0");
                Reset(KEY_TOTAL_XP);
            }
        }

        /// <summary>
        /// Check that all current values from the database exists (in case of deletion from one version to another)
        /// </summary>
        void CheckAllNonExistingCollectables()
        {
            foreach (Type collectableType in COLLECTABLE_TYPES)
            {
                CheckNonExistingCollectable(collectableType);
            }
        }

        void CheckNonExistingCollectable(Type collectableType)
        {
            List<int> indexesToRemove = new List<int>();
            int index = 0;
            var list = (m_Data[InfoCollectables[collectableType].Key] as List<SCollectableCloudData>);
            foreach (var item in list)
            {
                if (! Enum.GetNames(collectableType).Contains(item.CollectableName))
                {
                    indexesToRemove.Add(index);
                }
                index++;
            }

            if (indexesToRemove.Count == 0)
                return;

            // remove unused values from the data
            ErrorHandler.Warning("Found " + indexesToRemove.Count + " data to remove");
            foreach (int idx in indexesToRemove)
            {
                list.RemoveAt(idx);
            }
            m_Data[InfoCollectables[collectableType].Key] = list;
            SaveValue(InfoCollectables[collectableType].Key);
        }

        void CheckAllMissingCollectables()
        {
            foreach (Type collectableType in COLLECTABLE_TYPES)
            {
                CheckMissingCollectable(collectableType);                
            }
        }

        /// <summary>
        /// Check if any collectable is missing (fix & save if any)
        /// </summary>
        /// <param name="collectableType"></param>
        /// <returns></returns>
        void CheckMissingCollectable(Type collectableType)
        {
            bool hasMissing = false;

            foreach (Enum collectable in Enum.GetValues(collectableType))
            {
                if (collectable is ESpell spell && SpellLoader.IsBossSpell(spell))
                {
                    continue;
                }

                bool addedData = AddCollectableData(collectable, GetInfos(collectable).DefaultData.Contains(collectable));
                if (addedData)
                    hasMissing = true;
            }
            
            if (hasMissing)
                SaveValue(InfoCollectables[collectableType].Key);
        }

        public bool AddCollectableData(Enum collectable, bool unlock)
        {
            if (IGNORED_COLLECTABLES.Contains(collectable))
                return false;

            // linked spell : level is dependent on the character
            if (collectable.GetType() == typeof(ESpell) && SpellLoader.GetSpellData(collectable.ToString(), destroy: true).Linked)
                return false;

            // boss spell : cant be unlocked
            if (CollectablesManagementData.IsBossSpell(collectable))
                return false;

            // already in data : skip
            if (GetCollectableIndex(collectable) >= 0)
                return false;

            ErrorHandler.Warning("Unable to find " + collectable + " in cloud data : adding it manually");

            // if unlocked by default check start level, otherwise start level is 0
            int startLevel = unlock ? CollectablesManagementData.GetStartLevel(collectable) : 0;

            // add new empty spell data, set save to false as we save the batch at the end
            SetCollectable(new SCollectableCloudData(collectable, startLevel, 0), false);

            return true;
        }

        #endregion


        #region Infos

        public SInfoCollectable GetInfos(Enum collectable)
        {
            if (! InfoCollectables.ContainsKey(collectable.GetType()))
            {
                ErrorHandler.Error("Unable to find SInfoCollectable for collectale " + collectable + " of type " + collectable.GetType());
                return default;
            }

            return InfoCollectables[collectable.GetType()];
        }

        #endregion


        #region Listeners

        protected override void CheckData() 
        {
            CheckCurrencies();
            CheckAllNonExistingCollectables();
            CheckAllMissingCollectables();
        }

        #endregion

    }
}