/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using OpenNos.Domain;
using OpenNos.GameObject.Networking;
using static OpenNos.Domain.BCardType;

namespace OpenNos.GameObject.Helpers
{
    public class DamageHelper
    {
        #region Members

        private static DamageHelper _instance;

        #endregion

        #region Properties

        public static DamageHelper Instance => _instance ?? (_instance = new DamageHelper());

        #endregion

        #region Methods

        /// <summary>
        /// Calculates the damage attacker inflicts defender
        /// </summary>
        /// <param name="attacker">The attacking Entity</param>
        /// <param name="defender">The defending Entity</param>
        /// <param name="skill">The used Skill</param>
        /// <param name="hitMode">reference to HitMode</param>
        /// <param name="onyxWings"></param>
        /// <returns>Damage</returns>
        public int CalculateDamage(BattleEntity attacker, BattleEntity defender, Skill skill, ref int hitMode, ref bool onyxWings, int rare = 0)
        {
            int[] GetAttackerBenefitingBuffs(CardType type, byte subtype)
            {
                int value1 = 0;
                int value2 = 0;
                int value3 = 0;
                int temp = 0;

                int[] tmp = GetBuff(attacker.Level, attacker.Buffs, attacker.BCards, type, subtype, BuffType.Good, ref temp);

                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                tmp = GetBuff(attacker.Level, attacker.Buffs, attacker.BCards, type, subtype, BuffType.Neutral, ref temp);

                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                tmp = GetBuff(defender.Level, defender.Buffs, defender.BCards, type, subtype, BuffType.Bad, ref temp);

                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                return new[] { value1, value2, value3, temp };
            }

            int[] GetDefenderBenefitingBuffs(CardType type, byte subtype)
            {
                int value1 = 0;
                int value2 = 0;
                int value3 = 0;
                int temp = 0;

                int[] tmp = GetBuff(defender.Level, defender.Buffs, defender.BCards, type, subtype, BuffType.Good, ref temp);

                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                tmp = GetBuff(defender.Level, defender.Buffs, defender.BCards, type, subtype, BuffType.Neutral, ref temp);

                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                tmp = GetBuff(attacker.Level, attacker.Buffs, attacker.BCards, type, subtype, BuffType.Bad, ref temp);

                value1 += tmp[0];
                value2 += tmp[1];
                value3 += tmp[2];

                return new[] { value1, value2, value3, temp };
            }

            int GetShellWeaponEffectValue(ShellWeaponEffectType effectType)
            {
                return attacker.ShellWeaponEffects?.Where(s => s.Effect == (byte)effectType).FirstOrDefault()?.Value ?? 0;
            }

            int GetShellArmorEffectValue(ShellArmorEffectType effectType)
            {
                return defender.ShellArmorEffects?.Where(s => s.Effect == (byte)effectType).FirstOrDefault()?.Value ?? 0;
            }

            if (skill != null)
            {
                if (SkillHelper.IsNoDamage(skill.SkillVNum)
                    || (skill.BCards.Any(b => b.Type == (byte)CardType.LightAndShadow
                        && b.SubType == (byte)AdditionalTypes.LightAndShadow.InflictDamageOnUndead / 10)
                        && defender.MapMonster?.Monster?.Race != 5))
                {
                    return 0;
                }

                attacker.BCards.AddRange(skill.BCards);
            }

            #region Basic Buff Initialisation

            attacker.Morale +=
                GetAttackerBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased)[0];
            attacker.Morale +=
                GetDefenderBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased)[0];
            defender.Morale +=
                GetDefenderBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased)[0];
            defender.Morale +=
                GetAttackerBenefitingBuffs(CardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased)[0];

            attacker.AttackUpgrade += (short)GetAttackerBenefitingBuffs(CardType.AttackPower,
                (byte)AdditionalTypes.AttackPower.AttackLevelIncreased)[0];
            attacker.AttackUpgrade += (short)GetDefenderBenefitingBuffs(CardType.AttackPower,
                (byte)AdditionalTypes.AttackPower.AttackLevelDecreased)[0];
            defender.DefenseUpgrade += (short)GetDefenderBenefitingBuffs(CardType.Defence,
                (byte)AdditionalTypes.Defence.DefenceLevelIncreased)[0];
            defender.DefenseUpgrade += (short)GetAttackerBenefitingBuffs(CardType.Defence,
                (byte)AdditionalTypes.Defence.DefenceLevelDecreased)[0];

            int[] attackerpercentdamage = GetAttackerBenefitingBuffs(CardType.RecoveryAndDamagePercent, 11);
            int[] defenderpercentdefense = GetDefenderBenefitingBuffs(CardType.RecoveryAndDamagePercent, 2);

            //int[] attackerpercentdamage2 = GetDefenderBenefitingBuffs(CardType.RecoveryAndDamagePercent, 11);
            //int[] defenderpercentdefense2 = GetAttackerBenefitingBuffs(CardType.RecoveryAndDamagePercent, 2);

            if (attackerpercentdamage[3] != 0)
            {
                return defender.HpMax / 100 * attackerpercentdamage[2];
            }

            if (defenderpercentdefense[3] != 0)
            {
                return defender.HpMax / 100 * Math.Abs(defenderpercentdefense[2]);
            }

            /*
             *
             * Percentage Boost categories:
             *  1.: Adds to Total Damage
             *  2.: Adds to Normal Damage
             *  3.: Adds to Base Damage
             *  4.: Adds to Defense
             *  5.: Adds to Element
             *
             * Buff Effects get added, whereas
             * Shell Effects get multiplied afterwards.
             *
             * Simplified Example on Defense (Same for Attack):
             *  - 1k Defense
             *  - Costume(+5% Defense)
             *  - Defense Potion(+20% Defense)
             *  - S-Defense Shell with 20% Boost
             *
             * Calculation:
             *  1000 * 1.25 * 1.2 = 1500
             *  Def    Buff   Shell Total
             *
             * Keep in Mind that after each step, one has
             * to round the current value down if necessary
             *
             * Static Boost categories:
             *  1.: Adds to Total Damage
             *  2.: Adds to Normal Damage
             *  3.: Adds to Base Damage
             *  4.: Adds to Defense
             *  5.: Adds to Element
             *
             */

            #region Definitions

            double boostCategory1 = 1;
            double boostCategory2 = 1;
            double boostCategory3 = 1;
            double boostCategory4 = 1;
            double boostCategory5 = 1;
            double shellBoostCategory1 = 1;
            double shellBoostCategory2 = 1;
            double shellBoostCategory3 = 1;
            double shellBoostCategory4 = 1;
            double shellBoostCategory5 = 1;
            int staticBoostCategory1 = 0;
            int staticBoostCategory2 = 0;
            int staticBoostCategory3 = 0;
            int staticBoostCategory4 = 0;
            int staticBoostCategory5 = 0;

            #endregion

            #region Type 1

            #region Static

            // None for now

            #endregion

            #region Boost

            boostCategory1 += GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.DamageIncreased)[0] / 100D;
            boostCategory1 += GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.DamageDecreased)[0] / 100D;
            boostCategory1 += GetAttackerBenefitingBuffs(CardType.Item, (byte)AdditionalTypes.Item.AttackIncreased)[0] / 100D;
            boostCategory1 += GetDefenderBenefitingBuffs(CardType.Item, (byte)AdditionalTypes.Item.DefenceIncreased)[0] / 100D;

            shellBoostCategory1 += GetShellWeaponEffectValue(ShellWeaponEffectType.PercentageTotalDamage) / 100D;

            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                boostCategory1 += GetAttackerBenefitingBuffs(CardType.SpecializationBuffResistance, (byte)AdditionalTypes.SpecializationBuffResistance.IncreaseDamageInPVP)[0] / 100D;
                boostCategory1 += GetAttackerBenefitingBuffs(CardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.AttackIncreasedInPVP)[0] / 100D;

                shellBoostCategory1 += GetShellWeaponEffectValue(ShellWeaponEffectType.PercentageDamageInPVP) / 100D;
            }

            #endregion

            #endregion

            #region Type 2

            #region Static

            // None for now

            #endregion

            #region Boost

            boostCategory2 +=
                GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.DamageDecreased)
                    [0] / 100D;

            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                boostCategory2 += GetDefenderBenefitingBuffs(CardType.SpecializationBuffResistance,
                                      (byte)AdditionalTypes.SpecializationBuffResistance.DecreaseDamageInPVP)[0]
                                  / 100D;
                boostCategory2 += GetDefenderBenefitingBuffs(CardType.LeonaPassiveSkill,
                                      (byte)AdditionalTypes.LeonaPassiveSkill.AttackDecreasedInPVP)[0] / 100D;
            }

            #endregion

            #endregion

            #region Type 3

            #region Static

            staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksIncreased)[0];
            staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksDecreased)[0];
            staticBoostCategory3 += GetShellWeaponEffectValue(ShellWeaponEffectType.DamageImproved);

            if (attacker.Invisible)
            {
                staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.LightAndShadow, (byte)AdditionalTypes.LightAndShadow.AdditionalDamageWhenHidden)[0];
            }

            #endregion

            #region Soft-Damage

            int[] soft = GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                (byte)AdditionalTypes.IncreaseDamage.IncreasingPropability);
            int[] skin = GetAttackerBenefitingBuffs(CardType.EffectSummon,
                (byte)AdditionalTypes.EffectSummon.DamageBoostOnHigherLvl);
            if (attacker.Level < defender.Level)
            {
                soft[0] += skin[0];
                soft[1] += skin[1];
            }

            if (ServerManager.RandomNumber() < soft[0])
            {
                boostCategory3 += soft[1] / 100D;
                if (attacker.EntityType.Equals(EntityType.Player))
                {
                    attacker.Session?.CurrentMapInstance?.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Player, attacker.Session.Character.CharacterId, 15));
                }
            }

            #endregion

            #endregion

            #region Type 4

            #region Static

            staticBoostCategory4 += GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.AllIncreased)[0];
            staticBoostCategory4 += GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.AllDecreased)[0];

            #endregion

            #region Boost

            boostCategory4 += GetDefenderBenefitingBuffs(CardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DefenceIncreased)[0] / 100D;
            boostCategory4 += GetAttackerBenefitingBuffs(CardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DefenceReduced)[0] / 100D;

            shellBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.PercentageTotalDefence) / 100D;

            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                boostCategory4 += GetDefenderBenefitingBuffs(CardType.LeonaPassiveSkill,
                                      (byte)AdditionalTypes.LeonaPassiveSkill.DefenceIncreasedInPVP)[0] / 100D;
                boostCategory4 += GetAttackerBenefitingBuffs(CardType.LeonaPassiveSkill,
                                      (byte)AdditionalTypes.LeonaPassiveSkill.DefenceDecreasedInPVP)[0] / 100D;
                shellBoostCategory4 -=
                    GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesPercentageEnemyDefenceInPVP) / 100D;
                shellBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.PercentageAllPVPDefence) / 100D;
            }

            int[] def1 = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceAllIncreased);

            if (ServerManager.RandomNumber() < def1[0])
            {
                boostCategory3 += def1[1] / 100D;
            }

            #endregion

            #endregion

            #region Type 5

            #region Static

            staticBoostCategory5 +=
                GetAttackerBenefitingBuffs(CardType.Element, (byte)AdditionalTypes.Element.AllIncreased)[0];
            staticBoostCategory5 +=
                GetDefenderBenefitingBuffs(CardType.Element, (byte)AdditionalTypes.Element.AllDecreased)[0];
            staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedElementalProperties);

            #endregion

            #region Boost

            // Nothing for now

            #endregion

            #endregion

            #region All Type Class Dependant

            int[] def2 = null;

            switch (attacker.AttackType)
            {
                case AttackType.Melee:
                    def2 = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceMeleeIncreased);
                    boostCategory1 += GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MeleeIncreased)[0] / 100D;
                    boostCategory1 += GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MeleeDecreased)[0] / 100D;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0];
                    staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksDecreased)[0];
                    staticBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.CloseDefence);
                    staticBoostCategory4 += GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeIncreased)[0];
                    staticBoostCategory4 += GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeDecreased)[0];
                    break;

                case AttackType.Range:
                    def2 = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceRangedIncreased);
                    boostCategory1 += GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.RangedIncreased)[0] / 100D;
                    boostCategory1 += GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.RangedDecreased)[0] / 100D;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased)[0];
                    staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.RangedAttacksDecreased)[0];
                    staticBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.DistanceDefence);
                    staticBoostCategory4 += GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.RangedIncreased)[0];
                    staticBoostCategory4 += GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.RangedDecreased)[0];
                    break;

                case AttackType.Magical:
                    def2 = GetAttackerBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceRangedIncreased);
                    boostCategory1 += GetAttackerBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MagicalIncreased)[0] / 100D;
                    boostCategory1 += GetDefenderBenefitingBuffs(CardType.Damage, (byte)AdditionalTypes.Damage.MagicalDecreased)[0] / 100D;
                    staticBoostCategory3 += GetAttackerBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased)[0];
                    staticBoostCategory3 += GetDefenderBenefitingBuffs(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.MagicalAttacksDecreased)[0];
                    staticBoostCategory4 += GetShellArmorEffectValue(ShellArmorEffectType.MagicDefence);
                    staticBoostCategory4 += GetDefenderBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MagicalIncreased)[0];
                    staticBoostCategory4 += GetAttackerBenefitingBuffs(CardType.Defence, (byte)AdditionalTypes.Defence.MagicalDecreased)[0];
                    break;
            }

            if (def2 != null)
            {
                def1[0] += def2[0];
                def1[1] += def2[1];
            }

            #endregion

            #region Softdef finishing

            if (ServerManager.RandomNumber() < def1[0])
            {
                boostCategory3 += def1[1] / 100D;
            }

            #endregion

            #region Element Dependant

            switch (attacker.Element)
            {
                case 1:
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.FireIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.FireDecreased)[0];
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.FireResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.FireIncreased)[0];
                    defender.FireResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.FireDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyFireResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedFireResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedFireProperties);
                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                                          (byte)AdditionalTypes.IncreaseDamage.FireIncreased)[0] / 100D;
                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.FireIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.FireDecreased)[0];
                    break;

                case 2:
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.WaterIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.WaterDecreased)[0];
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.WaterResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.WaterIncreased)[0];
                    defender.WaterResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.WaterDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyWaterResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedWaterResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedWaterProperties);
                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                                          (byte)AdditionalTypes.IncreaseDamage.WaterIncreased)[0] / 100D;
                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.WaterIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.WaterDecreased)[0];
                    break;

                case 3:
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.LightIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.LightDecreased)[0];
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.LightResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.LightIncreased)[0];
                    defender.LightResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.LightDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyLightResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedLightResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedLightProperties);
                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                                          (byte)AdditionalTypes.IncreaseDamage.LightIncreased)[0] / 100D;
                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.LightIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.Light5Decreased)[0];
                    break;

                case 4:
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.AllDecreased)[0];
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.DarkIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.ElementResistance,
                        (byte)AdditionalTypes.ElementResistance.DarkDecreased)[0];
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.AllDecreased)[0];
                    defender.ShadowResistance += GetDefenderBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.DarkIncreased)[0];
                    defender.ShadowResistance += GetAttackerBenefitingBuffs(CardType.EnemyElementResistance,
                        (byte)AdditionalTypes.EnemyElementResistance.DarkDecreased)[0];
                    if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                        && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
                    {
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyDarkResistanceInPVP);
                        defender.FireResistance -=
                            GetShellWeaponEffectValue(ShellWeaponEffectType.ReducesEnemyAllResistancesInPVP);
                    }

                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedDarkResistence);
                    defender.FireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
                    staticBoostCategory5 += GetShellWeaponEffectValue(ShellWeaponEffectType.IncreasedDarkProperties);
                    boostCategory5 += GetAttackerBenefitingBuffs(CardType.IncreaseDamage,
                                          (byte)AdditionalTypes.IncreaseDamage.DarkIncreased)[0] / 100D;
                    staticBoostCategory5 += GetAttackerBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.DarkIncreased)[0];
                    staticBoostCategory5 += GetDefenderBenefitingBuffs(CardType.Element,
                        (byte)AdditionalTypes.Element.DarkDecreased)[0];
                    break;
            }

            #endregion

            #endregion

            #region Attack Type Related Variables

            switch (attacker.AttackType)
            {
                case AttackType.Melee:
                    defender.Defense = defender.MeleeDefense;
                    defender.ArmorDefense = defender.ArmorMeleeDefense;
                    defender.Dodge = defender.MeleeDefenseDodge;
                    break;

                case AttackType.Range:
                    defender.Defense = defender.RangeDefense;
                    defender.ArmorDefense = defender.ArmorRangeDefense;
                    defender.Dodge = defender.RangeDefenseDodge;
                    break;

                case AttackType.Magical:
                    defender.Defense = defender.MagicalDefense;
                    defender.ArmorDefense = defender.ArmorMagicalDefense;
                    break;
            }

            #endregion

            #region Too Near Range Attack Penalty (boostCategory2)

            if (attacker.AttackType == AttackType.Range && Map.GetDistance(
                    new MapCell { X = attacker.PositionX, Y = attacker.PositionY },
                    new MapCell { X = defender.PositionX, Y = defender.PositionY }) < 4)
            {
                boostCategory2 -= 0.3;
            }

            #endregion

            #region Morale and Dodge

            attacker.Morale -= defender.Morale;
            double missChance = 0;

            if (attacker.AttackType != AttackType.Magical)
            {
                int hitRate = attacker.Hitrate + attacker.Morale;

                hitRate += GetAttackerBenefitingBuffs(CardType.Target, (byte)AdditionalTypes.Target.AllHitRateIncreased)[0];

                double multiplier = defender.Dodge / (hitRate > 1 ? hitRate : 1);

                if (multiplier > 5)
                {
                    multiplier = 5;
                }

                missChance = (-0.25 * Math.Pow(multiplier, 3)) - (0.57 * Math.Pow(multiplier, 2)) + (25.3 * multiplier) - 1.41;

                if (missChance <= 1)
                {
                    missChance = 1;
                }

                int value;

                value = GetAttackerBenefitingBuffs(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance)[0];

                if (value != 0 && missChance > 100 - value)
                {
                    missChance = 100 - value;
                }

                value = GetDefenderBenefitingBuffs(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance)[0];

                if (value != 0 && missChance < 100 - value)
                {
                    missChance = 100 - value;
                }

                value = GetDefenderBenefitingBuffs(CardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AlwaysDodgeProbability)[0];

                if (value != 0 && missChance < value)
                {
                    missChance = value;
                }
            }

            int bonus = 0;
            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                switch (attacker.AttackType)
                {
                    case AttackType.Melee:
                        bonus += GetShellArmorEffectValue(ShellArmorEffectType.CloseDefenceDodgeInPVP);
                        break;

                    case AttackType.Range:
                        bonus += GetShellArmorEffectValue(ShellArmorEffectType.DistanceDefenceDodgeInPVP);
                        break;

                    case AttackType.Magical:
                        bonus += GetShellArmorEffectValue(ShellArmorEffectType.IgnoreMagicDamage);
                        break;
                }

                bonus += GetShellArmorEffectValue(ShellArmorEffectType.DodgeAllAttacksInPVP);
            }

            if (!defender.Invincible && ServerManager.RandomNumber() - bonus < missChance)
            {
                hitMode = 1;
                return 0;
            }

            #endregion

            #region Base Damage

            int baseDamage = ServerManager.RandomNumber(attacker.DamageMinimum, attacker.DamageMaximum + 1);
            int weaponDamage = ServerManager.RandomNumber(attacker.WeaponDamageMinimum, attacker.WeaponDamageMaximum + 1);

            #region Attack Level Calculation

            int[] atklvlfix = GetDefenderBenefitingBuffs(CardType.CalculatingLevel,
                (byte)AdditionalTypes.CalculatingLevel.CalculatedAttackLevel);
            int[] deflvlfix = GetAttackerBenefitingBuffs(CardType.CalculatingLevel,
                (byte)AdditionalTypes.CalculatingLevel.CalculatedDefenceLevel);

            if (atklvlfix[3] != 0)
            {
                attacker.AttackUpgrade = (short)atklvlfix[0];
            }

            if (deflvlfix[3] != 0)
            {
                attacker.DefenseUpgrade = (short)deflvlfix[0];
            }

            attacker.AttackUpgrade -= defender.DefenseUpgrade;

            if (attacker.AttackUpgrade < -10)
            {
                attacker.AttackUpgrade = -10;
            }
            else if (attacker.AttackUpgrade > ServerManager.Instance.Configuration.MaxUpgrade)
            {
                attacker.AttackUpgrade = ServerManager.Instance.Configuration.MaxUpgrade;
            }

            switch (attacker.AttackUpgrade)
            {
                case 0:
                    weaponDamage += 0;
                    break;

                case 1:
                    weaponDamage += (int)(weaponDamage * 0.1);
                    break;

                case 2:
                    weaponDamage += (int)(weaponDamage * 0.2125);
                    break;

                case 3:
                    weaponDamage += (int)(weaponDamage * 0.335);
                    break;

                case 4:
                    weaponDamage += (int)(weaponDamage * 0.475);
                    break;

                case 5:
                    weaponDamage += (int)(weaponDamage * 0.65);
                    break;

                case 6:
                    weaponDamage += (int)(weaponDamage * 0.7125);
                    break;

                case 7:
                    weaponDamage += (int)(weaponDamage * 0.8);
                    break;

                case 8:
                    weaponDamage += (int)(weaponDamage * 0.975);
                    break;

                case 9:
                    weaponDamage += (int)(weaponDamage * 1.025);
                    break;

                case 10:
                    weaponDamage += (int)(weaponDamage * 1.1);
                    break;

                    //default:
                    //    if (attacker.AttackUpgrade > 0)
                    //    {
                    //        weaponDamage *= attacker.AttackUpgrade / 5;
                    //    }

                    //    break;
            }

            #endregion

            baseDamage = (int)((int)((baseDamage + staticBoostCategory3 + weaponDamage + 15) * boostCategory3)
                                * shellBoostCategory3);

            #endregion

            #region Defense

            switch (attacker.AttackUpgrade)
            {
                //default:
                //    if (attacker.AttackUpgrade < 0)
                //    {
                //        defender.ArmorDefense += defender.ArmorDefense / 5;
                //    }

                //break;

                case -10:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 1.0);
                    break;

                case -9:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.8);
                    break;

                case -8:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.7);
                    break;

                case -7:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.55);
                    break;

                case -6:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.37);
                    break;

                case -5:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.32);
                    break;

                case -4:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.22);
                    break;

                case -3:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.18);
                    break;

                case -2:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.10);
                    break;

                case -1:
                    defender.ArmorDefense += (int)(defender.ArmorDefense * 0.1);
                    break;

                case 0:
                    defender.ArmorDefense += 0;
                    break;
            }

            int defence = (int)((int)((defender.Defense + defender.ArmorDefense + staticBoostCategory4) * boostCategory4) * shellBoostCategory4);

            int[] ignoreDefence = GetAttackerBenefitingBuffs(CardType.StealBuff, (byte)AdditionalTypes.StealBuff.IgnoreDefenceChance);

            if (ServerManager.RandomNumber() < ignoreDefence[0])
            {
                defence -= (int)((defence / 100D) * ignoreDefence[1]);
            }

            if (defender.HasBuff(CardType.SpecialDefence,
                    (byte)AdditionalTypes.SpecialDefence.NoDefence)
                || GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                    (byte)AdditionalTypes.SpecialDefence.AllDefenceNullified)[3] != 0
                || (GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                        (byte)AdditionalTypes.SpecialDefence.MeleeDefenceNullified)[3] != 0
                    && attacker.AttackType.Equals(AttackType.Melee))
                || (GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                        (byte)AdditionalTypes.SpecialDefence.RangedDefenceNullified)[3] != 0
                    && attacker.AttackType.Equals(AttackType.Range))
                || (GetAttackerBenefitingBuffs(CardType.SpecialDefence,
                        (byte)AdditionalTypes.SpecialDefence.MagicDefenceNullified)[3] != 0
                    && attacker.AttackType.Equals(AttackType.Magical)))
            {
                defence = 0;
            }

            #endregion

            #region Normal Damage

            int normalDamage = (int)((int)((baseDamage + staticBoostCategory2 - defence) * boostCategory2)
                                      * shellBoostCategory2);

            if (normalDamage < 0)
            {
                normalDamage = 0;
            }

            #endregion

            #region Crit Damage

            attacker.CritChance += GetShellWeaponEffectValue(ShellWeaponEffectType.CriticalChance);
            attacker.CritChance -= GetShellArmorEffectValue(ShellArmorEffectType.ReducedCritChanceRecive);
            attacker.CritChance += GetAttackerBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.InflictingIncreased)[0];
            attacker.CritChance += GetDefenderBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.ReceivingIncreased)[0];

            int receivingChancePercent = GetDefenderBenefitingBuffs(CardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.ReceivingChancePercent)[0];

            if (receivingChancePercent < 0)
            {
                if (attacker.CritChance < -receivingChancePercent)
                {
                    attacker.CritChance = -receivingChancePercent;
                }
            }

            if (defender.HasBuff(CardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.AlwaysReceives))
            {
                attacker.CritChance = 100;
            }

            if (defender.HasBuff(CardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.NeverReceives))
            {
                attacker.CritChance = 0;
            }

            attacker.CritRate += GetShellWeaponEffectValue(ShellWeaponEffectType.CriticalDamage);
            attacker.CritRate += GetAttackerBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreased)[0];
            attacker.CritRate += GetDefenderBenefitingBuffs(CardType.Critical, (byte)AdditionalTypes.Critical.DamageFromCriticalIncreased)[0];

            if (defender.CellonOptions != null)
            {
                attacker.CritRate -= defender.CellonOptions.Where(s => s.Type == CellonOptionType.CritReduce).Sum(s => s.Value);
            }

            int[] reduceCriticalReceivedChance = GetDefenderBenefitingBuffs(CardType.StealBuff, (byte)AdditionalTypes.StealBuff.ReduceCriticalReceivedChance);

            if (ServerManager.RandomNumber() < reduceCriticalReceivedChance[0])
            {
                attacker.CritRate -= (int)((attacker.CritRate / 100D) * reduceCriticalReceivedChance[1]);
            }

            if (ServerManager.RandomNumber() < attacker.CritChance && attacker.AttackType != AttackType.Magical)
            {
                double multiplier = attacker.CritRate / 100D;

                if (multiplier > 3)
                {
                    multiplier = 3;
                }

                normalDamage += (int)(normalDamage * multiplier);

                hitMode = 3;
            }

            #endregion

            #region Fairy Damage

            int fairyDamage = (int)((baseDamage + 100) * attacker.ElementRate / 100D);

            #endregion

            #region Elemental Damage Advantage

            double elementalBoost = 0;

            switch (attacker.Element)
            {
                case 0:
                    break;

                case 1:
                    defender.Resistance = defender.FireResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3; // Damage vs no element
                            break;

                        case 1:
                            elementalBoost = 1; // Damage vs fire
                            break;

                        case 2:
                            elementalBoost = 2; // Damage vs water
                            break;

                        case 3:
                            elementalBoost = 1; // Damage vs light
                            break;

                        case 4:
                            elementalBoost = 1.5; // Damage vs darkness
                            break;
                    }

                    break;

                case 2:
                    defender.Resistance = defender.WaterResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3;
                            break;

                        case 1:
                            elementalBoost = 2;
                            break;

                        case 2:
                            elementalBoost = 1;
                            break;

                        case 3:
                            elementalBoost = 1.5;
                            break;

                        case 4:
                            elementalBoost = 1;
                            break;
                    }

                    break;

                case 3:
                    defender.Resistance = defender.LightResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3;
                            break;

                        case 1:
                            elementalBoost = 1.5;
                            break;

                        case 2:
                        case 3:
                            elementalBoost = 1;
                            break;

                        case 4:
                            elementalBoost = 3;
                            break;
                    }

                    break;

                case 4:
                    defender.Resistance = defender.ShadowResistance;
                    switch (defender.Element)
                    {
                        case 0:
                            elementalBoost = 1.3;
                            break;

                        case 1:
                            elementalBoost = 1;
                            break;

                        case 2:
                            elementalBoost = 1.5;
                            break;

                        case 3:
                            elementalBoost = 3;
                            break;

                        case 4:
                            elementalBoost = 1;
                            break;
                    }

                    break;
            }

            if (skill?.Element == 0 || (skill?.Element != attacker.Element && attacker.EntityType == EntityType.Player))
            {
                elementalBoost = 0;
            }

            #endregion

            #region Elemental Damage

            int elementalDamage =
                (int)((int)((int)((int)((staticBoostCategory5 + fairyDamage) * elementalBoost)
                                     * (1 - (defender.Resistance / 100D))) * boostCategory5) * shellBoostCategory5);

            if (elementalDamage < 0)
            {
                elementalDamage = 0;
            }

            #endregion

            #region Total Damage

            int totalDamage =
                (int)((int)((normalDamage + elementalDamage + attacker.Morale + staticBoostCategory1)
                              * boostCategory1) * shellBoostCategory1);

            if ((attacker.EntityType == EntityType.Player || attacker.EntityType == EntityType.Mate)
                && (defender.EntityType == EntityType.Player || defender.EntityType == EntityType.Mate))
            {
                totalDamage /= 2;
            }

            if (defender.EntityType == EntityType.Monster || defender.EntityType == EntityType.NPC)
            {
                totalDamage -= GetMonsterDamageBonus(defender.Level);
            }

            if (totalDamage < 4)
            {
                totalDamage = ServerManager.RandomNumber(1, 6);
            }

            if (attacker.EntityType == EntityType.Monster || attacker.EntityType == EntityType.NPC)
            {
                totalDamage += GetMonsterDamageBonus(attacker.Level);
            }

            #endregion

            #region Onyx Wings

            int[] onyxBuff = GetAttackerBenefitingBuffs(CardType.StealBuff,
                (byte)AdditionalTypes.StealBuff.ChanceSummonOnyxDragon);
            if (onyxBuff[0] > ServerManager.RandomNumber())
            {
                onyxWings = true;
            }

            #endregion

            int[] hitAttacker = GetDefenderBenefitingBuffs(CardType.SecondSPCard, (byte)AdditionalTypes.SecondSPCard.HitAttacker);

            if (ServerManager.RandomNumber() < hitAttacker[0])
            {
                attacker.AddBuff((short)hitAttacker[1], defender);
            }

            int[] chanceAllDecreased = GetDefenderBenefitingBuffs(CardType.Block, (byte)AdditionalTypes.Block.ChanceAllDecreased);

            if (ServerManager.RandomNumber() < -chanceAllDecreased[0])
            {
                totalDamage += (int)((totalDamage / 100D) * -chanceAllDecreased[1]);

                if (totalDamage < 1)
                {
                    totalDamage = 1;
                }
            }

            return totalDamage;
        }

        private static int[] GetBuff(byte level, List<Buff> buffs, List<BCard> bcards, CardType type,
            byte subtype, BuffType btype, ref int count)
        {
            int value1 = 0;
            int value2 = 0;
            int value3 = 0;

            IEnumerable<BCard> cards;

            if (bcards != null && btype.Equals(BuffType.Good))
            {
                cards = subtype % 10 == 1
                    ? bcards.Where(s =>
                        s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10)) && s.FirstData >= 0)
                    : bcards.Where(s =>
                        s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10))
                        && (s.FirstData <= 0 || s.ThirdData < 0));

                foreach (BCard entry in cards)
                {
                    if (entry.IsLevelScaled)
                    {
                        if (entry.IsLevelDivided)
                        {
                            value1 += level / entry.FirstData;
                        }
                        else
                        {
                            value1 += entry.FirstData * level;
                        }
                    }
                    else
                    {
                        value1 += entry.FirstData;
                    }

                    value2 += entry.SecondData;
                    value3 += entry.ThirdData;
                    count++;
                }
            }

            if (buffs != null)
            {
                foreach (Buff buff in buffs.Where(b => b.Card.BuffType.Equals(btype)))
                {
                    cards = subtype % 10 == 1
                        ? buff.Card.BCards.Where(s =>
                            s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10))
                            && (s.CastType != 1 || (s.CastType == 1
                                                 && buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now))
                            && s.FirstData >= 0)
                        : buff.Card.BCards.Where(s =>
                            s.Type.Equals((byte)type) && s.SubType.Equals((byte)(subtype / 10))
                            && (s.CastType != 1 || (s.CastType == 1
                                                 && buff.Start.AddMilliseconds(buff.Card.Delay * 100) < DateTime.Now))
                            && s.FirstData <= 0);

                    foreach (BCard entry in cards)
                    {
                        if (entry.IsLevelScaled)
                        {
                            if (entry.IsLevelDivided)
                            {
                                value1 += buff.Level / entry.FirstData;
                            }
                            else
                            {
                                value1 += entry.FirstData * buff.Level;
                            }
                        }
                        else
                        {
                            value1 += entry.FirstData;
                        }

                        value2 += entry.SecondData;
                        value3 += entry.ThirdData;
                        count++;
                    }
                }
            }

            return new[] { value1, value2, value3 };
        }

        private static int GetMonsterDamageBonus(byte level)
        {
            if (level < 45)
            {
                return 0;
            }
            else if (level < 55)
            {
                return level;
            }
            else if (level < 60)
            {
                return level * 2;
            }
            else if (level < 65)
            {
                return level * 3;
            }
            else if (level < 70)
            {
                return level * 4;
            }
            else
            {
                return level * 5;
            }
        }

        #endregion
    }
}