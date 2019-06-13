/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free 
 * 
 * ware; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject
{
    public class BCard : BCardDTO
    {
        public BCard()
        {
        }

        public BCard(BCardDTO input)
        {
            BCardId = input.BCardId;
            CardId = input.CardId;
            CastType = input.CastType;
            FirstData = input.FirstData;
            IsLevelDivided = input.IsLevelDivided;
            IsLevelScaled = input.IsLevelScaled;
            ItemVNum = input.ItemVNum;
            NpcMonsterVNum = input.NpcMonsterVNum;
            SecondData = input.SecondData;
            SkillVNum = input.SkillVNum;
            SubType = input.SubType;
            ThirdData = input.ThirdData;
            Type = input.Type;
        }

        #region Properties

        public Card BuffCard => ServerManager.GetCard((short)SecondData);

        #endregion

        #region Methods

        public void ApplyBCards(object session, object sender = null)
        {
            //Console.WriteLine($"BCardId: {BCardId} Type: {(BCardType.CardType)Type} SubType: {SubType} CardId: {CardId?.ToString() ?? "null"} ItemVNum: {ItemVNum?.ToString() ?? "null"} SkillVNum: {SkillVNum?.ToString() ?? "null"} SessionType: {session?.GetType().ToString() ?? "null"} SenderType: {sender?.GetType().ToString() ?? "null"}");

            switch ((BCardType.CardType)Type)
            {
                case BCardType.CardType.Buff:
                    {
                        if (ServerManager.RandomNumber() < FirstData)
                        {
                            Character senderCharacter = sender is ClientSession senderSession
                                ? senderSession.Character : sender as Character;

                            if (session is Character character)
                            {
                                short cardId = (short)SecondData;

                                // If either Berserk Spirit or Strong Berserk Spirit is active
                                // then don't add Weak Berserk Spirit

                                if (cardId == 601
                                    && character.Buff?.Any(s => s?.Card?.CardId == 602 || s?.Card?.CardId == 603) == true)
                                {
                                    break;
                                }

                                if (senderCharacter != null)
                                {
                                    // TODO: Get anti stats from BCard

                                    character.AddBuff(new Buff((short)SecondData, senderCharacter.Level, senderCharacter));
                                }
                                else
                                {
                                    character.AddBuff(new Buff((short)SecondData, character.Level));
                                }
                            }
                            else if (session is MapMonster mapMonster)
                            {
                                if (senderCharacter != null)
                                {
                                    mapMonster.AddBuff(new Buff((short)SecondData, senderCharacter.Level, senderCharacter));
                                }
                                else
                                {
                                    mapMonster.AddBuff(new Buff((short)SecondData, mapMonster.Monster.Level));
                                }
                            }
                        }
                    }
                    break;

                case BCardType.CardType.Move:
                    {
                        if (session is Character character)
                        {
                            character.LastSpeedChange = DateTime.Now;
                            character.Session.SendPacket(character.GenerateCond());
                        }
                    }
                    break;

                case BCardType.CardType.Summons:
                    {
                        if (session is Character character)
                        {
                            if (character.MapInstance != null)
                            {
                                List<MonsterToSummon> monsterList = new List<MonsterToSummon>();

                                if (SubType == (byte)AdditionalTypes.Summons.Summons / 10)
                                {
                                    int aliveTime = ServerManager.GetNpc((short)SecondData).RespawnTime;

                                    for (int i = 0; i < FirstData; i++)
                                    {
                                        MapCell mapCell = new MapCell
                                        {
                                            X = (short)(ServerManager.RandomNumber(-1, 2) + character.PositionX),
                                            Y = (short)(ServerManager.RandomNumber(-1, 2) + character.PositionY),
                                        };

                                        monsterList.Add(new MonsterToSummon((short)SecondData, mapCell, -1, true, false, false, true, false, -1, character, aliveTime, false));
                                    }
                                }

                                if (monsterList.Any())
                                {
                                    EventHelper.Instance.RunEvent(new EventContainer(character.MapInstance,
                                        EventActionType.SPAWNMONSTERS, monsterList));
                                }
                            }
                        }
                    }
                    break;

                case BCardType.CardType.SpecialAttack:
                    break;

                case BCardType.CardType.SpecialDefence:
                    break;

                case BCardType.CardType.AttackPower:
                    break;

                case BCardType.CardType.Target:
                    break;

                case BCardType.CardType.Critical:
                    break;

                case BCardType.CardType.SpecialCritical:
                    break;

                case BCardType.CardType.Element:
                    break;

                case BCardType.CardType.IncreaseDamage:
                    break;

                case BCardType.CardType.Defence:
                    break;

                case BCardType.CardType.DodgeAndDefencePercent:
                    break;

                case BCardType.CardType.Block:
                    break;

                case BCardType.CardType.Absorption:
                    break;

                case BCardType.CardType.ElementResistance:
                    break;

                case BCardType.CardType.EnemyElementResistance:
                    break;

                case BCardType.CardType.Damage:
                    break;

                case BCardType.CardType.GuarantedDodgeRangedAttack:
                    break;

                case BCardType.CardType.Morale:
                    break;

                case BCardType.CardType.Casting:
                    break;

                case BCardType.CardType.Reflection:
                    break;

                case BCardType.CardType.DrainAndSteal:
                    break;

                case BCardType.CardType.HealingBurningAndCasting:
                    {
                        Character senderCharacter = sender is ClientSession senderSession
                            ? senderSession.Character : sender as Character;

                        if (senderCharacter != null)
                        {
                            #region Character

                            if (session is Character character)
                            {
                                void HealingBurningAndCastingAction()
                                {
                                    if (!character.IsAlive
                                        || character.MapInstance == null
                                        || character.Session == null)
                                    {
                                        return;
                                    }

                                    int amount = 0;

                                    if (SubType == (byte)AdditionalTypes.HealingBurningAndCasting.RestoreHP / 10
                                        || SubType == (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseHP / 10)
                                    {
                                        if (FirstData > 0)
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * FirstData;
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            if (character.Hp + amount > character.HPMax)
                                            {
                                                amount = character.HPMax - character.Hp;
                                            }

                                            character.Hp += amount;

                                            character.MapInstance.Broadcast(character.GenerateRc(amount));
                                        }
                                        else
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * (FirstData - 1);
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            amount *= -1;

                                            if (character.Hp - amount < 1)
                                            {
                                                amount = character.Hp - 1;
                                            }

                                            character.GetDamage(amount);

                                            character.MapInstance.Broadcast(character.GenerateDm(amount));
                                        }

                                        character.Session.SendPacket(character.GenerateStat());
                                    }
                                    else if (SubType == (byte)AdditionalTypes.HealingBurningAndCasting.RestoreMP / 10
                                        || SubType == (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseMP / 10)
                                    {
                                        if (FirstData > 0)
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * FirstData;
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            if (character.Mp + amount > character.MPMax)
                                            {
                                                amount = character.MPMax - character.Mp;
                                            }

                                            character.Mp += amount;
                                        }
                                        else
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * (FirstData - 1);
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            amount *= -1;

                                            if (character.Mp - amount < 1)
                                            {
                                                amount = character.Mp - 1;
                                            }

                                            character.DecreaseMp(amount);
                                        }

                                        character.Session.SendPacket(character.GenerateStat());
                                    }
                                }

                                HealingBurningAndCastingAction();

                                if (ThirdData > 0
                                    && CardId != null)
                                {
                                    IDisposable disposable = Observable.Interval(TimeSpan.FromSeconds(ThirdData * 2))
                                        .Subscribe(s => HealingBurningAndCastingAction());

                                    character.DisposeBCard(BCardId);
                                    character.BCardDisposables[BCardId] = disposable;
                                }
                            }

                            #endregion

                            #region MapMonster

                            else if (session is MapMonster mapMonster)
                            {
                                void HealingBurningAndCastingAction()
                                {
                                    if (!mapMonster.IsAlive
                                        || mapMonster.MapInstance == null)
                                    {
                                        return;
                                    }

                                    int amount = 0;

                                    if (SubType == (byte)AdditionalTypes.HealingBurningAndCasting.RestoreHP / 10
                                        || SubType == (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseHP / 10)
                                    {
                                        if (FirstData > 0)
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * FirstData;
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            if (mapMonster.CurrentHp + amount > mapMonster.MaxHp)
                                            {
                                                amount = mapMonster.MaxHp - mapMonster.CurrentHp;
                                            }

                                            mapMonster.CurrentHp += amount;

                                            mapMonster.MapInstance.Broadcast(mapMonster.GenerateRc(amount));
                                        }
                                        else
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * (FirstData - 1);
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            amount *= -1;

                                            if (mapMonster.CurrentHp - amount < 1)
                                            {
                                                amount = mapMonster.CurrentHp - 1;
                                            }

                                            mapMonster.CurrentHp -= amount;

                                            mapMonster.MapInstance.Broadcast(mapMonster.GenerateDm(amount));
                                        }
                                    }

                                    if (SubType == (byte)AdditionalTypes.HealingBurningAndCasting.RestoreMP / 10
                                        || SubType == (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseMP / 10)
                                    {
                                        if (FirstData > 0)
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * FirstData;
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            if (mapMonster.CurrentMp + amount > mapMonster.MaxMp)
                                            {
                                                amount = mapMonster.MaxMp - mapMonster.CurrentMp;
                                            }

                                            mapMonster.CurrentMp += amount;
                                        }
                                        else
                                        {
                                            if (IsLevelScaled)
                                            {
                                                amount = senderCharacter.Level * (FirstData - 1);
                                            }
                                            else
                                            {
                                                amount = FirstData;
                                            }

                                            amount *= -1;

                                            if (mapMonster.CurrentMp - amount < 1)
                                            {
                                                amount = mapMonster.CurrentMp - 1;
                                            }

                                            mapMonster.CurrentMp -= amount;
                                        }
                                    }
                                }

                                HealingBurningAndCastingAction();

                                if (ThirdData > 0
                                    && CardId != null)
                                {
                                    IDisposable disposable = Observable.Interval(TimeSpan.FromSeconds(ThirdData * 2))
                                        .Subscribe(s => HealingBurningAndCastingAction());

                                    mapMonster.DisposeBCard(BCardId);
                                    mapMonster.BCardDisposables[BCardId] = disposable;
                                }
                            }

                            #endregion
                        }
                    }
                    break;

                case BCardType.CardType.HPMP:
                    {
                        if (SubType == (byte)AdditionalTypes.HPMP.DecreaseRemainingMP / 10)
                        {
                            if (FirstData < 0)
                            {
                                double multiplier = (FirstData * -1) / 100D;

                                if (session is Character character)
                                {
                                    character.DecreaseMp((int)(character.Mp * multiplier));
                                    character.Session?.SendPacket(character.GenerateStat());
                                }
                                else if (session is MapMonster mapMonster)
                                {
                                    mapMonster.DecreaseMp((int)(mapMonster.CurrentMp * multiplier));
                                }
                            }
                        }
                    }
                    break;

                case BCardType.CardType.SpecializationBuffResistance:
                    {
                        if (SubType == (byte)AdditionalTypes.SpecializationBuffResistance.RemoveGoodEffects / 10)
                        {
                            if (session is Character character)
                            {
                                if (FirstData < 0)
                                {
                                    if (ServerManager.RandomNumber() < (FirstData * -1))
                                    {
                                        character.Buff?.GetAllItems()?.Where(s => s?.Card?.BuffType == BuffType.Bad && s.Card.Level < SecondData)?
                                            .ToList()?.ForEach(s => character.RemoveBuff(s.Card.CardId));
                                    }
                                }
                                else
                                {
                                    if (ServerManager.RandomNumber() < FirstData)
                                    {
                                        character.Buff?.GetAllItems()?.Where(s => s?.Card?.BuffType == BuffType.Good && s.Card.Level < SecondData)?
                                            .ToList()?.ForEach(s => character.RemoveBuff(s.Card.CardId));
                                    }
                                }
                            }
                        }
                    }
                    break;

                case BCardType.CardType.SpecialEffects:
                    {
                        if (SubType == (byte)AdditionalTypes.SpecialEffects.ShadowAppears / 10)
                        {
                            if (session is Character character)
                            {
                                character.NumberOfShadows = FirstData;
                                character.ShadowsDistance = SecondData;
                                character.MapInstance?.Broadcast(character.GenerateSpeed());
                            }
                        }
                    }
                    break;

                case BCardType.CardType.Capture:
                    {
                        if (session is MapMonster mapMonster
                            && sender is ClientSession senderSession)
                        {
                            NpcMonster mateNpc = ServerManager.GetNpc(mapMonster.MonsterVNum);

                            if (mateNpc != null)
                            {
                                if (mapMonster.Monster.Catch)
                                {
                                    if (mapMonster.IsAlive && mapMonster.CurrentHp <= (int)((double)mapMonster.MaxHp / 2))
                                    {
                                        if (mapMonster.Monster.Level < senderSession.Character.Level)
                                        {
                                            // TODO: Find a new algorithm
                                            int[] chance = { 100, 80, 60, 40, 20, 0 };
                                            if (ServerManager.RandomNumber() < chance[ServerManager.RandomNumber(0, 5)])
                                            {
                                                Mate mate = new Mate(senderSession.Character, mateNpc, (byte)(mapMonster.Monster.Level - 15 > 0 ? mapMonster.Monster.Level - 15 : 1), MateType.Pet);
                                                if (senderSession.Character.CanAddMate(mate))
                                                {
                                                    senderSession.Character.AddPetWithSkill(mate);
                                                    senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CATCH_SUCCESS"), 0));
                                                    senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, senderSession.Character.CharacterId, 197));
                                                    senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player, senderSession.Character.CharacterId, 3, mapMonster.MapMonsterId, -1, 0, 15, -1, -1, -1, true, (int)((float)mapMonster.CurrentHp / (float)mapMonster.MaxHp * 100), 0, -1, 0));
                                                    mapMonster.SetDeathStatement();
                                                    senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.Out(UserType.Monster, mapMonster.MapMonsterId));
                                                }
                                                else
                                                {
                                                    senderSession.SendPacket(senderSession.Character.GenerateSay(Language.Instance.GetMessageFromKey("PET_SLOT_FULL"), 10));
                                                    senderSession.SendPacket(StaticPacketHelper.Cancel(2, mapMonster.MapMonsterId));
                                                }
                                            }
                                            else
                                            {
                                                senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CATCH_FAIL"), 0));
                                                senderSession.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player, senderSession.Character.CharacterId, 3, mapMonster.MapMonsterId, -1, 0, 15, -1, -1, -1, true, (int)((float)mapMonster.CurrentHp / (float)mapMonster.MaxHp * 100), 0, -1, 0));
                                            }
                                        }
                                        else
                                        {
                                            senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LEVEL_LOWER_THAN_MONSTER"), 0));
                                            senderSession.SendPacket(StaticPacketHelper.Cancel(2, mapMonster.MapMonsterId));
                                        }
                                    }
                                    else
                                    {
                                        senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CURRENT_HP_TOO_HIGH"), 0));
                                        senderSession.SendPacket(StaticPacketHelper.Cancel(2, mapMonster.MapMonsterId));
                                    }
                                }
                                else
                                {
                                    senderSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MONSTER_CANT_BE_CAPTURED"), 0));
                                    senderSession.SendPacket(StaticPacketHelper.Cancel(2, mapMonster.MapMonsterId));
                                }
                            }
                        }
                    }
                    break;

                case BCardType.CardType.SpecialDamageAndExplosions:
                    break;

                case BCardType.CardType.SpecialEffects2:
                    {
                        if (SubType == (byte)AdditionalTypes.SpecialEffects2.TeleportInRadius / 10)
                        {
                            if (session is Character character)
                            {
                                character.Teleport((short)FirstData);
                            }
                        }
                    }
                    break;

                case BCardType.CardType.CalculatingLevel:
                    break;

                case BCardType.CardType.Recovery:
                    break;

                case BCardType.CardType.MaxHPMP:
                    {
                        if (session is Character character)
                        {
                            if (SubType == (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumHP / 10)
                            {
                                character.HPLoad();
                                character.Session?.SendPacket(character.GenerateStat());
                            }
                            else if (SubType == (byte)AdditionalTypes.MaxHPMP.IncreasesMaximumMP / 10)
                            {
                                character.MPLoad();
                                character.Session?.SendPacket(character.GenerateStat());
                            }
                        }
                    }
                    break;

                case BCardType.CardType.MultAttack:
                    break;

                case BCardType.CardType.MultDefence:
                    break;

                case BCardType.CardType.TimeCircleSkills:
                    break;

                case BCardType.CardType.RecoveryAndDamagePercent:
                    break;

                case BCardType.CardType.Count:
                    break;

                case BCardType.CardType.NoDefeatAndNoDamage:
                    break;

                case BCardType.CardType.SpecialActions:
                    {
                        Character senderCharacter = sender is ClientSession senderSession
                            ? senderSession.Character : sender as Character;

                        if (SubType == (byte)AdditionalTypes.SpecialActions.PushBack / 10)
                        {
                            if (senderCharacter != null)
                            {
                                if (session is Character character)
                                {
                                    if (character.ResistForcedMovementChance <= 0
                                        || ServerManager.RandomNumber() < character.ResistForcedMovementChance)
                                    {
                                        character.PushBack((short)FirstData, senderCharacter);
                                    }
                                }
                                else if (session is MapMonster mapMonster)
                                {
                                    mapMonster.PushBack((short)FirstData, senderCharacter);
                                }
                            }
                        }
                        else if (SubType == (byte)AdditionalTypes.SpecialActions.FocusEnemies / 10)
                        {
                            if (senderCharacter != null)
                            {
                                if (session is Character character)
                                {
                                    if (character.ResistForcedMovementChance <= 0
                                        || ServerManager.RandomNumber() < character.ResistForcedMovementChance)
                                    {
                                        character.Focus((short)FirstData, senderCharacter);
                                    }
                                }
                                else if (session is MapMonster mapMonster)
                                {
                                    mapMonster.Focus((short)FirstData, senderCharacter);
                                }
                            }
                        }
                        else if (SubType == (byte)AdditionalTypes.SpecialActions.Hide / 10)
                        {
                            if (session is Character character)
                            {
                                character.SetInvisible(true);
                            }
                        }
                    }
                    break;

                case BCardType.CardType.Transform:
                    break;

                case BCardType.CardType.Mode:
                    break;

                case BCardType.CardType.NoCharacteristicValue:
                    break;

                case BCardType.CardType.LightAndShadow:
                    {
                        if (SubType == (byte)AdditionalTypes.LightAndShadow.RemoveBadEffects / 10)
                        {
                            if (session is Character character)
                            {
                                character.Buff?.GetAllItems()?.Where(s => s?.Card?.BuffType == BuffType.Bad && s.Card.Level < FirstData)?
                                    .ToList()?.ForEach(s => character.RemoveBuff(s.Card.CardId));
                            }
                        }
                    }
                    break;

                case BCardType.CardType.Item:
                    break;

                case BCardType.CardType.DebuffResistance:
                    break;

                case BCardType.CardType.SpecialBehaviour:
                    break;

                case BCardType.CardType.Quest:
                    break;

                case BCardType.CardType.SecondSPCard:
                    {
                        if (session is Character character)
                        {
                            if (character.MapInstance != null)
                            {
                                List<MonsterToSummon> monsterList = new List<MonsterToSummon>();

                                if (SubType == (byte)AdditionalTypes.SecondSPCard.PlantBomb / 10)
                                {
                                    MapMonster bomb = character.MapInstance.Monsters.FirstOrDefault(m => m?.Owner?.CharacterId == character.CharacterId
                                        && m.MonsterVNum == (short)SecondData);

                                    if (bomb == null)
                                    {
                                        for (int i = 0; i < FirstData; i++)
                                        {
                                            monsterList.Add(new MonsterToSummon((short)SecondData, new MapCell { X = character.PositionX, Y = character.PositionY },
                                                -1, false, false, false, false, false, -1, character, 0, false));
                                        }
                                    }
                                    else
                                    {
                                        bomb.Explode();
                                        break;
                                    }
                                }
                                else if (SubType == (byte)AdditionalTypes.SecondSPCard.PlantSelfDestructionBomb / 10)
                                {
                                    int aliveTime = ServerManager.GetNpc((short)SecondData).RespawnTime;

                                    for (int i = 0; i < FirstData; i++)
                                    {
                                        MapCell mapCell = new MapCell
                                        {
                                            X = (short)(ServerManager.RandomNumber(-1, 2) + character.PositionX),
                                            Y = (short)(ServerManager.RandomNumber(-1, 2) + character.PositionY),
                                        };

                                        monsterList.Add(new MonsterToSummon((short)SecondData, mapCell, -1, true, false, false, true, false, -1, character, aliveTime, true));
                                    }
                                }

                                if (monsterList.Any())
                                {
                                    EventHelper.Instance.RunEvent(new EventContainer(character.MapInstance,
                                        EventActionType.SPAWNMONSTERS, monsterList));
                                }
                            }
                        }
                    }
                    break;

                case BCardType.CardType.SPCardUpgrade:
                    break;

                case BCardType.CardType.HugeSnowman:
                    break;

                case BCardType.CardType.Drain:
                    break;

                case BCardType.CardType.BossMonstersSkill:
                    break;

                case BCardType.CardType.LordHatus:
                    break;

                case BCardType.CardType.LordCalvinas:
                    break;

                case BCardType.CardType.SESpecialist:
                    {
                        if (SubType == (byte)AdditionalTypes.SESpecialist.LowerHPStrongerEffect / 10)
                        {
                            Character senderCharacter = sender is ClientSession senderSession
                                ? senderSession.Character : sender as Character;

                            if (senderCharacter != null)
                            {
                                if (session is Character character)
                                {
                                    double percentage = (character.Hp * 100) / character.HPMax;

                                    if (percentage < 35)
                                    {
                                        character.AddBuff(new Buff(274, senderCharacter.Level, senderCharacter));
                                    }
                                    else if (percentage < 67)
                                    {
                                        character.AddBuff(new Buff(273, senderCharacter.Level, senderCharacter));
                                    }
                                    else
                                    {
                                        character.AddBuff(new Buff(272, senderCharacter.Level, senderCharacter));
                                    }
                                }
                            }
                        }
                    }
                    break;

                case BCardType.CardType.FourthGlacernonFamilyRaid:
                    break;

                case BCardType.CardType.SummonedMonsterAttack:
                    break;

                case BCardType.CardType.BearSpirit:
                    {
                        if (session is Character character)
                        {
                            if (SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumHP / 10)
                            {
                                character.HPLoad();
                                character.Session?.SendPacket(character.GenerateStat());
                            }
                            else if (SubType == (byte)AdditionalTypes.BearSpirit.IncreaseMaximumMP / 10)
                            {
                                character.MPLoad();
                                character.Session?.SendPacket(character.GenerateStat());
                            }
                        }
                    }
                    break;

                case BCardType.CardType.SummonSkill:
                    break;

                case BCardType.CardType.InflictSkill:
                    break;

                case BCardType.CardType.HideBarrelSkill:
                    break;

                case BCardType.CardType.FocusEnemyAttentionSkill:
                    break;

                case BCardType.CardType.TauntSkill:
                    break;

                case BCardType.CardType.FireCannoneerRangeBuff:
                    break;

                case BCardType.CardType.VulcanoElementBuff:
                    break;

                case BCardType.CardType.DamageConvertingSkill:
                    break;

                case BCardType.CardType.MeditationSkill:
                    {
                        if (session is Character character)
                        {
                            if (SkillVNum.HasValue && SubType.Equals((byte)AdditionalTypes.MeditationSkill.CausingChance / 10) && ServerManager.RandomNumber() < FirstData)
                            {
                                Skill skill = ServerManager.GetSkill(SkillVNum.Value);
                                Skill newSkill = ServerManager.GetSkill((short)SecondData);

                                Observable.Timer(TimeSpan.FromMilliseconds(100)).Subscribe(observer =>
                                {
                                    foreach (QuicklistEntryDTO quicklistEntry in character.QuicklistEntries.Where(s => s.Pos.Equals(skill.CastId)))
                                    {
                                        character.Session.SendPacket($"qset {quicklistEntry.Q1} {quicklistEntry.Q2} {quicklistEntry.Type}.{quicklistEntry.Slot}.{newSkill.CastId}.0");
                                    }

                                    character.Session.SendPacket($"mslot {newSkill.CastId} -1");
                                });

                                character.SkillComboCount++;
                                character.LastSkillComboUse = DateTime.Now;

                                if (skill.CastId > 10)
                                {
                                    Observable.Timer(TimeSpan.FromMilliseconds((skill.GetCooldown(character) * 100) + 500)).Subscribe(observer => character.Session.SendPacket(StaticPacketHelper.SkillReset(skill.CastId)));
                                }
                            }

                            switch (SubType)
                            {
                                case 2:
                                    character.MeditationDictionary[(short)SecondData] = DateTime.Now.AddSeconds(4);
                                    break;

                                case 3:
                                    character.MeditationDictionary[(short)SecondData] = DateTime.Now.AddSeconds(8);
                                    break;

                                case 4:
                                    character.MeditationDictionary[(short)SecondData] = DateTime.Now.AddSeconds(12);
                                    break;
                            }
                        }
                    }
                    break;

                case BCardType.CardType.FalconSkill:
                    break;

                case BCardType.CardType.AbsorptionAndPowerSkill:
                    break;

                case BCardType.CardType.LeonaPassiveSkill:
                    break;

                case BCardType.CardType.FearSkill:
                    break;

                case BCardType.CardType.SniperAttack:
                    break;

                case BCardType.CardType.FrozenDebuff:
                    break;

                case BCardType.CardType.JumpBackPush:
                    break;

                case BCardType.CardType.FairyXPIncrease:
                    break;

                case BCardType.CardType.SummonAndRecoverHP:
                    break;

                case BCardType.CardType.TeamArenaBuff:
                    break;

                case BCardType.CardType.ArenaCamera:
                    break;

                case BCardType.CardType.DarkCloneSummon:
                    break;

                case BCardType.CardType.AbsorbedSpirit:
                    break;

                case BCardType.CardType.AngerSkill:
                    break;

                case BCardType.CardType.MeteoriteTeleport:
                    break;

                case BCardType.CardType.StealBuff:
                    break;

                case BCardType.CardType.Unknown:
                    break;

                case BCardType.CardType.EffectSummon:
                    break;

                default:
                    Logger.Warn($"Card Type {Type} Not Found!");
                    break;
            }
        }

        #endregion
    }
}