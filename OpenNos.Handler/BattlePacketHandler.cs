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

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Battle;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using static OpenNos.Domain.BCardType;

namespace OpenNos.Handler
{
    public class BattlePacketHandler : IPacketHandler
    {
        #region Instantiation

        public BattlePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        /// <summary>
        /// mtlist packet
        /// </summary>
        /// <param name="mutliTargetListPacket"></param>
        public void MultiTargetListHit(MultiTargetListPacket mutliTargetListPacket)
        {
            if (!Session.HasCurrentMapInstance)
            {
                return;
            }
            bool isMuted = Session.Character.MuteMessage();
            if (isMuted || Session.Character.IsVehicled)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }
            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                return;
            }
            if (mutliTargetListPacket.TargetsAmount > 0 && mutliTargetListPacket.Targets == null)
            {
                foreach (ClientSession team in ServerManager.Instance.Sessions.Where(s =>
                 s.Account.Authority >= AuthorityType.GameMaster || s.Account.Authority == AuthorityType.Supporter))
                    if (team.HasSelectedCharacter)
                    {
                        team.SendPacket(team.Character.GenerateSay($"User {Session.Character.Name} tried a crash: MultiTargetLastHit",
                        12));
                    }
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("Don't try it again..."));
                Logger.Log.Debug($"user {Session.Character.Name} tried an Crash: MultiTargetListHit");
                return;
            }
            if (mutliTargetListPacket.TargetsAmount > 0 && mutliTargetListPacket.TargetsAmount == mutliTargetListPacket.Targets.Count && mutliTargetListPacket.Targets != null)
            {
                Session.Character.MTListTargetQueue.Clear();
                foreach (MultiTargetListSubPacket subpacket in mutliTargetListPacket.Targets)
                {
                    Session.Character.MTListTargetQueue.Push(new MTListHitTarget(subpacket.TargetType, subpacket.TargetId));
                }
            }
        }

        /// <summary>
        /// u_s packet
        /// </summary>
        /// <param name="useSkillPacket"></param>
        public void UseSkill(UseSkillPacket useSkillPacket)
        {
            if (Session?.Character == null)
            {
                return;
            }

            if (Session.Character.NoAttack || Session.Character.NoMove)
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
                Session.SendPacket(Session.Character.GenerateCond());
                return;
            }

            if (Session.Character.Hp > 0
                && Session.Character.CanFight && useSkillPacket != null)
            {
                Session.Character.RemoveBuff(614);
                Session.Character.RemoveBuff(615);
                Session.Character.RemoveBuff(616);
                bool isMuted = Session.Character.MuteMessage();

                if (isMuted || Session.Character.IsVehicled || Session.Character.InvisibleGm)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel(2));
                    return;
                }

                if (useSkillPacket.MapX.HasValue && useSkillPacket.MapY.HasValue)
                {
                    Session.Character.PositionX = useSkillPacket.MapX.Value;
                    Session.Character.PositionY = useSkillPacket.MapY.Value;
                }

                if (Session.Character.IsSitting)
                {
                    Session.Character.Rest();
                }

                switch (useSkillPacket.UserType)
                {
                    case UserType.Player:
                        {
                            if (useSkillPacket.MapMonsterId != Session.Character.CharacterId)
                            {
                                TargetHit(useSkillPacket.CastId, useSkillPacket.MapMonsterId, true);
                            }
                            else
                            {
                                TargetHit(useSkillPacket.CastId, useSkillPacket.MapMonsterId);
                            }
                        }
                        break;

                    case UserType.Monster:
                        {
                            MapMonster monsterToAttack = Session.CurrentMapInstance.GetMonster(useSkillPacket.MapMonsterId);

                            if (monsterToAttack?.Owner == null)
                            {
                                TargetHit(useSkillPacket.CastId, useSkillPacket.MapMonsterId);
                            }
                            else
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                                return;
                            }
                        }
                        break;

                    default:
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                        }
                        break;
                }

                if (useSkillPacket.UserType == UserType.Player
                    || useSkillPacket.UserType == UserType.Monster)
                {
                    int[] effectSummon = Session.Character.GetBuff(CardType.EffectSummon, 11);

                    if (effectSummon[0] > ServerManager.RandomNumber())
                    {
                        Observable.Timer(TimeSpan.FromSeconds(1))
                            .Subscribe(o =>
                            {
                                CharacterSkill ski = (Session.Character.UseSp ? Session.Character.SkillsSp?.GetAllItems()
                                    : Session.Character.Skills?.GetAllItems())?.Find(s => s.Skill != null
                                    && s.Skill.CastId == useSkillPacket.CastId && s.Skill.UpgradeSkill == 0);

                                if (ski != null)
                                {
                                    ski.LastUse = DateTime.Now.AddMilliseconds(ski.Skill.GetCooldown(Session.Character) * 100 * -1);
                                    Session.SendPacket(StaticPacketHelper.SkillReset(useSkillPacket.CastId));
                                }
                            });
                    }
                }
            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
            }
        }


        /// <summary>
        /// u_as packet
        /// </summary>
        /// <param name="useAoeSkillPacket"></param>
        public void UseZonesSkill(UseAOESkillPacket useAoeSkillPacket)
        {
            bool isMuted = Session.Character.MuteMessage();
            if (isMuted || Session.Character.IsVehicled)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
            }
            else
            {
                if (Session.Character.LastTransform.AddSeconds(3) > DateTime.Now)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                    return;
                }

                if (Session.Character.CanFight && Session.Character.Hp > 0)
                {
                    ZoneHit(useAoeSkillPacket.CastId, useAoeSkillPacket.MapX, useAoeSkillPacket.MapY);
                }
            }
            List<CharacterSkill> skills = Session.Character.UseSp
                ? Session.Character.SkillsSp.GetAllItems()
                : Session.Character.Skills.GetAllItems();
            CharacterSkill characterSkill = skills?.Find(s => s.Skill?.CastId == useAoeSkillPacket.CastId);

        }

        private void PvpHit(HitRequest hitRequest, ClientSession target)
        {
            try
            {
                if (target?.Character.Hp > 0 && hitRequest?.Session.Character.Hp > 0)
                {
                    if ((Session.CurrentMapInstance.MapInstanceId == ServerManager.Instance.ArenaInstance.MapInstanceId
                         || Session?.CurrentMapInstance?.MapInstanceId
                         == ServerManager.Instance.FamilyArenaInstance.MapInstanceId)
                        && (Session.CurrentMapInstance.Map.JaggedGrid[Session.Character.PositionX][
                                    Session.Character.PositionY]
                                ?.Value != 0
                            || target.CurrentMapInstance?.Map?.JaggedGrid[target.Character.PositionX][
                                    target.Character.PositionY]
                                ?.Value != 0))
                    {
                        // User in SafeZone
                        hitRequest.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                        return;
                    }

                    if (target.Character.IsSitting)
                    {
                        target.Character.Rest();
                    }

                    // Apply Equipment BCards
                    {
                        Character attacker = hitRequest?.Session?.Character;
                        Character defender = target?.Character;

                        if (attacker != null && defender != null)
                        {
                            attacker.GetMainWeaponBCards(CardType.Buff)?.ToList()
                                .ForEach(s => s.ApplyBCards(s.BuffCard?.BuffType == BuffType.Bad ? defender : attacker, attacker));

                            attacker.GetSecondaryWeaponBCards(CardType.Buff)?.ToList()
                                .ForEach(s => s.ApplyBCards(s.BuffCard?.BuffType == BuffType.Bad ? defender : attacker, attacker));

                            defender.GetArmorBCards(CardType.Buff)?.ToList()
                                .ForEach(s => s.ApplyBCards(s.BuffCard?.BuffType == BuffType.Bad ? attacker : defender, defender));
                        }
                    }

                    int hitmode = 0;
                    bool onyxWings = false;
                    BattleEntity battleEntity = new BattleEntity(hitRequest.Session.Character, hitRequest.Skill);
                    BattleEntity battleEntityDefense = new BattleEntity(target.Character, null);
                    int damage = DamageHelper.Instance.CalculateDamage(battleEntity, battleEntityDefense, hitRequest.Skill,
                        ref hitmode, ref onyxWings);

                    if (Session.Character.Class == ClassType.Wreastler)
                    {
                        damage = (ushort)(damage * 1.35);
                    }

                    // Charge
                    {
                        if (hitmode != 1)
                        {
                            Session.Character.ApplyCharge(ref damage);
                        }
                    }

                    if (target.Character.HasGodMode)
                    {
                        damage = 0;
                        hitmode = 1;
                    }
                    else if (target.Character.LastPVPRevive > DateTime.Now.AddSeconds(-10)
                            || hitRequest.Session.Character.LastPVPRevive > DateTime.Now.AddSeconds(-10))
                    {
                        damage = 0;
                        hitmode = 1;
                    }

                    // Absorb
                    {
                        if (target.Character.HasBuff(CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.TransferAttackPower))
                        {
                            target.Character.Absorb(ref damage, ref hitmode);
                        }
                    }

                    // Invisible
                    {
                        if (damage > 0)
                        {
                            if (Session.Character.Invisible)
                            {
                                Session.Character.SetInvisible(false);
                            }

                            if (target.Character.Invisible)
                            {
                                target.Character.SetInvisible(false);
                            }
                        }
                    }

                    // HPDecreasedByConsumingMP
                    {
                        Character attacker = Session.Character;

                        if (attacker.HasBuff(CardType.HealingBurningAndCasting, (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP))
                        {
                            attacker.GetDamage(hitRequest.Skill.MpCost);

                            if (attacker.Hp < 1)
                            {
                                attacker.Hp = 1;
                            }

                            attacker.MapInstance?.Broadcast(attacker.GenerateDm(hitRequest.Skill.MpCost));
                            Session.SendPacket(attacker.GenerateStat());
                        }
                    }

                    if (damage > 0)
                    {
                        damage /= 2;

                        if (damage < 1)
                        {
                            damage = 1;
                        }
                    }

                    // InflictDamageToMP
                    {
                        int amount = (int)((damage / 100D) * target.Character.GetBuff(CardType.LightAndShadow, (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP)[0]);

                        target.Character.Mp -= amount;
                        damage -= amount;

                        if (target.Character.Mp < 1)
                        {
                            target.Character.Mp = 1;
                        }
                    }

                    if (onyxWings && hitmode != 1)
                    {
                        if (hitRequest != null)
                        {
                            short onyxX = (short)(hitRequest?.Session?.Character?.PositionX + 2);
                            short onyxY = (short)(hitRequest?.Session?.Character?.PositionY + 2);
                            int onyxId = target.CurrentMapInstance.GetNextMonsterId();
                            MapMonster onyx = new MapMonster
                            {
                                MonsterVNum = 2371,
                                MapX = onyxX,
                                MapY = onyxY,
                                MapMonsterId = onyxId,
                                IsHostile = false,
                                IsMoving = false,
                                ShouldRespawn = false
                            };
                            target.CurrentMapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(31, 1,
                                hitRequest.Session.Character.CharacterId, onyxX, onyxY));
                            onyx.Initialize(target.CurrentMapInstance);
                            target.CurrentMapInstance.AddMonster(onyx);
                            target.CurrentMapInstance.Broadcast(onyx.GenerateIn());
                            target.Character.GetDamage(damage);
                            Observable.Timer(TimeSpan.FromMilliseconds(350)).Subscribe(o =>
                            {
                                target.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, onyxId, 1,
                                    target.Character.CharacterId, -1, 0, -1, hitRequest.Skill.Effect, -1, -1, true, 92,
                                    damage, 0, 0));
                                target.CurrentMapInstance.RemoveMonster(onyx);
                                target?.CurrentMapInstance?.Broadcast(StaticPacketHelper.Out(UserType.Monster,
                                    onyxId));
                            });
                        }
                    }

                    // Reflection.EnemyMPDecreasedChance
                    {
                        BCard bcard = hitRequest.Skill?.BCards?.FirstOrDefault(s => s.Type == (byte)CardType.Reflection
                            && s.SubType == (byte)AdditionalTypes.Reflection.EnemyMPDecreasedChance / 10);

                        if (bcard != null)
                        {
                            if (ServerManager.RandomNumber() < (bcard.FirstData * -1))
                            {
                                target.Character.Mp -= (int)((target.Character.Mp / 100D) * bcard.SecondData);

                                if (target.Character.Mp < 1)
                                {
                                    target.Character.Mp = 1;
                                }
                            }
                        }
                    }

                    target.Character.GetDamage(damage);
                    target.SendPacket(target.Character.GenerateStat());

                    // Reflection.HPIncreased
                    // Reflection.MPIncreased
                    {
                        Character attacker = Session.Character;

                        double hpIncreased = attacker.GetBuff(CardType.Reflection, (byte)AdditionalTypes.Reflection.HPIncreased)[0] / 100D;
                        double mpIncreased = attacker.GetBuff(CardType.Reflection, (byte)AdditionalTypes.Reflection.MPIncreased)[0] / 100D;

                        if (hpIncreased > 0)
                        {
                            int amount = (int)(damage * hpIncreased);

                            if (attacker.Hp + amount > attacker.HPMax)
                            {
                                amount = attacker.HPMax - attacker.Hp;
                            }

                            attacker.Hp += amount;

                            attacker.MapInstance?.Broadcast(attacker.GenerateRc(amount));
                            attacker.Session.SendPacket(attacker.GenerateStat());
                        }

                        if (mpIncreased > 0)
                        {
                            int amount = (int)(damage * mpIncreased);

                            if (attacker.Mp + amount > attacker.MPMax)
                            {
                                amount = attacker.MPMax - attacker.Mp;
                            }

                            attacker.Mp += amount;

                            attacker.Session.SendPacket(attacker.GenerateStat());
                        }
                    }

                    bool isAlive = target.Character.IsAlive;
                    if (!isAlive && target.HasCurrentMapInstance)
                    {
                        try
                        {
                            if (target?.CurrentMapInstance?.Map?.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4)
                                == true)
                            {
                                if (ServerManager.Instance.ChannelId == 51 && ServerManager.Instance.Act4DemonStat.Mode == 0
                                                                           && ServerManager.Instance.Act4AngelStat.Mode == 0)
                                {
                                    switch (Session.Character.Faction)
                                    {
                                        case FactionType.Angel:
                                            ServerManager.Instance.Act4AngelStat.Percentage += 100;
                                            break;

                                        case FactionType.Demon:
                                            ServerManager.Instance.Act4DemonStat.Percentage += 100;
                                            break;
                                    }
                                }

                                hitRequest.Session.Character.Act4Kill++;
                                target.Character.Act4Dead++;
                                target.Character.GetAct4Points(-1);
                                if (target.Character.Level + 10 >= hitRequest.Session.Character.Level
                                    && hitRequest.Session.Character.Level <= target.Character.Level - 10)
                                {
                                    hitRequest.Session.Character.GetAct4Points(2);
                                }

                                if (target.Character.Reputation < 50000)
                                {
                                    target.SendPacket(Session.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("LOSE_REP"), 0), 11));
                                }
                                else
                                {
                                    target.Character.Reputation -= target.Character.Level * 50;
                                    hitRequest.Session.Character.Reputation += target.Character.Level * 50;
                                    hitRequest.Session.SendPacket(hitRequest.Session.Character.GenerateLev());
                                    target?.SendPacket(target.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("LOSE_REP"),
                                            (short)(target.Character.Level * 50)), 11));
                                }

                                foreach (ClientSession sess in ServerManager.Instance.Sessions.Where(
                                    s => s.HasSelectedCharacter))
                                {
                                    if (sess.Character.Faction == Session.Character.Faction)
                                    {
                                        sess.SendPacket(sess.Character.GenerateSay(
                                            string.Format(
                                                Language.Instance.GetMessageFromKey(
                                                    $"ACT4_PVP_KILL{(int)target.Character.Faction}"), Session.Character.Name),
                                            12));
                                    }
                                    else if (sess.Character.Faction == target.Character.Faction)
                                    {
                                        sess.SendPacket(sess.Character.GenerateSay(
                                            string.Format(
                                                Language.Instance.GetMessageFromKey(
                                                    $"ACT4_PVP_DEATH{(int)target.Character.Faction}"), target.Character.Name),
                                            11));
                                    }
                                }

                                target?.SendPacket(target.Character.GenerateFd());
                                target?.Character.DisableBuffs(BuffType.All);
                                target?.CurrentMapInstance?.Broadcast(target, target.Character.GenerateIn(),
                                    ReceiverType.AllExceptMe);
                                target?.CurrentMapInstance.Broadcast(target, target.Character.GenerateGidx(),
                                    ReceiverType.AllExceptMe);
                                target?.SendPacket(
                                    target?.Character.GenerateSay(Language.Instance.GetMessageFromKey("ACT4_PVP_DIE"), 11));
                                target?.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ACT4_PVP_DIE"), 0));
                                Observable.Timer(TimeSpan.FromMilliseconds(2000)).Subscribe(o =>
                                {
                                    target?.CurrentMapInstance?.Broadcast(target,
                                        $"c_mode 1 {target.Character.CharacterId} 1564 0 0 0");
                                    target?.CurrentMapInstance?.Broadcast(target.Character.GenerateRevive());
                                });
                                Observable.Timer(TimeSpan.FromMilliseconds(30000)).Subscribe(o =>
                                {
                                    target.Character.Hp = (int)target.Character.HPLoad();
                                    target.Character.Mp = (int)target.Character.MPLoad();
                                    short x = (short)(39 + ServerManager.RandomNumber(-2, 3));
                                    short y = (short)(42 + ServerManager.RandomNumber(-2, 3));
                                    if (target?.CurrentMapInstance?.MapInstanceType == MapInstanceType.Ac4Raid)
                                    {
                                        if (target.Character.Faction == FactionType.Angel)
                                        {
                                            ServerManager.Instance.ChangeMap(target.Character.CharacterId, 153, 122, 160);
                                        }
                                        else
                                            ServerManager.Instance.ChangeMap(target.Character.CharacterId, 153, 68, 156);
                                    }
                                    else if (target.Character.Faction == FactionType.Angel)
                                    {
                                        ServerManager.Instance.ChangeMap(target.Character.CharacterId, 130, x, y);
                                    }
                                    else if (target.Character.Faction == FactionType.Demon)
                                    {
                                        ServerManager.Instance.ChangeMap(target.Character.CharacterId, 131, x, y);
                                    }
                                    else
                                    {
                                        target.Character.MapId = 145;
                                        target.Character.MapX = 51;
                                        target.Character.MapY = 41;
                                        string connection =
                                            CommunicationServiceClient.Instance.RetrieveOriginWorld(Session.Account.AccountId);
                                        if (string.IsNullOrWhiteSpace(connection))
                                        {
                                            return;
                                        }
                                        int port = Convert.ToInt32(connection.Split(':')[1]);
                                        Session.Character.ChangeChannel(connection.Split(':')[0], port, 3);
                                        return;
                                    }

                                    target.CurrentMapInstance?.Broadcast(target, target.Character.GenerateTp());
                                    target.CurrentMapInstance?.Broadcast(target.Character.GenerateRevive());
                                    target.SendPacket(target.Character.GenerateStat());
                                });
                            }
                            else
                            {
                                hitRequest.Session.Character.TalentWin++;
                                target.Character.TalentLose++;
                                hitRequest.Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("PVP_KILL"),
                                        hitRequest.Session.Character.Name, target.Character.Name), 10));
                                Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                                    ServerManager.Instance.AskPvpRevive(target.Character.CharacterId));
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (hitmode != 1)
                    {
                        hitRequest.Skill?.BCards?.ForEach(s =>
                        {
                            switch ((CardType)s.Type)
                            {
                                case CardType.Buff:
                                    {
                                        object attacker = Session.Character;
                                        object defender = target.Character;

                                        s.ApplyBCards(s?.BuffCard?.BuffType != BuffType.Good ? defender : attacker, attacker);
                                    }
                                    break;
                                case CardType.SpecialActions:
                                case CardType.JumpBackPush:
                                case CardType.DrainAndSteal:
                                    {
                                        s.ApplyBCards(target.Character, Session.Character);
                                    }
                                    break;
                            }
                        });

                        if (battleEntity?.ShellWeaponEffects != null)
                        {
                            foreach (ShellEffectDTO shell in battleEntity.ShellWeaponEffects)
                            {
                                switch (shell.Effect)
                                {
                                    case (byte)ShellWeaponEffectType.Blackout:
                                        {
                                            Buff buff = new Buff(7, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value
                                                - (shell.Value
                                                   * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                          s.Effect == (byte)ShellArmorEffectType.ReducedStun)?.Value
                                                      + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                          s.Effect == (byte)ShellArmorEffectType.ReducedAllStun)?.Value
                                                      + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte)ShellArmorEffectType.ReducedAllNegativeEffect)
                                                          ?.Value) / 100D))
                                            {
                                                target.Character.AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.DeadlyBlackout:
                                        {
                                            Buff buff = new Buff(66, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value
                                                - (shell.Value
                                                   * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                          s.Effect == (byte)ShellArmorEffectType.ReducedAllStun)?.Value
                                                      + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                              s.Effect == (byte)ShellArmorEffectType.ReducedAllNegativeEffect)
                                                          ?.Value) / 100D))
                                            {
                                                target.Character.AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.MinorBleeding:
                                        {
                                            Buff buff = new Buff(1, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value
                                                - (shell.Value * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedMinorBleeding)?.Value
                                                                  + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedBleedingAndMinorBleeding)?.Value
                                                                  + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedAllBleedingType)?.Value
                                                                  + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedAllNegativeEffect)?.Value) / 100D))
                                            {
                                                target.Character.AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.Bleeding:
                                        {
                                            Buff buff = new Buff(21, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value
                                                - (shell.Value * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedBleedingAndMinorBleeding)?.Value
                                                                  + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedAllBleedingType)?.Value
                                                                  + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedAllNegativeEffect)?.Value) / 100D))
                                            {
                                                target.Character.AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.HeavyBleeding:
                                        {
                                            Buff buff = new Buff(42, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value
                                                - (shell.Value * (battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedAllBleedingType)?.Value
                                                                  + battleEntityDefense.ShellArmorEffects?.Find(s =>
                                                                      s.Effect == (byte)ShellArmorEffectType
                                                                          .ReducedAllNegativeEffect)?.Value) / 100D))
                                            {
                                                target.Character.AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.Freeze:
                                        {
                                            Buff buff = new Buff(27, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value - (shell.Value
                                                                                              * (battleEntityDefense
                                                                                                     .ShellArmorEffects?.Find(
                                                                                                         s =>
                                                                                                             s.Effect ==
                                                                                                             (byte)
                                                                                                             ShellArmorEffectType
                                                                                                                 .ReducedFreeze)
                                                                                                     ?.Value
                                                                                                 + battleEntityDefense
                                                                                                     .ShellArmorEffects?.Find(
                                                                                                         s =>
                                                                                                             s.Effect ==
                                                                                                             (byte)
                                                                                                             ShellArmorEffectType
                                                                                                                 .ReducedAllNegativeEffect)
                                                                                                     ?.Value) / 100D))
                                            {
                                                target.Character.AddBuff(buff);
                                            }

                                            break;
                                        }
                                }
                            }
                        }
                    }

                    switch (hitRequest.TargetHitType)
                    {
                        case TargetHitType.SingleTargetHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SingleTargetHitCombo:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character), hitRequest.SkillCombo.Animation,
                                hitRequest.SkillCombo.Effect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SingleAOETargetHit:
                            switch (hitmode)
                            {
                                case 1:
                                    hitmode = 4;
                                    break;

                                case 3:
                                    break;

                                default:
                                    hitmode = 0;
                                    break;
                            }

                            if (hitRequest.ShowTargetHitAnimation)
                            {
                                hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(
                                    UserType.Player, hitRequest.Session.Character.CharacterId, 1,
                                    target.Character.CharacterId, hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect, 0, 0, isAlive,
                                    (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), 0, 0,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                            }

                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.AOETargetHit:
                            switch (hitmode)
                            {
                                case 1:
                                    hitmode = 4;
                                    break;

                                case 3:
                                    break;

                                default:
                                    hitmode = 0;
                                    break;
                            }

                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, hitmode,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.ZoneHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.MapX, hitRequest.MapY, isAlive,
                                (int)(target.Character.Hp / (float)target.Character.HPLoad() * 100), damage, 5,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        case TargetHitType.SpecialZoneHit:
                            hitRequest.Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                hitRequest.Session.Character.CharacterId, 1, target.Character.CharacterId,
                                hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character), hitRequest.Skill.AttackAnimation,
                                hitRequest.SkillEffect, hitRequest.Session.Character.PositionX,
                                hitRequest.Session.Character.PositionY, isAlive,
                                (int)(target.Character.Hp / target.Character.HPLoad() * 100), damage, 0,
                                (byte)(hitRequest.Skill.SkillType - 1)));
                            break;

                        default:
                            Logger.Warn("Not Implemented TargetHitType Handling!");
                            break;
                    }
                    target.SendPacket(target.Character.GenerateCond());

                    Observable.Timer(TimeSpan.FromSeconds(1))
                        .Subscribe(o => target.Character.MapInstance?.Broadcast(target.Character.GenerateSpeed()));
                }
                else
                {
                    // monster already has been killed, send cancel
                    if (target != null)
                    {
                        hitRequest?.Session.SendPacket(StaticPacketHelper.Cancel(2, target.Character.CharacterId));
                    }
                }
            }
            catch
            {
            }
        }

        private void TargetHit(int castingId, int targetId, bool isPvp = false)
        {
            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"),
                    0));
                return;
            }

            List<CharacterSkill> skills = Session.Character.UseSp
                ? Session.Character.SkillsSp?.GetAllItems()
                : Session.Character.Skills?.GetAllItems();
            if (skills != null)
            {
                CharacterSkill
                    ski = skills.Find(s =>
                        s.Skill?.CastId
                        == castingId); // && (s.Skill?.UpgradeSkill == 0 || s.Skill?.UpgradeSkill == 3));
                if (castingId != 0)
                {
                    Session.SendPacket("ms_c 0");
                }

                if (ski != null)
                {
                    if (!Session.Character.WeaponLoaded(ski) || !ski.CanBeUsed(Session.Character))
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        return;
                    }

                    foreach (BCard bc in ski.Skill.BCards.Where(s => s.Type.Equals((byte)CardType.MeditationSkill)))
                    {
                        bc.ApplyBCards(Session.Character);
                    }

                    if (Session.Character.Mp >= ski.Skill.MpCost && Session.HasCurrentMapInstance)
                    {
                        // AOE Target hit
                        if (ski.Skill.TargetType == 1 && ski.Skill.HitType == 1)
                        {
                            if (!Session.Character.HasGodMode)
                            {
                                Session.Character.Mp -= ski.Skill.MpCost;
                            }

                            if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                            {
                                Session.SendPackets(Session.Character.GenerateQuicklist());
                            }


                            Session.SendPacket(Session.Character.GenerateStat());
                            CharacterSkill skillinfo = Session.Character.Skills.FirstOrDefault(s =>
                                s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                            && s.Skill.SkillType == 2);
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, 1, Session.Character.CharacterId,
                                ski.Skill.CastAnimation, skillinfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                ski.Skill.SkillVNum));

                            // Generate scp
                            ski.LastUse = DateTime.Now;
                            if (ski.Skill.CastEffect != 0)
                            {
                                Thread.Sleep(ski.Skill.CastTime * 100);
                            }

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.Skill.SkillVNum,
                                ski.Skill.GetCooldown(Session.Character), ski.Skill.AttackAnimation,
                                skillinfo?.Skill.Effect ?? ski.Skill.Effect, Session.Character.PositionX,
                                Session.Character.PositionY, true,
                                (int)(Session.Character.Hp / Session.Character.HPLoad() * 100), 0, -2,
                                (byte)(ski.Skill.SkillType - 1)));
                            if (ski.Skill.TargetRange != 0)
                            {
                                foreach (ClientSession character in ServerManager.Instance.Sessions.Where(s =>
                                    s.CurrentMapInstance == Session.CurrentMapInstance
                                    && s.Character.CharacterId != Session.Character.CharacterId
                                    && s.Character.IsInRange(Session.Character.PositionX, Session.Character.PositionY,
                                        ski.Skill.TargetRange)))
                                {
                                    if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                        s.MapTypeId == (short)MapTypeEnum.Act4))
                                    {
                                        if (Session.Character.Faction != character.Character.Faction
                                            && Session.CurrentMapInstance.Map.MapId != 130
                                            && Session.CurrentMapInstance.Map.MapId != 131)
                                        {
                                            PvpHit(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill),
                                                character);
                                        }
                                    }
                                    else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                        m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                    {
                                        if (Session.Character.Group == null
                                            || !Session.Character.Group.IsMemberOfGroup(character.Character.CharacterId)
                                        )
                                        {
                                            PvpHit(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill),
                                                character);
                                        }
                                    }
                                    else if (Session.CurrentMapInstance.IsPVP)
                                    {
                                        if (Session.Character.Group == null
                                            || !Session.Character.Group.IsMemberOfGroup(character.Character.CharacterId)
                                        )
                                        {
                                            PvpHit(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill),
                                                character);
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    }
                                }

                                foreach (MapMonster mon in Session.CurrentMapInstance
                                    .GetListMonsterInRange(Session.Character.PositionX, Session.Character.PositionY,
                                        ski.Skill.TargetRange).Where(s => s.CurrentHp > 0))
                                {
                                    mon.HitQueue.Enqueue(new HitRequest(TargetHitType.AOETargetHit, Session, ski.Skill,
                                        skillinfo?.Skill.Effect ?? ski.Skill.Effect));
                                }
                            }
                        }
                        else if (ski.Skill.TargetType == 2 && ski.Skill.HitType == 0)
                        {
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, 1, Session.Character.CharacterId,
                                ski.Skill.CastAnimation, ski.Skill.CastEffect, ski.Skill.SkillVNum));
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                Session.Character.CharacterId, 1, targetId, ski.Skill.SkillVNum, ski.Skill.GetCooldown(Session.Character),
                                ski.Skill.AttackAnimation, ski.Skill.Effect, Session.Character.PositionX,
                                Session.Character.PositionY, true,
                                (int)(Session.Character.Hp / Session.Character.HPLoad() * 100), 0, -1,
                                (byte)(ski.Skill.SkillType - 1)));

                            ClientSession target = ServerManager.Instance.GetSessionByCharacterId(targetId) ?? Session;
                            ski.Skill.BCards.Where(s => !s.Type.Equals((byte)CardType.MeditationSkill)).ToList()
                                .ForEach(s =>
                                {
                                    if (target == null)
                                    {
                                        if (s.BuffCard?.BuffType != BuffType.Bad)
                                        {
                                            s.ApplyBCards(Session.Character, Session.Character);
                                        }
                                    }
                                    else
                                    {
                                        if (s.BuffCard?.BuffType == BuffType.Bad)
                                        {
                                            s.ApplyBCards(target.Character, Session.Character);
                                        }
                                        else
                                        {
                                            s.ApplyBCards(Session.Character.IsEnemy(target.Character) ? Session.Character : target.Character, Session.Character);
                                        }
                                    }
                                });

                            ski.LastUse = DateTime.Now;
                        }
                        else if (ski.Skill.TargetType == 1 && ski.Skill.HitType != 1)
                        {
                            if (!Session.Character.Cooldowns.ContainsKey(ski.SkillVNum))
                            {
                                switch (ski.SkillVNum)
                                {
                                    case 915: // Bomber
                                        {
                                            if (Session.Character.MapInstance.Monsters.FirstOrDefault(m =>
                                                m.Owner?.CharacterId == Session.Character.CharacterId && m.MonsterVNum == 945) != null)
                                            {
                                                Session.Character.Cooldowns.TryAdd(ski.SkillVNum, 300);

                                                Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(o =>
                                                    Session.Character.Cooldowns.TryRemove(ski.SkillVNum, out _));
                                            }
                                        }
                                        break;
                                }
                            }

                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Player,
                                Session.Character.CharacterId, 1, Session.Character.CharacterId,
                                ski.Skill.CastAnimation, ski.Skill.CastEffect, ski.Skill.SkillVNum));
                            Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                Session.Character.CharacterId, 1, Session.Character.CharacterId, ski.Skill.SkillVNum,
                                ski.Skill.GetCooldown(Session.Character), ski.Skill.AttackAnimation, ski.Skill.Effect,
                                Session.Character.PositionX, Session.Character.PositionY, true,
                                (int)(Session.Character.Hp / Session.Character.HPLoad() * 100), 0, -1,
                                (byte)(ski.Skill.SkillType - 1)));
                            switch (ski.Skill.HitType)
                            {
                                case 2:
                                    IEnumerable<ClientSession> clientSessions =
                                        Session.CurrentMapInstance.Sessions?.Where(s =>
                                            s.Character.IsInRange(Session.Character.PositionX,
                                                Session.Character.PositionY, ski.Skill.TargetRange));
                                    if (clientSessions != null)
                                    {
                                        foreach (ClientSession target in clientSessions)
                                        {
                                            ski.Skill.BCards.Where(s => !s.Type.Equals((byte)CardType.MeditationSkill))
                                                    .ToList().ForEach(s => s.ApplyBCards(target.Character, Session.Character));

                                        }
                                    }

                                    ski.LastUse = DateTime.Now;
                                    break;

                                case 4:
                                case 0:
                                    ski.Skill.BCards.Where(s => !s.Type.Equals((byte)CardType.MeditationSkill))
                                        .ToList().ForEach(s => s.ApplyBCards(Session.Character, Session.Character));
                                    ski.LastUse = DateTime.Now;
                                    break;
                            }
                        }
                        else if (ski.Skill.TargetType == 0) // monster target
                        {

                            if (isPvp)
                            {
                                ClientSession playerToAttack = ServerManager.Instance.GetSessionByCharacterId(targetId);
                                if (playerToAttack != null && Session.Character.Mp >= ski.Skill.MpCost)
                                {
                                    if (Map.GetDistance(
                                            new MapCell
                                            {
                                                X = Session.Character.PositionX,
                                                Y = Session.Character.PositionY
                                            },
                                            new MapCell
                                            {
                                                X = playerToAttack.Character.PositionX,
                                                Y = playerToAttack.Character.PositionY
                                            }) <= ski.Skill.Range + 5)
                                    {
                                        if (!Session.Character.HasGodMode)
                                        {
                                            Session.Character.Mp -= ski.Skill.MpCost;
                                        }

                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }

                                        Session.SendPacket(Session.Character.GenerateStat());
                                        CharacterSkill characterSkillInfo = Session.Character.Skills.FirstOrDefault(s =>
                                            s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                                        && s.Skill.SkillType == 2);
                                        Session.CurrentMapInstance.Broadcast(
                                            StaticPacketHelper.CastOnTarget(UserType.Player,
                                                Session.Character.CharacterId, 1, targetId, ski.Skill.CastAnimation,
                                                characterSkillInfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                                ski.Skill.SkillVNum));
                                        Session.Character.Skills.Where(s => s.Id != ski.Id).ForEach(i => i.Hit = 0);

                                        // Generate scp
                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        ski.LastUse = DateTime.Now;

                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            int count = 0;
                                            if (playerToAttack.CurrentMapInstance == Session.CurrentMapInstance
                                                && playerToAttack.Character.CharacterId !=
                                                Session.Character.CharacterId)
                                            {
                                                if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                    s.MapTypeId == (short)MapTypeEnum.Act4))
                                                {
                                                    if (Session.Character.Faction != playerToAttack.Character.Faction
                                                        && Session.CurrentMapInstance.Map.MapId != 130
                                                        && Session.CurrentMapInstance.Map.MapId != 131)
                                                    {
                                                        count++;
                                                        PvpHit(
                                                            new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                                ski.Skill), playerToAttack);
                                                    }
                                                }
                                                else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                                    m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                {
                                                    if (Session.Character.Group == null
                                                        || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                            .Character.CharacterId))
                                                    {
                                                        count++;
                                                        PvpHit(
                                                            new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                                ski.Skill), playerToAttack);
                                                    }
                                                }
                                                else if (Session.CurrentMapInstance.IsPVP)
                                                {
                                                    if (Session.Character.Group == null
                                                        || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                            .Character.CharacterId))
                                                    {
                                                        count++;
                                                        PvpHit(
                                                            new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                                ski.Skill), playerToAttack);
                                                    }
                                                }
                                            }

                                            foreach (long id in Session.Character.MTListTargetQueue
                                                .Where(s => s.EntityType == UserType.Player).Select(s => s.TargetId))
                                            {
                                                ClientSession character =
                                                    ServerManager.Instance.GetSessionByCharacterId(id);
                                                if (character != null
                                                    && character.CurrentMapInstance == Session.CurrentMapInstance
                                                    && character.Character.CharacterId != Session.Character.CharacterId)
                                                {
                                                    if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                        s.MapTypeId == (short)MapTypeEnum.Act4))
                                                    {
                                                        if (Session.Character.Faction != character.Character.Faction
                                                            && Session.CurrentMapInstance.Map.MapId != 130
                                                            && Session.CurrentMapInstance.Map.MapId != 131)
                                                        {
                                                            count++;
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill), character);
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                                        m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(character
                                                                .Character
                                                                .CharacterId))
                                                        {
                                                            count++;
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill), character);
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.IsPVP)
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(character
                                                                .Character
                                                                .CharacterId))
                                                        {
                                                            count++;
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill), character);
                                                        }
                                                    }
                                                }
                                            }

                                            if (count == 0)
                                            {
                                                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                            }
                                        }
                                        else
                                        {
                                            // check if we will hit mutltiple targets
                                            if (ski.Skill.TargetRange != 0)
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    IEnumerable<ClientSession> playersInAoeRange =
                                                        ServerManager.Instance.Sessions.Where(s =>
                                                            s.CurrentMapInstance == Session.CurrentMapInstance
                                                            && s.Character.CharacterId != Session.Character.CharacterId
                                                            && s.Character.IsInRange(Session.Character.PositionX,
                                                                Session.Character.PositionY, ski.Skill.TargetRange));
                                                    int count = 0;
                                                    if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                        s.MapTypeId == (short)MapTypeEnum.Act4))
                                                    {
                                                        if (Session.Character.Faction
                                                            != playerToAttack.Character.Faction
                                                            && Session.CurrentMapInstance.Map.MapId != 130
                                                            && Session.CurrentMapInstance.Map.MapId != 131)
                                                        {
                                                            count++;
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                    Session, ski.Skill, skillCombo: skillCombo),
                                                                playerToAttack);
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                                        m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                                .Character.CharacterId))
                                                        {
                                                            count++;
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                    Session, ski.Skill, skillCombo: skillCombo),
                                                                playerToAttack);
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.IsPVP)
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                                .Character.CharacterId))
                                                        {
                                                            count++;
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                    Session, ski.Skill, skillCombo: skillCombo),
                                                                playerToAttack);
                                                        }
                                                    }

                                                    foreach (ClientSession character in playersInAoeRange)
                                                    {
                                                        if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                            s.MapTypeId == (short)MapTypeEnum.Act4))
                                                        {
                                                            if (Session.Character.Faction
                                                                != character.Character.Faction
                                                                && Session.CurrentMapInstance.Map.MapId != 130
                                                                && Session.CurrentMapInstance.Map.MapId != 131)
                                                            {
                                                                count++;
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                        Session, ski.Skill, skillCombo: skillCombo),
                                                                    character);
                                                            }
                                                        }
                                                        else if (Session.CurrentMapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                        {
                                                            if (Session.Character.Group == null
                                                                || !Session.Character.Group.IsMemberOfGroup(
                                                                    character.Character.CharacterId))
                                                            {
                                                                count++;
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                        Session, ski.Skill, skillCombo: skillCombo),
                                                                    character);
                                                            }
                                                        }
                                                        else if (Session.CurrentMapInstance.IsPVP)
                                                        {
                                                            if (Session.Character.Group == null
                                                                || !Session.Character.Group.IsMemberOfGroup(
                                                                    character.Character.CharacterId))
                                                            {
                                                                count++;
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                        Session, ski.Skill, skillCombo: skillCombo),
                                                                    character);
                                                            }
                                                        }
                                                    }

                                                    if (playerToAttack.Character.Hp <= 0 || count == 0)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    IEnumerable<ClientSession> playersInAoeRange =
                                                        ServerManager.Instance.Sessions.Where(s =>
                                                            s.CurrentMapInstance == Session.CurrentMapInstance
                                                            && s.Character.CharacterId != Session.Character.CharacterId
                                                            && s.Character.IsInRange(Session.Character.PositionX,
                                                                Session.Character.PositionY, ski.Skill.TargetRange));

                                                    // hit the targetted monster
                                                    if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                        s.MapTypeId == (short)MapTypeEnum.Act4))
                                                    {
                                                        if (Session.Character.Faction
                                                            != playerToAttack.Character.Faction)
                                                        {
                                                            if (Session.CurrentMapInstance.Map.MapId != 130
                                                                && Session.CurrentMapInstance.Map.MapId != 131)
                                                            {
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                        Session, ski.Skill), playerToAttack);
                                                            }
                                                            else
                                                            {
                                                                Session.SendPacket(
                                                                    StaticPacketHelper.Cancel(2, targetId));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                                        m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                                .Character.CharacterId))
                                                        {
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill), playerToAttack);
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.IsPVP)
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                                .Character.CharacterId))
                                                        {
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill), playerToAttack);
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    //hit all other monsters
                                                    foreach (ClientSession character in playersInAoeRange)
                                                    {
                                                        if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                            s.MapTypeId == (short)MapTypeEnum.Act4))
                                                        {
                                                            if (Session.Character.Faction
                                                                != character.Character.Faction
                                                                && Session.CurrentMapInstance.Map.MapId != 130
                                                                && Session.CurrentMapInstance.Map.MapId != 131)
                                                            {
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                        Session, ski.Skill), character);
                                                            }
                                                        }
                                                        else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                                            m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                        {
                                                            if (Session.Character.Group == null
                                                                || !Session.Character.Group.IsMemberOfGroup(
                                                                    character.Character.CharacterId))
                                                            {
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                        Session, ski.Skill), character);
                                                            }
                                                        }
                                                        else if (Session.CurrentMapInstance.IsPVP)
                                                        {
                                                            if (Session.Character.Group == null
                                                                || !Session.Character.Group.IsMemberOfGroup(
                                                                    character.Character.CharacterId))
                                                            {
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                        Session, ski.Skill), character);
                                                            }
                                                        }
                                                    }

                                                    if (playerToAttack.Character.Hp <= 0)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                        s.MapTypeId == (short)MapTypeEnum.Act4))
                                                    {
                                                        if (Session.Character.Faction
                                                            != playerToAttack.Character.Faction)
                                                        {
                                                            if (Session.CurrentMapInstance.Map.MapId != 130
                                                                && Session.CurrentMapInstance.Map.MapId != 131)
                                                            {
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                        Session, ski.Skill, skillCombo: skillCombo),
                                                                    playerToAttack);
                                                            }
                                                            else
                                                            {
                                                                Session.SendPacket(
                                                                    StaticPacketHelper.Cancel(2, targetId));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                                        m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                                .Character.CharacterId))
                                                        {
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                    Session, ski.Skill, skillCombo: skillCombo),
                                                                playerToAttack);
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.IsPVP)
                                                    {
                                                        if (Session.CurrentMapInstance.MapInstanceId
                                                            != ServerManager.Instance.FamilyArenaInstance.MapInstanceId)
                                                        {
                                                            if (Session.Character.Group == null
                                                                || !Session.Character.Group.IsMemberOfGroup(
                                                                    playerToAttack
                                                                        .Character.CharacterId))
                                                            {
                                                                PvpHit(new HitRequest(TargetHitType.SingleTargetHit,
                                                                    Session,
                                                                    ski.Skill), playerToAttack);
                                                            }
                                                            else
                                                            {
                                                                Session.SendPacket(
                                                                    StaticPacketHelper.Cancel(2, targetId));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (Session.Character.Faction
                                                                != playerToAttack.Character.Faction)
                                                            {
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleTargetHit,
                                                                        Session, ski.Skill), playerToAttack);
                                                            }
                                                            else
                                                            {
                                                                Session.SendPacket(
                                                                    StaticPacketHelper.Cancel(2, targetId));
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                                        s.MapTypeId == (short)MapTypeEnum.Act4))
                                                    {
                                                        if (Session.Character.Faction
                                                            != playerToAttack.Character.Faction)
                                                        {
                                                            if (Session.CurrentMapInstance.Map.MapId != 130
                                                                && Session.CurrentMapInstance.Map.MapId != 131)
                                                            {
                                                                PvpHit(
                                                                    new HitRequest(TargetHitType.SingleTargetHit,
                                                                        Session, ski.Skill), playerToAttack);
                                                            }
                                                            else
                                                            {
                                                                Session.SendPacket(
                                                                    StaticPacketHelper.Cancel(2, targetId));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.Map.MapTypes.Any(m =>
                                                        m.MapTypeId == (short)MapTypeEnum.PVPMap))
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                                .Character.CharacterId))
                                                        {
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHit, Session,
                                                                    ski.Skill), playerToAttack);
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else if (Session.CurrentMapInstance.IsPVP)
                                                    {
                                                        if (Session.Character.Group == null
                                                            || !Session.Character.Group.IsMemberOfGroup(playerToAttack
                                                                .Character.CharacterId))
                                                        {
                                                            PvpHit(
                                                                new HitRequest(TargetHitType.SingleTargetHit, Session,
                                                                    ski.Skill), playerToAttack);
                                                        }
                                                        else
                                                        {
                                                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                }
                            }
                            else
                            {
                                MapMonster monsterToAttack = Session.CurrentMapInstance.GetMonster(targetId);
                                if (monsterToAttack != null && Session.Character.Mp >= ski.Skill.MpCost)
                                {
                                    if (Map.GetDistance(
                                            new MapCell
                                            {
                                                X = Session.Character.PositionX,
                                                Y = Session.Character.PositionY
                                            },
                                            new MapCell { X = monsterToAttack.MapX, Y = monsterToAttack.MapY })
                                        <= ski.Skill.Range + 5 + monsterToAttack.Monster.BasicArea)
                                    {
                                        if (!Session.Character.HasGodMode)
                                        {
                                            Session.Character.Mp -= ski.Skill.MpCost;
                                        }

                                        if (Session.Character.UseSp && ski.Skill.CastEffect != -1)
                                        {
                                            Session.SendPackets(Session.Character.GenerateQuicklist());
                                        }
                                        // check this
                                        monsterToAttack.Monster.BCards.Where(s => s.CastType == 1).ToList()
                                            .ForEach(s => s.ApplyBCards(this));
                                        Session.SendPacket(Session.Character.GenerateStat());
                                        CharacterSkill characterSkillInfo = Session.Character.Skills.FirstOrDefault(s =>
                                            s.Skill.UpgradeSkill == ski.Skill.SkillVNum && s.Skill.Effect > 0
                                                                                        && s.Skill.SkillType == 2);

                                        Session.CurrentMapInstance.Broadcast(StaticPacketHelper.CastOnTarget(
                                            UserType.Player, Session.Character.CharacterId, 3,
                                            monsterToAttack.MapMonsterId, ski.Skill.CastAnimation,
                                            characterSkillInfo?.Skill.CastEffect ?? ski.Skill.CastEffect,
                                            ski.Skill.SkillVNum));
                                        Session.Character.Skills.Where(s => s.Id != ski.Id).ForEach(i => i.Hit = 0);

                                        // Generate scp
                                        if ((DateTime.Now - ski.LastUse).TotalSeconds > 3)
                                        {
                                            ski.Hit = 0;
                                        }
                                        else
                                        {
                                            ski.Hit++;
                                        }

                                        ski.LastUse = DateTime.Now;
                                        if (ski.Skill.CastEffect != 0)
                                        {
                                            Thread.Sleep(ski.Skill.CastTime * 100);
                                        }

                                        if (ski.Skill.HitType == 3)
                                        {
                                            monsterToAttack.HitQueue.Enqueue(new HitRequest(
                                                TargetHitType.SingleAOETargetHit, Session, ski.Skill,
                                                characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect,
                                                showTargetAnimation: true));

                                            foreach (long id in Session.Character.MTListTargetQueue
                                                .Where(s => s.EntityType == UserType.Monster).Select(s => s.TargetId))
                                            {
                                                MapMonster mon = Session.CurrentMapInstance.GetMonster(id);
                                                if (mon?.CurrentHp > 0)
                                                {
                                                    mon.HitQueue.Enqueue(new HitRequest(
                                                        TargetHitType.SingleAOETargetHit, Session, ski.Skill,
                                                        characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (ski.Skill.TargetRange != 0) // check if we will hit mutltiple targets
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    List<MapMonster> monstersInAoeRange = Session.CurrentMapInstance?
                                                        .GetListMonsterInRange(monsterToAttack.MapX,
                                                            monsterToAttack.MapY, ski.Skill.TargetRange).ToList();
                                                    if (monstersInAoeRange.Count != 0)
                                                    {
                                                        foreach (MapMonster mon in monstersInAoeRange)
                                                        {
                                                            mon.HitQueue.Enqueue(
                                                                new HitRequest(TargetHitType.SingleTargetHitCombo,
                                                                    Session, ski.Skill, skillCombo: skillCombo));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    if (!monsterToAttack.IsAlive)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                                else
                                                {
                                                    List<MapMonster> monstersInAoeRange = Session.CurrentMapInstance?
                                                                                              .GetListMonsterInRange(
                                                                                                  monsterToAttack.MapX,
                                                                                                  monsterToAttack.MapY,
                                                                                                  ski.Skill.TargetRange)
                                                                                              ?.ToList();

                                                    //hit the targetted monster
                                                    monsterToAttack.HitQueue.Enqueue(
                                                        new HitRequest(TargetHitType.SingleAOETargetHit, Session,
                                                            ski.Skill,
                                                            characterSkillInfo?.Skill.Effect ?? ski.Skill.Effect,
                                                            showTargetAnimation: true));

                                                    //hit all other monsters
                                                    if (monstersInAoeRange != null && monstersInAoeRange.Count != 0)
                                                    {
                                                        foreach (MapMonster mon in monstersInAoeRange.Where(m =>
                                                            m.MapMonsterId != monsterToAttack.MapMonsterId)
                                                        ) //exclude targetted monster
                                                        {
                                                            mon.HitQueue.Enqueue(
                                                                new HitRequest(TargetHitType.SingleAOETargetHit,
                                                                    Session, ski.Skill,
                                                                    characterSkillInfo?.Skill.Effect ??
                                                                    ski.Skill.Effect));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }

                                                    if (!monsterToAttack.IsAlive)
                                                    {
                                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ComboDTO skillCombo = ski.Skill.Combos.Find(s => ski.Hit == s.Hit);
                                                if (skillCombo != null)
                                                {
                                                    if (ski.Skill.Combos.OrderByDescending(s => s.Hit).First().Hit
                                                        == ski.Hit)
                                                    {
                                                        ski.Hit = 0;
                                                    }

                                                    monsterToAttack.HitQueue.Enqueue(
                                                        new HitRequest(TargetHitType.SingleTargetHitCombo, Session,
                                                            ski.Skill, skillCombo: skillCombo));
                                                }
                                                else
                                                {
                                                    monsterToAttack.HitQueue.Enqueue(
                                                        new HitRequest(TargetHitType.SingleTargetHit, Session,
                                                            ski.Skill));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                                }
                            }

                            if (ski.Skill.HitType == 3)
                            {
                                Session.Character.MTListTargetQueue.Clear();
                            }
                        }
                        else
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        }

                        if (ski.Skill.UpgradeSkill == 3 && ski.Skill.SkillType == 1)
                        {
                            Session.SendPacket(StaticPacketHelper.SkillResetWithCoolDown(castingId, ski.Skill.GetCooldown(Session.Character)));
                        }

                        Session.SendPacketAfter(StaticPacketHelper.SkillReset(castingId), ski.Skill.GetCooldown(Session.Character) * 100);
                    }
                    else
                    {
                        Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MP"), 10));
                    }
                }
            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2, targetId));
            }

            if (Session.Character.SkillComboCount > 7)
            {
                Session.SendPackets(Session.Character.GenerateQuicklist());
                Session.SendPacket("mslot 0 -1");
            }

            Session.Character.LastSkillUse = DateTime.Now;
        }

        private void ZoneHit(int castingid, short x, short y)
        {
            List<CharacterSkill> skills = Session.Character.UseSp
                ? Session.Character.SkillsSp.GetAllItems()
                : Session.Character.Skills.GetAllItems();
            CharacterSkill characterSkill = skills?.Find(s => s.Skill?.CastId == castingid);
            if (!Session.Character.WeaponLoaded(characterSkill) || !Session.HasCurrentMapInstance)
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
                return;
            }


            if (characterSkill != null && characterSkill.CanBeUsed(Session.Character))
            {
                if (Session.Character.Mp >= characterSkill.Skill.MpCost)
                {
                    Session.CurrentMapInstance?.Broadcast(
                        $"ct_n 1 {Session.Character.CharacterId} 3 -1 {characterSkill.Skill.CastAnimation} {characterSkill.Skill.CastEffect} {characterSkill.Skill.SkillVNum}");
                    characterSkill.LastUse = DateTime.Now;
                    if (!Session.Character.HasGodMode)
                    {
                        Session.Character.Mp -= characterSkill.Skill.MpCost;
                    }

                    Session.SendPacket(Session.Character.GenerateStat());
                    characterSkill.LastUse = DateTime.Now;
                    Observable.Timer(TimeSpan.FromMilliseconds(characterSkill.Skill.CastTime * 100)).Subscribe(o =>
                    {
                        Session.Character.LastSkillUse = DateTime.Now;

                        Session.CurrentMapInstance?.Broadcast(
                            $"bs 1 {Session.Character.CharacterId} {x} {y} {characterSkill.Skill.SkillVNum} {characterSkill.Skill.GetCooldown(Session.Character)} {characterSkill.Skill.AttackAnimation} {characterSkill.Skill.Effect} 0 0 1 1 0 0 0");

                        IEnumerable<MapMonster> monstersInRange = Session.CurrentMapInstance?.GetListMonsterInRange(x, y, characterSkill.Skill.TargetRange).ToList();

                        if (monstersInRange != null)
                        {
                            foreach (MapMonster mon in monstersInRange.Where(s => s.CurrentHp > 0))
                            {
                                foreach (BCard bcard in characterSkill.Skill.BCards)
                                {
                                    var bf = new Buff((short)bcard.SecondData, Session.Character.Level);
                                    switch (bf.Card?.BuffType)
                                    {
                                        case BuffType.Bad:
                                            bcard.ApplyBCards(mon, Session.Character);
                                            break;
                                    }
                                }
                                mon.HitQueue.Enqueue(new HitRequest(TargetHitType.ZoneHit, Session, characterSkill.Skill, x, y));
                            }
                        }

                        foreach (BCard bcard in characterSkill.Skill.BCards)
                        {
                            var bf = new Buff((short)bcard.SecondData, Session.Character.Level);
                            switch (bf.Card?.BuffType)
                            {
                                case BuffType.Good:
                                case BuffType.Neutral:
                                    bcard.ApplyBCards(Session.Character, Session.Character);
                                    break;
                            }
                        }

                        foreach (ClientSession character in ServerManager.Instance.Sessions.Where(s =>
                            s.CurrentMapInstance == Session.CurrentMapInstance && s.Character.CharacterId != Session.Character.CharacterId &&
                            s.Character.IsInRange(x, y, characterSkill.Skill.TargetRange)))
                        {
                            if (Session.CurrentMapInstance == null || !Session.CurrentMapInstance.IsPVP)
                            {
                                continue;
                            }

                            if (Session.Character.Group == null || !Session.Character.Group.IsMemberOfGroup(character.Character.CharacterId))
                            {
                                PvpHit(new HitRequest(TargetHitType.ZoneHit, Session, characterSkill.Skill, mapX: x, mapY: y), character);
                            }
                        }
                    });

                    Observable.Timer(TimeSpan.FromMilliseconds(characterSkill.Skill.GetCooldown(Session.Character) * 100)).Subscribe(o =>
                    {
                        Session.SendPacket($"sr {castingid}");
                    });
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MP"), 10));
                    Session.SendPacket("cancel 2 0");
                }
            }
            else
            {
                Session.SendPacket($"cancel 0 {(characterSkill != null ? castingid : 0)}");
            }
        }

        #endregion
    }
}