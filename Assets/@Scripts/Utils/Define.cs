using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Define 
{
    public enum ToastPosition
    {
        Top,
        Middle
    }

    public enum TextFont
    {
        None,
        Main,
        Sub
    }

    public enum TextColorPalette
    {
        None,
        Brown1,
        Brown2,
        Olive1,
        Olive2,
        White
    }

    public enum TextOutlineStyle
    {
        None,
        Main_Brown1,
        Main_Brown2,
        Main_Olive2,
        Sub_Brown1,
        Sub_Olive1
    }

    public enum AudioSourceType
    {
        Bgm,
        Sfx,
        LoopSfx
    }

    public enum AudioClip
    {
        HomeBGM = 0,
        ButtonClick = 1,
        SummonNoa = 2,
        FireAttack= 3,
        WaterAttack= 5,
        WindAttack= 7,
        EarthAttack= 9,
        PurifyBGM = 11,
        BlessingSummonBuildUp = 12,
        BlessingSummonGlow = 13,
        WaveStart = 14,
        LevelUp = 15,
        BlessingSummonBGM = 16,
        BlessingReveal = 17,
        PurifySuccess = 18,
        PurifyFailed = 19,
        ForestBreath = 20,
        RewardGet = 21,
        DamageTree = 22,
        EnergyGained = 23,
    }

    public enum AudioPath
    {
        None,
        Common,
        Home,
        Purify
    }

    public enum Scene
    {
        GameBootstrap,
        Home,
        Purify,
    }

    public enum CurrencyType
    {
        ElementCore,
        Seed,
        Energy,
        NoaMemory,
        BlessingTicket
    }

    public enum CollectionType
    {
        Noa,
        Blessing
    }

    public enum ElementType
    {
        Neutral = 0,
        Water = 1,
        Fire = 2,
        Wind = 3,
        Earth = 4
    }

    public enum NoaTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3
    }

    public enum BlessingRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    public enum BlessingCategory
    {
        Summon,
        Purification,
        Element,
        Survival,
        Tactical
    }

    public enum BlessingEffectType
    {
        SummonCostReductionRate,
        PurifySpeedBonusRate,
        PurifyPowerBonusRate,
        WaveEndHealBonus,
        MoteKillRewardBonusRate,
        MoteEscapeDamageReductionRate,
        HigherTierSummonProbabilityBonusRate,
        ForestBreathChargeTimeReductionRate
    }

    public enum ContentIconType
    {
        Noa,
        Mote,
        Blessing,
        CommonUI
    }

    public enum StageId
    {
        None = 0,
        Stage1 = 1,
        Stage2 = 2
    }

}
