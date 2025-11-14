using Enums;
using Game.Loaders;
using System;
using System.Linq;

namespace Tools
{
    public static class PropertyHandler
    {
        #region Members

        #endregion


        #region Conversion

        public static bool ConvertSpecialPropertyName(ref string propertyName)
        {
            if (propertyName == "TickDamage")
            {
                propertyName = EHitCategory.Dot.ToString() + ESpellProperty.Damage;
                return true;
            }

            if (propertyName == "DurationTick")
            {
                propertyName = "Tick";
                return true;
            }

            // if is "Tick" property, name and icon are the same as regular value
            if (propertyName.StartsWith("Tick") && propertyName != "Tick")
            {
                propertyName = EHitCategory.Dot.ToString() + propertyName[4..];
                return true;
            }

            if (propertyName == "NProjectiles")
            {
                propertyName = "Projectiles";
                return true;
            }

            if (propertyName == "NWaves")
            {
                propertyName = "Waves";
                return true;
            }

            return false;
        }

        #endregion


        #region Property Name


        /// <summary>
        /// From the raw key value of a property, get Icon / Pretty Name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static (string, string) GetIconAndPrettyName(string key)
        {
            if (!TryExtractSpecialPropertyInfo(key, out string iconName, out string prettyName))
            {
                iconName = key;
                prettyName = TextHandler.Split(key);
            }

            if (prettyName.EndsWith(" Perc"))
                prettyName.Remove(0, prettyName.Count() - " Perc".Count());
            return (iconName, prettyName);
        }


        /// <summary>
        /// Uses a Property and its sepcial attributes to create a unique key name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="damageCategory"></param>
        /// <param name="hitCategory"></param>
        /// <param name="specialCondition"></param>
        /// <returns></returns>
        public static string FormatSpecialPropertyName(string propertyName, EDamageCategory? damageCategory = null, EHitCategory? hitCategory = null, string specialCondition = "")
        {
            if (damageCategory == null && hitCategory == null && specialCondition == "")
                return propertyName;

            return $"{propertyName}.{damageCategory}.{hitCategory}.{specialCondition}";
        }

        /// <summary>
        /// Check if the provided property is a property with special attributes
        /// </summary>
        /// <param name="key"></param>
        /// <param name="propertyName"></param>
        /// <param name="damageCategory"></param>
        /// <param name="hitCategory"></param>
        /// <param name="specialCondition"></param>
        /// <returns></returns>
        public static bool IsSpecialPropertyName(string key, out string propertyName, out string damageCategory, out string hitCategory, out string specialCondition)
        {
            propertyName = key;
            damageCategory = "";
            hitCategory = "";
            specialCondition = "";

            if (!key.Contains("."))
                return false;

            var split = key.Split(".");

            propertyName        = split[0];
            damageCategory      = split[1];
            hitCategory         = split[2];
            specialCondition    = split[3];

            return true;
        }

        /// <summary>
        /// Extract / Format name of the Icon and the Pretty Name of a property from its raw key form
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iconName"></param>
        /// <param name="prettyName"></param>
        /// <returns></returns>
        public static bool TryExtractSpecialPropertyInfo(string key, out string iconName, out string prettyName)
        {
            iconName = "";
            prettyName = "";

            if (!IsSpecialPropertyName(key, out string propertyName, out string damageCategory, out string hitCategory, out string specialCondition))
                return false;

            iconName = propertyName;
            if (specialCondition != "")
                iconName = specialCondition;
            else if (hitCategory != "" && hitCategory != EHitCategory.Direct.ToString() && hitCategory != EHitCategory.Piercing.ToString())
                iconName = hitCategory + propertyName;
            else if (damageCategory != "")
                iconName = damageCategory + propertyName;

            prettyName = TextHandler.Split(propertyName);
            if (specialCondition != "")
                prettyName = specialCondition + " " + TextHandler.Split(propertyName.Replace("Bonus", ""));
            else if (hitCategory != "")
                prettyName = hitCategory + " " + TextHandler.Split(propertyName.Replace("Bonus", ""));
            else if (damageCategory != "")
            {
                if (propertyName.Contains("Bonus"))
                {
                    prettyName = "Bonus " + damageCategory + " " + TextHandler.Split(propertyName.Replace("Bonus", ""));
                }
                else
                {
                    prettyName = damageCategory + " " + prettyName;
                }
            }

            return true;
        }

