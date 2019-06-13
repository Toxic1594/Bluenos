﻿/*
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
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.Domain
{
    //TODO IMPROVE NAMING
    public enum ShellOptionType
    {
        /* WEAPON OPTIONS */
        // INCREASE DAMAGES
        IncreaseDamage = 1,
        SDamagePercentage = 2,

        // DEBUFFS
        MinorBleeding = 3,
        Bleeding = 4,
        SeriousBleeding = 5,
        Blackout = 6,
        Frozen = 7,
        DeadlyBlackout = 8,

        // PVE OPTIONS
        IncreaseDamageOnPlants = 9,
        IncreaseDamageOnAnimals = 10,
        IncreaseDamageOnDemons = 11,
        IncreaseDamagesOnZombies = 12,
        IncreaseDamagesOnSmallAnimals = 13,
        SDamagePercentageOnGiantMonsters = 14,

        // CHARACTER BONUSES
        IncreaseCritChance = 15,
        IncreaseCritDamages = 16,
        ProtectWandSkillInterruption = 17,
        IncreaseFireElement = 18,
        IncreaseWaterElement = 19,
        IncreaseLightElement = 20,
        IncreaseDarknessElement = 21,
        SIncreaseAllElements = 22,
        ReduceMpConsumption = 23,
        HpRegenerationOnKill = 24,
        MpRegenerationOnKill = 25,

        // SP BONUSES
        AttackSl = 26,
        DefenseSl = 27,
        ElementSl = 28,
        HpMpSl = 29,
        SGlobalSl = 30,

        // PVE RATES INCREASE
        GoldPercentage = 31,
        XpPercentage = 32,
        JobXpPercentage = 33,

        // PVP OPTIONS
        PvpDamagePercentage = 34,
        PvpEnemyDefenseDecreased = 35,
        PvpResistanceDecreasedFire = 36,
        PvpResistanceDecreasedWater = 37,
        PvpResistanceDecreasedLight = 38,
        PvpResistanceDecreasedDark = 39,
        PvpResistanceDecreasedAll = 40,
        PvpAlwaysHit = 41,
        PvpDamageProbabilityPercentage = 42,
        PvpWithdrawMp = 43,

        // R8 CHAMPION OPTIONS
        PvpIgnoreResistanceFire = 44,
        PvpIgnoreResistanceWater = 45,
        PvpIgnoreResistanceLight = 46,
        PvpIgnoreResistanceDark = 47,
        RegenSpecialistPointPerKill = 48,
        IncreasePrecision = 49,
        IncreaseConcentration = 50,

        /* ARMOR OPTIONS */

        // DEFENSE INCREASE
        CloseCombatDefense = 51,
        LongRangeDefense = 52,
        MagicalDefense = 53,
        SDefenseAllPercentage = 54,

        // ANTI-DEBUFFS
        ReducedMinorBleeding = 55,
        ReducedSeriousBleeding = 56,
        ReducedAllBleeding = 57,
        ReducedSmallBlackout = 58,
        ReducedAllBlackout = 59,
        ReducedHandOfDeath = 60,
        ReducedFrozenChance = 61,
        ReducedBlindChance = 62,
        ReducedArrestationChance = 63,
        ReducedDefenseReduction = 64,
        ReducedShockChance = 65,
        ReducedRigidityChance = 66,
        SReducedAllNegative = 67,

        // CHARACTER BONUSES
        OnRestHpRecoveryPercentage = 68,
        NaturalHpRecoveryPercentage = 69,
        OnRestMpRecoveryPercentage = 70,
        NaturalMpRecoveryPercentage = 71,
        SOnAttackRecoveryPercentage = 72,
        ReduceCriticalChance = 73,

        // RESISTANCE INCREASE
        FireResistanceIncrease = 74,
        WaterResistanceIncrease = 75,
        LightResistanceIncrease = 76,
        DarkResistanceIncrease = 77,
        SIncreaseAllResistance = 78,

        // VARIOUS PVE BONUSES
        DignityLossReduced = 79,
        PointConsumptionReduced = 80,
        MiniGameProductionIncreased = 81,
        FoodHealing = 82,

        // PVP BONUSES
        PvpDefensePercentage = 83,
        PvpDodgeClose = 84,
        PvpDodgeRanged = 85,
        PvpDodgeMagic = 86,
        SPvpDodgeAll = 87,
        PvpMpProtect = 88,

        // R8 OPTIONS
        ChampionPvpIgnoreAttackFire = 89,
        ChampionPvpIgnoreAttackWater = 90,
        ChampionPvpIgnoreAttackLight = 91,
        ChampionPvpIgnoreAttackDark = 92,
        AbsorbDamagePercentageA = 93,
        AbsorbDamagePercentageB = 94,
        AbsorbDamagePercentageC = 95,
        IncreaseEvasiveness = 96
    }
}