        #endregion


        #region Descriptions

        public static string GetDescription(string key, string value)
        {
            if (! IsSpecialPropertyName(key, out string propertyName, out string damageCategory, out string hitCategory, out string specialCondition))
            {
                propertyName = key;
                damageCategory = "";
                hitCategory = "";
                specialCondition = "";
            }

            string description = "";

            // === Resistances & Damage ===
            if (propertyName == EStateEffectProperty.Resistance.ToString())
            {
                string damageType = "Damage";
                if (hitCategory != "")
                    damageType = hitCategory + "Damage";
                else if (damageCategory != "")
                    damageType = damageCategory + "Damage";

                if (!float.TryParse(value, out float fValue))
                    return "";

                description = $"Increases your resistance to {TextHandler.FormatPropertyIcon(damageType, null, true, withPropertyName: true)} by {fValue:0} "
                            + $"(≈ {100 * (1 - 100 / (100 + fValue)):0}% final damage reduction).";
            }

            else if (propertyName == EStateEffectProperty.ResistanceFix.ToString())
            {
                string dmgText = "Damage";
                if (hitCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(hitCategory + "Damage", null, true, withPropertyName: true);
                else if (damageCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(damageCategory + "Damage", null, true, withPropertyName: true);

                if (!float.TryParse(value, out float fValue))
                    return "";

                description = $"Reduces all incoming {dmgText} by {fValue:0}. "
                            + "This reduction is applied before any percentage modifiers.";
            }

            else if (propertyName == EStateEffectProperty.Power.ToString())
            {
                string dmgText = "Damage";
                if (hitCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(hitCategory + "Damage", null, true, withPropertyName: true);
                else if (damageCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(damageCategory + "Damage", null, true, withPropertyName: true);

                if (!float.TryParse(value, out float fValue))
                    return "";
                description = $"Increases the base {dmgText} of your abilities by {fValue:0}%";
            }

            else if (propertyName == EStateEffectProperty.BonusDamage.ToString())
            {
                if (!float.TryParse(value, out float fValue))
                    return "";

                string dmgText = "Damage";
                if (specialCondition != "")
                    dmgText = specialCondition + " Damage";
                else if (hitCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(hitCategory + "Damage", null, true, withPropertyName: true);
                else if (damageCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(damageCategory + "Damage", null, true, withPropertyName: true);

                description = $"Increases your {dmgText} by {value:0}.";

                if (hitCategory != "" && hitCategory != "Direct")
                    description += "\n\n" + TextHandler.KEY_WORDS[hitCategory + "Damage"];
            }

            else if (propertyName == EStateEffectProperty.BonusDamagePerc.ToString())
            {
                if (!float.TryParse(value, out float fValue))
                    return "";

                string dmgText = "Damage";
                if (specialCondition != "")
                    dmgText = specialCondition + " Damage";
                else if (hitCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(hitCategory + "Damage", null, true, withPropertyName: true);
                else if (damageCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(damageCategory + "Damage", null, true, withPropertyName: true);

                description = $"Increases your final {dmgText} by {100 * fValue:0}%.";

                if (hitCategory != "" && hitCategory != "Direct")
                    description += "\n\n" + TextHandler.KEY_WORDS[hitCategory + "Damage_Key"];
            }

            else if (propertyName == EStateEffectProperty.Damage.ToString())
            {
                string dmgText = "damage";
                if (hitCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(hitCategory + "Damage", null, true, withPropertyName: true);
                else if (damageCategory != "")
                    dmgText = TextHandler.FormatPropertyIcon(damageCategory + "Damage", null, true, withPropertyName: true);

                description = $"Deals {value} {dmgText}.";
            }

            // === Duration & Stack Mechanics ===
            else if (propertyName == EStateEffectProperty.Duration.ToString())
                description = $"Total duration of the effect: {value} seconds.";

            else if (propertyName == EStateEffectProperty.MaxStacks.ToString())
                description = $"This effect can stack up to {value} times.";

            else if (propertyName == EStateEffectProperty.Tick.ToString())
                description = $"The interval between each tick of periodic effects: every {value} seconds.";

            // === Generic Bonuses ===
            else if (propertyName == EStateEffectProperty.SpeedBonus.ToString())
                description = $"Increases your movement speed by {value}.";

            else if (propertyName == EStateEffectProperty.Shield.ToString())
                description = $"Grants a shield that absorbs {value} damage.";

            else if (propertyName == EStateEffectProperty.BonusShieldPerc.ToString())
                description = $"Increases the strength of your shields by {value}%.";

            else if (propertyName == EStateEffectProperty.Lethality.ToString())
                description = $"Your {TextHandler.FormatPropertyIcon("ExecutionDamage", null, true, withPropertyName: true)} consiedere that the target is missing an extra {value}% health.";

            else if (propertyName == EStateEffectProperty.BonusLifeSteal.ToString())
                description = $"Increases life steal by {value}%.";

            else if (propertyName == EStateEffectProperty.AttackSpeed.ToString())
                description = $"Increases your attack speed by {value}%.";

            else if (propertyName == EStateEffectProperty.CastSpeed.ToString())
                description = $"Reduces your spell cast time by {value}%.";

            else if (propertyName == EStateEffectProperty.CooldownReduction.ToString())
                description = $"Reduces ability cooldowns by {value} seconds.";

            else if (propertyName == EStateEffectProperty.Haste.ToString())
            {
                if (!float.TryParse(value, out float fValue))
                    return "";
                description = $"Reduces ability cooldowns by {100 * (1 - 100 / (fValue + 100)):F2}% (={value:0} {propertyName}).";
            }

            // === Healing, Life Steal & Energy ===
            else if (propertyName == EStateEffectProperty.Heal.ToString())
                description = $"Heals the target for {value} health.";

            else if (propertyName == EStateEffectProperty.LifeSteal.ToString())
                description = $"Converts {value}% of damage dealt into healing.";

            else if (propertyName == EStateEffectProperty.BonusHeal.ToString())
                description = $"Increases your healing effects by {value} points.";

            else if (propertyName == EStateEffectProperty.BonusHealPerc.ToString())
                description = $"Increases all your healing effects by {value}%.";

            else if (propertyName == EStateEffectProperty.HealReduction.ToString())
                description = $"Reduces healing received by the target by {value}.";

            else if (propertyName == EStateEffectProperty.HealReductionPerc.ToString())
                description = $"Reduces healing received by the target by {value}%.";

            else if (propertyName == EStateEffectProperty.Hp.ToString())
                description = $"Increases your maximum health by {value}.";

            else if (propertyName == EStateEffectProperty.Energy.ToString())
                description = $"Restores {value} energy points.";

            else if (propertyName == EStateEffectProperty.PassiveEnergyGain.ToString())
                description = $"Regenerates {value} energy per second passively.";

            else if (propertyName == EStateEffectProperty.MaxEnergy.ToString())
                description = $"Maximum energy that can be stored on your character.";

            // === Periodic Effects (DoTs, HoTs, etc.) ===
            else if (propertyName == EStateEffectProperty.DotDamage.ToString())
                description = $"Deals {value} damage over time.";

            else if (propertyName == EStateEffectProperty.DotHeal.ToString())
                description = $"Heals {value} health every tick of time.";

            else if (propertyName == EStateEffectProperty.DotShield.ToString())
                description = $"Grants a shield absorbing {value} damage every tick of time.";

            else if (propertyName == EStateEffectProperty.DotEnergy.ToString())
                description = $"Restores {value} energy over time.";

            else if (propertyName == EStateEffectProperty.BonusSlowPerc.ToString())
                description = $"Increases the strength of your slowing effects by {value}%.";

            // === Spell Properties ===
            else if (propertyName == ESpellProperty.SpellTarget.ToString())
            {
                description = $"Determines how this spell selects its target (self, ally, enemy, or area).";
            }

            else if (propertyName == ESpellProperty.Cooldown.ToString())
                description = $"Time in seconds before the spell can be cast again ({value}s).";

            else if (propertyName == ESpellProperty.Duration.ToString())
                description = $"Specifies how long the spell effect lasts ({value}s).";

            else if (propertyName == ESpellProperty.NProjectiles.ToString() || propertyName == "Projectiles")
                description = $"Launches {value} projectiles at each wave.";

            else if (propertyName == ESpellProperty.NWaves.ToString() || propertyName == "Waves")
                description = $"Releases {value} waves during the spell’s duration.";

            else if (propertyName == ESpellProperty.DelayBetweenLaunches.ToString())
                description = $"Time delay between each projectile launch: {value}s.";

            else if (propertyName == ESpellProperty.DelayBetweenWaves.ToString())
                description = $"Time delay between each wave: {value}s.";

            else if (propertyName == ESpellProperty.ProjectileZoneSize.ToString())
                description = $"Defines the impact area size of each projectile ({value} units).";

            else if (propertyName == ESpellProperty.Size.ToString())
                description = $"Determines the area of effect size: {value} units radius.";

            else if (propertyName == ESpellProperty.DurationTick.ToString())
                description = $"Defines how frequently the spell effect triggers while active (every {value}s).";

            else if (propertyName == ESpellProperty.GrowSizeFactor.ToString())
                description = $"The spell’s area of effect grows by a factor of {value} over time.";

            else if (propertyName == ESpellProperty.Delay.ToString())
                description = $"Delay before the spell activates after being cast: {value}s.";

            else if (propertyName == ESpellProperty.ExecutionDamage.ToString())
                description = $"Deals {value} execution damage, scaling based on the target’s missing health.";

            else if (propertyName == ESpellProperty.Charges.ToString())
                description = $"This spell can be stored up to {value} times before needing to recharge.";

            else if (propertyName == ESpellProperty.Trajectory.ToString())
                description = $"Defines the flight path of the spell’s projectiles (e.g., straight, arc, homing).";

            else if (propertyName == ESpellProperty.AnimationTimer.ToString() || propertyName == "CastDuration")
                description = $"Determines how long the spell takes to be casted : {value}s.";

            else if (propertyName == ESpellProperty.Speed.ToString())
                description = $"Controls the projectile or effect travel speed ({value} units per second).";

            else if (propertyName == ESpellProperty.MaxHit.ToString())
                description = $"The spell can hit up to {value} targets before dissipating.";

            // === TYPES ===
            else if (propertyName == "Type")
            {
                description = $"This spell is a {value}";
            }

            // === TARGETS ===
            else if (propertyName == "Target")
            {
                if (value == "Auto")
                {
                    description = "The spell automatically locks onto its target.";
                }
                else if (value == "Fixed")
                {
                    description = "The spell targets a position at a fixed distance from the caster.";
                }
                else if (value == "Mirror")
                {
                    description = "The spell targets the caster’s symetrical position.";
                }
            }

            // === STATE EFFECTS ===
            else if (SpellLoader.IsStateEffect(propertyName))
                description = $"Consumes {value} stacks of {TextHandler.FormatStateEffectIcon(propertyName)} to be casted";

            // === SPECIAL STRINGS ===
            else if (propertyName == "MovementSpeed")
                description = $"Movement Speed of the character. The default speed is 1";

            else if (propertyName == "Health")
                description = $"Maximum HP of the character.";

            else if (propertyName == "IgnoreCC")
                description = $"This spell can be casted while beeing controlled ([Stun], [Frozen], ...)";

            else
                description = $"(Missing description for {propertyName})";

            return TextHandler.ReplaceKeyWords(description);
        }


        #endregion
    }
}