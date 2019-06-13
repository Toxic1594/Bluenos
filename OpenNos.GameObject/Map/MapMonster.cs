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

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Battle;
using OpenNos.GameObject.Event;
using OpenNos.GameObject.Helpers;
using OpenNos.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenNos.GameObject.Networking;
using static OpenNos.Domain.BCardType;
using System.Threading.Tasks;

namespace OpenNos.GameObject
{
    public class MapMonster : MapMonsterDTO
    {
        #region Members

        public readonly object PveLockObject = new object();

        private int _movetime;

        private bool _noAttack;

        private bool _noMove;

        private Random _random;

        private int _waitCount;

        private int killedbyFaction;

        public struct DMGList
        {
            public ClientSession ClientSession;
            public long Damage;
        }

        #endregion

        #region Instantiation

        public MapMonster()
        {
            Buff = new ThreadSafeSortedList<short, Buff>();
            HitQueue = new ConcurrentQueue<HitRequest>();
            OnDeathEvents = new List<EventContainer>();
            OnNoticeEvents = new List<EventContainer>();
            BCardDisposables = new ConcurrentDictionary<short, IDisposable>();
        }

        public MapMonster(MapMonsterDTO input) : this()
        {
            IsDisabled = input.IsDisabled;
            IsMoving = input.IsMoving;
            MapId = input.MapId;
            MapMonsterId = input.MapMonsterId;
            MapX = input.MapX;
            MapY = input.MapY;
            MonsterVNum = input.MonsterVNum;
            Position = input.Position;
        }

        #endregion

        #region Properties

        public Node[][] BrushFireJagged { get; set; }

        public Character Owner { get; set; }

        public int AliveTime { get; set; }

        public bool IsKamikaze { get; set; }

        public ConcurrentDictionary<short, IDisposable> BCardDisposables;

        public ThreadSafeSortedList<short, Buff> Buff { get; set; }

        public int CurrentHp { get; set; }

        public int CurrentMp { get; set; }

        public IDictionary<long, long> DamageList { get; private set; }

        public DateTime Death { get; set; }

        public ConcurrentQueue<HitRequest> HitQueue { get; }

        public bool IsAlive { get; set; }

        public IDisposable Life { get; set; }

        public bool IsBonus { get; set; }

        public bool IsBoss { get; set; }

        public bool IsHostile { get; set; }

        public bool IsTarget { get; set; }

        public DateTime LastEffect { get; set; }

        public DateTime LastMonsterAggro { get; set; }

        public DateTime LastMove { get; set; }

        public DateTime LastSkill { get; set; }

        public DateTime LastSelfDamage { get; set; }

        public IDisposable LifeEvent { get; set; }

        public MapInstance MapInstance { get; set; }

        public int MaxHp { get; set; }

        public int MaxMp { get; set; }

        public NpcMonster Monster { get; private set; }

        public ZoneEvent MoveEvent { get; set; }

        public bool NoAggresiveIcon { get; internal set; }

        public byte NoticeRange { get; set; }

        public List<EventContainer> OnDeathEvents { get; set; }

        public List<EventContainer> OnNoticeEvents { get; set; }

        public List<Node> Path { get; set; }

        public bool? ShouldRespawn { get; set; }

        public List<NpcMonsterSkill> Skills { get; set; }

        public bool Started { get; internal set; }

        public long Target { get; set; }

        public UserType TargetType { get; set; }

        private short FirstX { get; set; }

        private short FirstY { get; set; }
        public DateTime LastHPRemove { get; private set; }
        public long Faction { get; internal set; }

        #endregion

        #region Methods

        public void Explode()
        {
            if (MapInstance == null || Owner == null)
            {
                return;
            }

            NpcMonsterSkill ski = Skills.FirstOrDefault();

            if (ski?.Skill != null)
            {
                foreach (MapMonster monsterInRange in MapInstance.GetListMonsterInRange(MapX, MapY, ski.Skill.Range)
                    .Where(m => m != null))
                {
                    int hitMode = 0;
                    bool onyxWings = false;

                    int damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(Owner, null),
                        new BattleEntity(monsterInRange), ski.Skill, ref hitMode, ref onyxWings);

                    if (monsterInRange.CurrentHp <= damage)
                    {
                        monsterInRange.SetDeathStatement();
                    }
                    else
                    {
                        monsterInRange.CurrentHp -= damage;
                    }

                    MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 3, monsterInRange.MapMonsterId,
                        ski.SkillVNum, ski.Skill.Cooldown, ski.Skill.AttackAnimation, ski.Skill.Effect, 0, 0, monsterInRange.CurrentHp > 0,
                        monsterInRange.CurrentHp / monsterInRange.MaxHp * 100, damage, hitMode, ski.Skill.Type));
                }

                foreach (Character characterInRange in MapInstance.GetCharactersInRange(MapX, MapY, ski.Skill.Range)
                    .Where(c => c != null && c.IsEnemy(Owner)))
                {
                    int hitMode = 0;
                    bool onyxWings = false;

                    int damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(Owner, null),
                        new BattleEntity(characterInRange, null), ski.Skill, ref hitMode, ref onyxWings);

                    characterInRange.GetDamage(damage);

                    if (characterInRange.Hp <= 0)
                    {
                        Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(o =>
                            ServerManager.Instance.AskRevive(characterInRange.CharacterId));
                    }

                    MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 1, characterInRange.CharacterId,
                        ski.SkillVNum, ski.Skill.Cooldown, ski.Skill.AttackAnimation, ski.Skill.Effect, 0, 0, characterInRange.Hp > 0,
                        (int)(characterInRange.Hp / characterInRange.HPLoad() * 100), damage, hitMode, ski.Skill.Type));
                }
            }

            Observable.Timer(TimeSpan.FromSeconds(1))
                .Subscribe(observer =>
                {
                    SetDeathStatement();
                    MapInstance.RemoveMonster(this);
                    MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
                });
        }

        public void DisposeBCard(short bcardId)
        {
            if (BCardDisposables.TryRemove(bcardId, out IDisposable disposable))
            {
                disposable?.Dispose();
            }
        }

        public void DisposeBCards()
        {
            BCardDisposables.ToList().ForEach(s => s.Value?.Dispose());

            BCardDisposables.Clear();
        }

        public void PushBack(short numberOfCells, Character senderCharacter)
        {
            if (IsAlive && MapInstance != null)
            {
                short tempX = MapX;
                short tempY = MapY;

                if (SkillHelper.CalculateNewPosition(MapInstance, senderCharacter.PositionX,
                    senderCharacter.PositionY, numberOfCells, ref tempX, ref tempY))
                {
                    MapX = tempX;
                    MapY = tempY;

                    MapInstance.Broadcast($"guri 3 3 {MapMonsterId} {MapX} {MapY} 3 4 2 -1");
                }
            }
        }

        public void Focus(short numberOfCells, Character senderCharacter) => PushBack((short)(numberOfCells * -1), senderCharacter);

        public bool HasBuff(CardType type, byte subType) => Buff?.GetAllItems()?.Where(s => s?.Card?.BCards != null)?.SelectMany(s => s.Card.BCards)?.Any(s => s.Type == (byte)type && s.SubType == (subType / 10)) == true;

        public bool HasBuff(int cardId) => Buff?.Any(s => s?.Card?.CardId == cardId) == true;

        public void AddBuff(Buff indicator)
        {
            if (indicator?.Card != null)
            {
                Buff[indicator.Card.CardId] = indicator;
                indicator.RemainingTime = indicator.Card.Duration;
                indicator.Start = DateTime.Now;

                indicator.Card.BCards.ForEach(c => c.ApplyBCards(this, indicator.Sender));
                Observable.Timer(TimeSpan.FromMilliseconds(indicator.Card.Duration * 100)).Subscribe(o =>
                {
                    RemoveBuff(indicator.Card.CardId);
                    if (indicator.Card.TimeoutBuff != 0 &&
                        ServerManager.RandomNumber() < indicator.Card.TimeoutBuffChance)
                    {
                        AddBuff(new Buff(indicator.Card.TimeoutBuff, Monster.Level, indicator.Sender));
                    }
                });
                _noAttack |= indicator.Card.BCards.Any(s =>
                    s.Type == (byte)CardType.SpecialAttack &&
                    s.SubType.Equals((byte)AdditionalTypes.SpecialAttack.NoAttack / 10));
                _noMove |= indicator.Card.BCards.Any(s =>
                    s.Type == (byte)CardType.Move &&
                    s.SubType.Equals((byte)AdditionalTypes.Move.MovementImpossible / 10));
            }
        }

        public string GenerateRc(int monsterHealth) => $"rc 3 {MapMonsterId} {monsterHealth} 0";

        public string GenerateDm(int monsterDamage) => $"dm 3 {MapMonsterId} {monsterDamage} 0";

        public string GenerateBfE(Card card, bool turnOff = false) => $"bf_e 3 {MapMonsterId} {card.CardId} {(turnOff ? 0 : card.Duration)}";

        public string GenerateBoss() => $"rboss 3 {MapMonsterId} {CurrentHp} {MaxHp}";

        public string GenerateIn()
        {
            if (IsAlive && !IsDisabled)
            {
                return StaticPacketHelper.In(UserType.Monster, MonsterVNum, MapMonsterId, MapX, MapY, Position,
                    (int)(CurrentHp / (float)MaxHp * 100), (int)(CurrentMp / (float)MaxMp * 100), 0,
                    NoAggresiveIcon ? InRespawnType.NoEffect : InRespawnType.TeleportationEffect, false);
            }

            return string.Empty;
        }

        public void Initialize(MapInstance currentMapInstance)
        {
            MapInstance = currentMapInstance;
            Initialize();
        }

        public void Initialize()
        {
            FirstX = MapX;
            FirstY = MapY;
            LastSkill = LastMove = LastEffect = DateTime.Now;
            Target = -1;
            Path = new List<Node>();
            IsAlive = true;
            ShouldRespawn = ShouldRespawn ?? true;
            Monster = ServerManager.GetNpc(MonsterVNum);

            MaxHp = Monster.MaxHP;
            MaxMp = Monster.MaxMP;
            if (MapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
            {
                if (IsBoss)
                {
                    MaxHp *= 7;
                    MaxMp *= 7;
                }
                else
                {
                    MaxHp *= 5;
                    MaxMp *= 5;

                    if (IsTarget)
                    {
                        MaxHp *= 6;
                        MaxMp *= 6;
                    }
                }
            }

            // Irrelevant for now(Act4)
            //if (MapInstance?.MapInstanceType == MapInstanceType.Act4Morcos || MapInstance?.MapInstanceType == MapInstanceType.Act4Hatus || MapInstance?.MapInstanceType == MapInstanceType.Act4Calvina || MapInstance?.MapInstanceType == MapInstanceType.Act4Berios)
            //{
            //    if (MonsterVNum == 563 || MonsterVNum == 577 || MonsterVNum == 629 || MonsterVNum == 624)
            //    {
            //        MaxHp *= 5;
            //        MaxMp *= 5;
            //    }
            //}

            NoAggresiveIcon = Monster.NoAggresiveIcon;

            IsHostile = Monster.IsHostile;
            CurrentHp = MaxHp;
            CurrentMp = MaxMp;
            Skills = Monster.Skills.ToList();
            DamageList = new Dictionary<long, long>();
            _random = new Random(MapMonsterId);
            _movetime = ServerManager.RandomNumber(400, 3200);
        }

        /// <summary>
        /// Check if the Monster is in the given Range.
        /// </summary>
        /// <param name="mapX">The X coordinate on the Map of the object to check.</param>
        /// <param name="mapY">The Y coordinate on the Map of the object to check.</param>
        /// <param name="distance">The maximum distance of the object to check.</param>
        /// <returns>True if the Monster is in range, False if not.</returns>
        public bool IsInRange(short mapX, short mapY, byte distance)
        {
            return Map.GetDistance(new MapCell
            {
                X = mapX,
                Y = mapY
            }, new MapCell
            {
                X = MapX,
                Y = MapY
            }) <= distance + 1;
        }

        public void RunDeathEvent()
        {
            Buff.ClearAll();
            DisposeBCards();
            _noMove = false;
            _noAttack = false;
            if (IsBonus)
            {
                MapInstance.InstanceBag.Combo++;
                MapInstance.InstanceBag.Point += EventHelper.CalculateComboPoint(MapInstance.InstanceBag.Combo + 1);
            }
            else
            {
                MapInstance.InstanceBag.Combo = 0;
                MapInstance.InstanceBag.Point += EventHelper.CalculateComboPoint(MapInstance.InstanceBag.Combo);
            }

            MapInstance.InstanceBag.MonstersKilled++;
            OnDeathEvents.ForEach(e => EventHelper.Instance.RunEvent(e, monster: this));
        }

        public void SetDeathStatement()
        {
            IsAlive = false;
            LastMove = DateTime.Now;
            CurrentHp = 0;
            CurrentMp = 0;
            Death = DateTime.Now;
        }

        public void DecreaseMp(int damage)
        {
            CurrentMp -= damage;

            if (CurrentMp < 0)
            {
                CurrentMp = 0;
            }
        }

        public void StartLife()
        {
            try
            {
                if (!MapInstance.IsSleeping)
                {
                    MonsterLife();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal void GetNearestOponent()
        {
            if (Target == -1
                && MapInstance != null)
            {
                IEnumerable<Character> characters = DamageList.Keys.Select(characterId => MapInstance.GetCharacterById(characterId));

                if (IsHostile && !characters.Any())
                {
                    characters = MapInstance.Sessions.Select(s => s?.Character);
                }

                characters = characters.Where(c => c?.MapInstance == MapInstance && !c.IsInvisible && (Owner == null || c.IsEnemy(Owner))).ToList();

                Character character = characters.Where(c => c.Hp > 0
                        && (ServerManager.Instance.ChannelId != 51 || (MonsterVNum - (byte)c.Faction != 678 && MonsterVNum - (byte)c.Faction != 971))
                        && Map.GetDistance(MapX, MapY, c.PositionX, c.PositionY) < Monster.NoticeRange)
                    .OrderBy(c => Map.GetDistance(MapX, MapY, c.PositionX, c.PositionY))
                    .FirstOrDefault();

                Mate mate = characters.SelectMany(c => c.Mates)
                    .Where(m => m?.Owner != null
                        && m.IsTeamMember
                        && m.IsAlive
                        && (ServerManager.Instance.ChannelId != 51 || (MonsterVNum - (byte)m.Faction != 678 && MonsterVNum - (byte)m.Faction != 971))
                        && Map.GetDistance(MapX, MapY, m.PositionX, m.PositionY) < Monster.NoticeRange)
                    .OrderBy(m => Map.GetDistance(MapX, MapY, m.PositionX, m.PositionY))
                    .FirstOrDefault();

                MapMonster mapMonster = MapInstance.Monsters
                    .Where(m => m.IsAlive
                        && m.Owner == null
                        && Owner != null
                        && m.MapMonsterId != MapMonsterId
                        && (ServerManager.Instance.ChannelId != 51 || (MonsterVNum - (byte)m.Faction != 678 && MonsterVNum - (byte)m.Faction != 971))
                        && Map.GetDistance(MapX, MapY, m.MapX, m.MapY) < Monster.NoticeRange)
                    .OrderBy(m => Map.GetDistance(MapX, MapY, m.MapX, m.MapY))
                    .FirstOrDefault();

                int nearest = int.MaxValue;

                if (character != null)
                {
                    int distance = Map.GetDistance(MapX, MapY, character.PositionX, character.PositionY);

                    if (distance < nearest)
                    {
                        TargetType = UserType.Player;
                        Target = character.CharacterId;
                        nearest = distance;
                    }
                }

                if (mate != null)
                {
                    int distance = Map.GetDistance(MapX, MapY, mate.MapX, mate.MapY);

                    if (distance < nearest)
                    {
                        TargetType = UserType.Npc;
                        Target = mate.MateTransportId;
                        nearest = distance;
                    }
                }

                if (mapMonster != null)
                {
                    int distance = Map.GetDistance(MapX, MapY, mapMonster.MapX, mapMonster.MapY);

                    if (distance < nearest)
                    {
                        TargetType = UserType.Monster;
                        Target = mapMonster.MapMonsterId;
                        nearest = distance;
                    }
                }
            }
        }

        internal void HostilityTarget()
        {
            if (IsHostile && Target == -1)
            {
                GetNearestOponent();

                if (Target != -1)
                {
                    if (!NoAggresiveIcon && MoveEvent == null)
                    {
                        if (TargetType == UserType.Player)
                        {
                            Character character = MapInstance.GetCharacterById(Target);

                            if(character != null)
                                character.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 5000));
                        } else if (TargetType == UserType.Npc)
                        {
                            Mate mate = MapInstance.Sessions.SelectMany(s => s.Character.Mates)
                                .FirstOrDefault(m => m.Owner != null && m.IsTeamMember && m.MateTransportId == Target);

                            if (mate != null && mate.Owner != null)
                                mate.Owner.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 5000));
                        }
                        else
                            MapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 5000));
                    }

                    if (OnNoticeEvents.Any())
                    {
                        OnNoticeEvents.ForEach(e => EventHelper.Instance.RunEvent(e, monster: this));
                        OnNoticeEvents.RemoveAll(e => e != null);
                    }
                }
            }
        }

        /// <summary>
        /// Remove the current Target from Monster.
        /// </summary>
        internal void RemoveTarget()
        {
            if (Target != -1)
            {
                (Path ?? (Path = new List<Node>())).Clear();
                Target = -1;

                //return to origin
                Path = BestFirstSearch.FindPathJagged(new Node { X = MapX, Y = MapY }, new Node { X = FirstX, Y = FirstY },
                    MapInstance.Map.JaggedGrid);
            }
        }

        private void RunAway(short mapX, short mapY)
        {
            if (Monster == null || !IsAlive || !IsMoving || Monster.Speed < 1)
            {
                return;
            }

            double time = (DateTime.Now - LastMove).TotalMilliseconds;

            int timeToWalk = 2000 / Monster.Speed;

            if (time > timeToWalk)
            {
                short tempX = MapX;
                short tempY = MapY;

                short cells = (short)ServerManager.RandomNumber(1, 5);

                if (SkillHelper.CalculateNewPosition(MapInstance, mapX, mapY, cells, ref tempX, ref tempY))
                {
                    MapX = tempX;
                    MapY = tempY;

                    Observable.Timer(TimeSpan.FromMilliseconds(timeToWalk))
                        .Subscribe(x =>
                        {
                            MapX = tempX;
                            MapY = tempY;

                            MoveEvent?.Events.ForEach(e => EventHelper.Instance.RunEvent(e, monster: this));
                        });

                    MapInstance.Broadcast(StaticPacketHelper.Move(UserType.Monster, MapMonsterId, tempX, tempY, Monster.Speed));
                    MapInstance.Broadcast(StaticPacketHelper.Say(3, MapMonsterId, 0, "!!!!"));
                }
            }
        }

        public void UpdateBushFire()
        {
            BrushFireJagged = BestFirstSearch.LoadBrushFireJagged(new GridPos
            {
                X = MapX,
                Y = MapY
            }, MapInstance.Map.JaggedGrid);
        }

        /// <summary>
        /// Follow the Monsters target to it's position.
        /// </summary>
        /// <param name="targetSession">The TargetSession to follow</param>
        private void FollowTarget(ClientSession targetSession)
        {
            try
            {
                if (IsMoving && !_noMove)
                {
                    const short maxDistance = 22;
                    int distance = Map.GetDistance(new MapCell
                    {
                        X = targetSession.Character.PositionX,
                        Y = targetSession.Character.PositionY
                    },
                        new MapCell
                        {
                            X = MapX,
                            Y = MapY
                        });
                    if (targetSession.Character.LastMonsterAggro.AddSeconds(5) < DateTime.Now ||
                        targetSession.Character.BrushFireJagged == null)
                    {
                        targetSession.Character.UpdateBushFire();
                    }

                    targetSession.Character.LastMonsterAggro = DateTime.Now;
                    if (Path.Count == 0)
                    {
                        short xoffset = (short)ServerManager.RandomNumber(-1, 1);
                        short yoffset = (short)ServerManager.RandomNumber(-1, 1);
                        try
                        {
                            Path = BestFirstSearch.TracePathJagged(new Node { X = MapX, Y = MapY },
                                targetSession.Character.BrushFireJagged,
                                targetSession.Character.MapInstance.Map.JaggedGrid);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(
                                $"Pathfinding using Pathfinder failed. Map: {MapId} StartX: {MapX} StartY: {MapY} TargetX: {(short)(targetSession.Character.PositionX + xoffset)} TargetY: {(short)(targetSession.Character.PositionY + yoffset)}",
                                ex);
                            RemoveTarget();
                        }
                    }

                    if (Monster != null && DateTime.Now > LastMove && Monster.Speed > 0 && Path.Count > 0)
                    {
                        int maxindex = Path.Count > Monster.Speed / 2 ? Monster.Speed / 2 : Path.Count;
                        short mapX = Path[maxindex - 1].X;
                        short mapY = Path[maxindex - 1].Y;
                        double waitingtime = WaitingTime(mapX, mapY);
                        MapInstance.Broadcast(new BroadcastPacket(null,
                            PacketFactory.Serialize(StaticPacketHelper.Move(UserType.Monster, MapMonsterId, mapX, mapY,
                                Monster.Speed)), ReceiverType.All, xCoordinate: mapX, yCoordinate: mapY));

                        Observable.Timer(TimeSpan.FromMilliseconds((int)((waitingtime > 1 ? 1 : waitingtime) * 1000)))
                            .Subscribe(x =>
                            {
                                MapX = mapX;
                                MapY = mapY;
                            });
                        distance = (int)Path[0].F;
                        Path.RemoveRange(0, maxindex > Path.Count ? Path.Count : maxindex);
                    }

                    if (MapId != targetSession.Character.MapInstance.Map.MapId || distance > (maxDistance) + 3)
                    {
                        if (_waitCount == 10)
                        {
                            RemoveTarget();
                            _waitCount = 0;
                        }

                        _waitCount++;
                    }
                    else
                    {
                        _waitCount = 0;
                    }
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Log.Warn($"There is a Problem with the Account: {targetSession.Account.Name} Warn: {ex.Message}");
            }
        }

        private void FollowTarget(Mate mate)
        {
            if (IsMoving && !_noMove)
            {
                const short maxDistance = 22;
                int distance = Map.GetDistance(new MapCell
                {
                    X = mate.PositionX,
                    Y = mate.PositionY
                },
                    new MapCell
                    {
                        X = MapX,
                        Y = MapY
                    });

                if (mate.LastMonsterAggro.AddSeconds(5) < DateTime.Now || mate.BrushFireJagged == null)
                {
                    mate.UpdateBushFire();
                }

                mate.LastMonsterAggro = DateTime.Now;
                if (Path.Count == 0)
                {
                    short xoffset = (short)ServerManager.RandomNumber(-1, 1);
                    short yoffset = (short)ServerManager.RandomNumber(-1, 1);
                    try
                    {
                        Path = BestFirstSearch.TracePathJagged(new Node { X = MapX, Y = MapY }, mate.BrushFireJagged,
                            mate.Owner.MapInstance.Map.JaggedGrid);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"Pathfinding using Pathfinder failed. Map: {MapId} StartX: {MapX} StartY: {MapY} TargetX: {(short)(mate.PositionX + xoffset)} TargetY: {(short)(mate.PositionY + yoffset)}",
                            ex);
                        RemoveTarget();
                    }
                }

                if (Monster != null && DateTime.Now > LastMove && Monster.Speed > 0 && Path.Count > 0)
                {
                    int maxindex = Path.Count > Monster.Speed / 2 ? Monster.Speed / 2 : Path.Count;
                    short mapX = Path[maxindex - 1].X;
                    short mapY = Path[maxindex - 1].Y;
                    double waitingtime = WaitingTime(mapX, mapY);
                    MapInstance.Broadcast(new BroadcastPacket(null,
                        PacketFactory.Serialize(StaticPacketHelper.Move(UserType.Monster, MapMonsterId, mapX, mapY,
                            Monster.Speed)), ReceiverType.All, xCoordinate: mapX, yCoordinate: mapY));

                    Observable.Timer(TimeSpan.FromMilliseconds((int)((waitingtime > 1 ? 1 : waitingtime) * 1000)))
                        .Subscribe(x =>
                        {
                            MapX = mapX;
                            MapY = mapY;
                        });
                    distance = (int)Path[0].F;
                    Path.RemoveRange(0, maxindex > Path.Count ? Path.Count : maxindex);
                }

                if (MapId != mate.Owner.MapInstance.Map.MapId || distance > (maxDistance) + 3)
                {
                    if (_waitCount == 10)
                    {
                        RemoveTarget();
                        _waitCount = 0;
                    }

                    _waitCount++;
                }
                else
                {
                    _waitCount = 0;
                }
            }

        }

        private void FollowTarget(MapMonster mapMonster)
        {
            if (!IsMoving || _noMove)
            {
                return;
            }

            int distance = Map.GetDistance(MapX, MapY, mapMonster.MapX, mapMonster.MapY);

            if (mapMonster.BrushFireJagged == null
                || mapMonster.LastMonsterAggro.AddSeconds(5) < DateTime.Now)
            {
                mapMonster.UpdateBushFire();
            }

            mapMonster.LastMonsterAggro = DateTime.Now;

            if (Path.Count == 0)
            {
                short offsetX = (short)ServerManager.RandomNumber(-1, 1);
                short offsetY = (short)ServerManager.RandomNumber(-1, 1);

                try
                {
                    Path = BestFirstSearch.TracePathJagged(new Node { X = MapX, Y = MapY }, mapMonster.BrushFireJagged, mapMonster.MapInstance.Map.JaggedGrid);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Pathfinding using Pathfinder failed. Map: {MapId} StartX: {MapX} StartY: {MapY} TargetX: {(short)(mapMonster.MapX + offsetX)} TargetY: {(short)(mapMonster.MapY + offsetY)}", ex);
                    RemoveTarget();
                }
            }

            if (Monster != null
                && Monster.Speed > 0
                && Path.Count > 0
                && DateTime.Now > LastMove)
            {
                int maxIndex = Path.Count > Monster.Speed / 2 ? Monster.Speed / 2 : Path.Count;

                short mapX = Path[maxIndex - 1].X;
                short mapY = Path[maxIndex - 1].Y;

                double waitingTime = WaitingTime(mapX, mapY);

                MapInstance.Broadcast(new BroadcastPacket(null, PacketFactory.Serialize(StaticPacketHelper.Move(UserType.Monster, MapMonsterId,
                    mapX, mapY, Monster.Speed)), ReceiverType.All, xCoordinate: mapX, yCoordinate: mapY));

                Observable.Timer(TimeSpan.FromMilliseconds((int)((waitingTime > 1 ? 1 : waitingTime) * 1000)))
                    .Subscribe(x =>
                    {
                        MapX = mapX;
                        MapY = mapY;
                    });

                distance = (int)Path[0].F;

                Path.RemoveRange(0, maxIndex > Path.Count ? Path.Count : maxIndex);
            }

            const short maxDistance = 22;

            if (MapId != mapMonster.MapInstance.Map.MapId
                || distance > (maxDistance) + 3)
            {
                if (_waitCount == 10)
                {
                    RemoveTarget();
                    _waitCount = 0;
                }

                _waitCount++;
            }
            else
            {
                _waitCount = 0;
            }
        }

        private void TeleportPlayers(List<ClientSession> sessions)
        {
            Parallel.ForEach(sessions, s =>
            {
                if (s.Character.Faction == FactionType.Angel)
                {
                    ServerManager.Instance.ChangeMap(s.Character.CharacterId, 130, 39, 42);
                }
                else
                {
                    ServerManager.Instance.ChangeMap(s.Character.CharacterId, 131, 39, 42);
                }
            });
        }

        /// <summary>
        /// Handle any kind of Monster interaction
        /// </summary>
        private void MonsterLife()
        {
            if (Monster == null)
            {
                return;
            }

            if (IsAlive
                && AliveTime > 0
                && (DateTime.Now - LastSelfDamage).TotalSeconds >= 1)
            {
                LastSelfDamage = DateTime.Now;

                GetDamage((int)(MaxHp / 100D * (100D / AliveTime)));

                if (CurrentHp <= 0)
                {
                    SetDeathStatement();
                    MapInstance?.RemoveMonster(this);
                    MapInstance?.Broadcast(StaticPacketHelper.Out(UserType.Monster, MapMonsterId));
                    return;
                }
            }

            if ((DateTime.Now - LastEffect).TotalSeconds >= 5)
            {
                LastEffect = DateTime.Now;

                if (IsTarget)
                {
                    MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 824));
                }

                if (IsBonus)
                {
                    MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId, 826));
                }
            }

            if (IsBoss && IsAlive)
            {
                MapInstance.Broadcast(GenerateBoss());
            }

            if (Buff.Any(s => s.Card.CardId == 160))
            {
                if (DateTime.Now >= LastHPRemove.AddSeconds(2))
                {
                    CurrentHp -= Monster.Level * 5;
                    MapInstance.Broadcast($"dm 3 {MapMonsterId} {Monster.Level * 5}");
                }
            }

            // handle hit queue
            while (HitQueue.TryDequeue(out HitRequest hitRequest))
            {
                if (Owner != null)
                {
                    continue;
                }

                if (IsAlive
                    && hitRequest.Session.Character.Hp > 0
                    && (hitRequest.Mate == null || hitRequest.Mate.Hp > 0)
                    && (ServerManager.Instance.ChannelId != 51
                    || (MonsterVNum - (byte)hitRequest.Session.Character.Faction != 678
                    && MonsterVNum - (byte)hitRequest.Session.Character.Faction != 971)))
                {

                    // Apply Equipment BCards
                    {
                        Character attacker = hitRequest?.Session?.Character;

                        if (attacker != null)
                        {
                            attacker.GetMainWeaponBCards(CardType.Buff)?.ToList()
                                .ForEach(s => s.ApplyBCards(s.BuffCard?.BuffType == BuffType.Bad ? this : (object)attacker, attacker));

                            attacker.GetSecondaryWeaponBCards(CardType.Buff)?.ToList()
                                .ForEach(s => s.ApplyBCards(s.BuffCard?.BuffType == BuffType.Bad ? this : (object)attacker, attacker));
                        }
                    }

                    int hitmode = 0;
                    bool isCaptureSkill = hitRequest.Skill?.BCards.Any(s => s.Type.Equals((byte)CardType.Capture)) ?? false;

                    // calculate damage
                    bool onyxWings = false;
                    BattleEntity battleEntity = hitRequest.Mate == null
                        ? new BattleEntity(hitRequest.Session.Character, hitRequest.Skill)
                        : new BattleEntity(hitRequest.Mate);
                    int damage = DamageHelper.Instance.CalculateDamage(battleEntity, new BattleEntity(this),
                        hitRequest.Skill, ref hitmode, ref onyxWings);


                    // Charge
                    {
                        if (hitmode != 1)
                        {
                            hitRequest.Session.Character.ApplyCharge(ref damage);
                        }
                    }

                    // Invisible
                    {
                        if (damage > 0 && hitRequest.Session.Character.Invisible)
                        {
                            hitRequest.Session.Character.SetInvisible(false);
                        }
                    }

                    // HPDecreasedByConsumingMP
                    {
                        Character attacker = hitRequest.Session.Character;

                        if (attacker.HasBuff(CardType.HealingBurningAndCasting, (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP))
                        {
                            attacker.GetDamage(hitRequest.Skill.MpCost);

                            if (attacker.Hp < 1)
                            {
                                attacker.Hp = 1;
                            }

                            attacker.MapInstance?.Broadcast(attacker.GenerateDm(hitRequest.Skill.MpCost));
                            hitRequest.Session.SendPacket(attacker.GenerateStat());
                        }
                    }

                    if (Monster.BCards.Find(s =>
                        s.Type == (byte)CardType.LightAndShadow &&
                        s.SubType == (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP) is BCard card)
                    {
                        int reduce = damage / 100 * card.FirstData;
                        if (CurrentMp < reduce)
                        {
                            CurrentMp = 0;
                        }
                        else
                        {
                            CurrentMp -= reduce;
                        }
                    }

                    if (damage >= CurrentHp &&
                        Monster.BCards.Any(s => s.Type == 39 && s.SubType == 0 && s.ThirdData == -1))
                    {
                        damage = CurrentHp - 1;
                    }
                    else if (onyxWings)
                    {
                        short onyxX = (short)(hitRequest.Session.Character.PositionX + 2);
                        short onyxY = (short)(hitRequest.Session.Character.PositionY + 2);
                        int onyxId = MapInstance.GetNextMonsterId();
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
                        MapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(31, 1,
                            hitRequest.Session.Character.CharacterId, onyxX, onyxY));
                        onyx.Initialize(MapInstance);
                        MapInstance.AddMonster(onyx);
                        MapInstance.Broadcast(onyx.GenerateIn());
                        CurrentHp -= damage / 2;
                        var request = hitRequest;
                        var damage1 = damage;
                        Observable.Timer(TimeSpan.FromMilliseconds(350)).Subscribe(o =>
                        {
                            MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, onyxId, 3,
                                MapMonsterId, -1, 0, -1, request.Skill?.Effect ?? 0, -1, -1, IsAlive, CurrentHp / (MaxHp * 100), damage1 / 2, 0,
                                0));
                            MapInstance.RemoveMonster(onyx);
                            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, onyx.MapMonsterId));
                        });
                    }

                    if (hitmode != 1)
                    {
                        hitRequest.Skill?.BCards?.ForEach(s =>
                        {
                            switch ((CardType)s.Type)
                            {
                                case CardType.Buff:
                                    {
                                        object attacker = hitRequest.Session.Character;
                                        object defender = this;

                                        s.ApplyBCards(s?.BuffCard?.BuffType != BuffType.Good
                                            ? defender : attacker, attacker);
                                    }
                                    break;
                                case CardType.SpecialActions:
                                case CardType.JumpBackPush:
                                case CardType.DrainAndSteal:
                                case CardType.Capture:
                                    {
                                        s.ApplyBCards(this, hitRequest.Session.Character);
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
                                            if (ServerManager.RandomNumber() < shell.Value)
                                            {
                                                AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.DeadlyBlackout:
                                        {
                                            Buff buff = new Buff(66, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value)
                                            {
                                                AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.MinorBleeding:
                                        {
                                            Buff buff = new Buff(1, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value)
                                            {
                                                AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.Bleeding:
                                        {
                                            Buff buff = new Buff(21, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value)
                                            {
                                                AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.HeavyBleeding:
                                        {
                                            Buff buff = new Buff(42, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value)
                                            {
                                                AddBuff(buff);
                                            }

                                            break;
                                        }
                                    case (byte)ShellWeaponEffectType.Freeze:
                                        {
                                            Buff buff = new Buff(27, battleEntity.Level);
                                            if (ServerManager.RandomNumber() < shell.Value)
                                            {
                                                AddBuff(buff);
                                            }

                                            break;
                                        }
                                }
                            }
                        }
                    }

                    if (DamageList.ContainsKey(hitRequest.Session.Character.CharacterId))
                    {
                        DamageList[hitRequest.Session.Character.CharacterId] += damage;
                    }
                    else
                    {
                        DamageList.Add(hitRequest.Session.Character.CharacterId, damage);
                    }

                    if (IsBoss && MapInstance == CaligorRaid.CaligorMapInstance)
                    {
                        switch (hitRequest.Session.Character.Faction)
                        {
                            case FactionType.Angel:
                                CaligorRaid.AngelDamage += damage;
                                if (onyxWings)
                                {
                                    CaligorRaid.AngelDamage += damage / 2;
                                }

                                break;

                            case FactionType.Demon:
                                CaligorRaid.DemonDamage += damage;
                                if (onyxWings)
                                {
                                    CaligorRaid.DemonDamage += damage / 2;
                                }

                                break;
                        }
                    }

                    if (isCaptureSkill)
                    {
                        damage = 0;
                    }

                    if (CurrentHp <= damage)
                    {
                        killedbyFaction = (byte)hitRequest.Session.Character.Faction;
                        SetDeathStatement();
                    }
                    else
                    {
                        CurrentHp -= damage;
                    }

                    // Reflection.EnemyMPDecreasedChance
                    {
                        BCard bcard = hitRequest.Skill?.BCards?.FirstOrDefault(s => s.Type == (byte)CardType.Reflection
                            && s.SubType == (byte)AdditionalTypes.Reflection.EnemyMPDecreasedChance / 10);

                        if (bcard != null)
                        {
                            if (ServerManager.RandomNumber() < (bcard.FirstData * -1))
                            {
                                CurrentMp -= (int)((CurrentMp / 100D) * bcard.SecondData);

                                if (CurrentMp < 1)
                                {
                                    CurrentMp = 1;
                                }
                            }
                        }
                    }

                    // Reflection.HPIncreased
                    // Reflection.MPIncreased
                    {
                        Character attacker = hitRequest.Session.Character;

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

                    // only set the hit delay if we become the monsters target with this hit
                    if (Target == -1)
                    {
                        LastSkill = DateTime.Now;
                    }

                    int nearestDistance = 100;
                    foreach (KeyValuePair<long, long> kvp in DamageList)
                    {
                        ClientSession session = MapInstance.GetSessionByCharacterId(kvp.Key);
                        if (session != null)
                        {
                            int distance = Map.GetDistance(new MapCell
                            {
                                X = MapX,
                                Y = MapY
                            }, new MapCell
                            {
                                X = session.Character.PositionX,
                                Y = session.Character.PositionY
                            });
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                Target = session.Character.CharacterId;
                                TargetType = UserType.Player;
                            }

                            foreach (Mate mate in session.Character.Mates)
                            {
                                int mateDistance = Map.GetDistance(new MapCell
                                {
                                    X = MapX,
                                    Y = MapY
                                }, new MapCell
                                {
                                    X = mate.PositionX,
                                    Y = mate.PositionY
                                });
                                if (mateDistance < nearestDistance)
                                {
                                    nearestDistance = mateDistance;
                                    Target = mate.MateTransportId;
                                    TargetType = UserType.Npc;
                                }
                            }
                        }
                    }

                    if (hitRequest.Mate == null)
                    {
                        switch (hitRequest.TargetHitType)
                        {
                            case TargetHitType.SingleTargetHit:
                                if (!isCaptureSkill)
                                {
                                    MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                        hitRequest.Session.Character.CharacterId, 3, MapMonsterId,
                                        hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character),
                                        hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                        hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY,
                                        IsAlive, (int)(CurrentHp / (float)MaxHp * 100), damage, hitmode,
                                        (byte)(hitRequest.Skill.SkillType - 1)));
                                }

                                break;

                            case TargetHitType.SingleTargetHitCombo:
                                MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                    hitRequest.Session.Character.CharacterId, 3, MapMonsterId,
                                    hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character),
                                    hitRequest.SkillCombo.Animation, hitRequest.SkillCombo.Effect,
                                    hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY,
                                    IsAlive, (int)(CurrentHp / (float)MaxHp * 100), damage, hitmode,
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
                                    MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                        hitRequest.Session.Character.CharacterId, 3, MapMonsterId,
                                        hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character),
                                        hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                        hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY,
                                        IsAlive, (int)(CurrentHp / (float)MaxHp * 100), damage, hitmode,
                                        (byte)(hitRequest.Skill.SkillType - 1)));
                                }
                                else
                                {
                                    MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                        hitRequest.Session.Character.CharacterId, 3, MapMonsterId, 0, 0, 0, 0, 0, 0,
                                        IsAlive, (int)(CurrentHp / (float)MaxHp * 100), damage, hitmode,
                                        (byte)(hitRequest.Skill.SkillType - 1)));
                                }

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

                                MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                    hitRequest.Session.Character.CharacterId, 3, MapMonsterId,
                                    hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY,
                                    IsAlive, (int)(CurrentHp / (float)MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.ZoneHit:
                                MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                    hitRequest.Session.Character.CharacterId, 3, MapMonsterId,
                                    hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect, hitRequest.MapX,
                                    hitRequest.MapY, IsAlive, (int)(CurrentHp / (float)MaxHp * 100), damage, 5,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;

                            case TargetHitType.SpecialZoneHit:
                                MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                    hitRequest.Session.Character.CharacterId, 3, MapMonsterId,
                                    hitRequest.Skill.SkillVNum, hitRequest.Skill.GetCooldown(hitRequest.Session.Character),
                                    hitRequest.Skill.AttackAnimation, hitRequest.SkillEffect,
                                    hitRequest.Session.Character.PositionX, hitRequest.Session.Character.PositionY,
                                    IsAlive, (int)(CurrentHp / (float)MaxHp * 100), damage, hitmode,
                                    (byte)(hitRequest.Skill.SkillType - 1)));
                                break;
                        }
                    }
                    else
                    {
                        MapInstance?.Broadcast(StaticPacketHelper.SkillUsed(UserType.Npc,
                            hitRequest.Mate.MateTransportId, 3, MapMonsterId, 0, 12, 11, 200, 0, 0, IsAlive,
                            CurrentHp / (MaxHp * 100), damage, hitmode, 0));
                    }

                    if (CurrentHp <= 0 && !isCaptureSkill)
                    {
                        // generate the kill bonus
                        hitRequest.Session.Character.GenerateKillBonus(this);
                    }
                }
                else
                {
                    // monster already has been killed, send cancel
                    hitRequest.Session.SendPacket(StaticPacketHelper.Cancel(2, MapMonsterId));
                }

                if (IsBoss)
                {
                    MapInstance.Broadcast(GenerateBoss());
                }
            }

            if (!IsAlive && ShouldRespawn != null && !ShouldRespawn.Value)
            {

                if (MapId == 153 && MonsterVNum == 2305 && IsBoss)
                {

                    try
                    {
                        List<DMGList> clients = new List<DMGList>();
                        List<ClientSession> Players = new List<ClientSession>();
                        foreach (KeyValuePair<long, long> id in DamageList)
                        {
                            clients.Add(new DMGList { ClientSession = ServerManager.Instance.GetSessionByCharacterId(id.Key), Damage = id.Value });
                            Players.Add(ServerManager.Instance.GetSessionByCharacterId(id.Key));
                        }
                        long DMGAngel = 0;
                        long DMGDemon = 0;
                        foreach (DMGList list in clients)
                        {
                            if (list.ClientSession == null)
                                continue;
                            if (list.ClientSession.Character.Faction == FactionType.Angel)
                            {
                                DMGAngel += list.Damage;
                            }
                            else
                                DMGDemon += list.Damage;
                        }
                        FactionType WinnerFaction = (DMGAngel > DMGDemon ? FactionType.Angel : FactionType.Demon);


                        foreach (ClientSession session in Players)
                        {
                            //Change it Yourself like you said
                            //Reward Winner
                            if (session != null)
                            {
                                sbyte rare = (sbyte)ServerManager.RandomNumber(0, 8);
                                if (session.Character.Faction == WinnerFaction)
                                {
                                    if (session.Character.Inventory.CanAddItem(185))
                                    {
                                        session.Character.Inventory.AddNewToInventory(185, Rare: rare);
                                    }
                                    else
                                    {
                                        session.Character.SendGift(session.Character.CharacterId, 185,
                                        1, rare, 0, false,0);
                                    }
                                }
                                //Reward Looser
                                else
                                {
                                    if (session.Character.Inventory.CanAddItem(942))
                                    {
                                        session.Character.Inventory.AddNewToInventory(942, Rare: rare);
                                    }
                                    else
                                    {
                                        session.Character.SendGift(session.Character.CharacterId, 942,
                                        1, rare, 0, false,0);
                                    }
                                }
                            }

                        }
                        MapInstance.Broadcast($"msg 0 The {WinnerFaction.ToString()} have done most damage and thereby won!");
                        MapInstance bitoren = ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(134));
                        bitoren.Portals.Remove(bitoren.Portals.FirstOrDefault(s => s.SourceX == 140 && s.SourceY == 100));
                        bitoren.MapClear();
                        switch (Faction)
                        {
                            case 1:
                                ServerManager.Instance.Act4AngelStat.Mode = 0;
                                ServerManager.Instance.Act4AngelStat.IsMorcos = false;
                                ServerManager.Instance.Act4AngelStat.IsHatus = false;
                                ServerManager.Instance.Act4AngelStat.IsCalvina = false;
                                ServerManager.Instance.Act4AngelStat.IsBerios = false;
                                break;

                            case 2:
                                ServerManager.Instance.Act4DemonStat.Mode = 0;
                                ServerManager.Instance.Act4DemonStat.IsMorcos = false;
                                ServerManager.Instance.Act4DemonStat.IsHatus = false;
                                ServerManager.Instance.Act4DemonStat.IsCalvina = false;
                                ServerManager.Instance.Act4DemonStat.IsBerios = false;
                                break;
                        }
                        Observable.Timer(TimeSpan.FromSeconds(20)).Subscribe(x => TeleportPlayers(MapInstance.Sessions.ToList()));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error($"A4 Error: {ex.Message}");
                        MapInstance bitoren = ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(134));
                        bitoren.Portals.Remove(bitoren.Portals.FirstOrDefault(s => s.SourceX == 140 && s.SourceY == 100));
                        bitoren.MapClear();
                        switch (Faction)
                        {
                            case 1:
                                ServerManager.Instance.Act4AngelStat.Mode = 0;
                                ServerManager.Instance.Act4AngelStat.IsMorcos = false;
                                ServerManager.Instance.Act4AngelStat.IsHatus = false;
                                ServerManager.Instance.Act4AngelStat.IsCalvina = false;
                                ServerManager.Instance.Act4AngelStat.IsBerios = false;
                                break;

                            case 2:
                                ServerManager.Instance.Act4DemonStat.Mode = 0;
                                ServerManager.Instance.Act4DemonStat.IsMorcos = false;
                                ServerManager.Instance.Act4DemonStat.IsHatus = false;
                                ServerManager.Instance.Act4DemonStat.IsCalvina = false;
                                ServerManager.Instance.Act4DemonStat.IsBerios = false;
                                break;
                        }
                        ServerManager.Instance.StartedEvents.Remove(EventType.Act4Raid);
                        TeleportPlayers(MapInstance.Sessions.ToList());
                    }
                }
                MapInstance.RemoveMonster(this);
            }

            if (!IsAlive && ShouldRespawn != null && ShouldRespawn.Value)
            {
                double timeDeath = (DateTime.Now - Death).TotalSeconds;
                if (timeDeath >= Monster.RespawnTime / 10d)
                {
                    Respawn();
                }
            }

            // normal movement
            else if (Target == -1)
            {
                Move();
            }

            // Follow target
            else if (MapInstance != null)
            {
                GetNearestOponent();
                HostilityTarget();

                NpcMonsterSkill npcMonsterSkill = null;

                switch (TargetType)
                {
                    case UserType.Player:
                        {
                            Character character = MapInstance.GetCharacterById(Target);

                            if (character == null
                                || character.IsInvisible
                                || character.Hp < 1
                                || !IsAlive)
                            {
                                RemoveTarget();
                                return;
                            }

                            if (HasBuff(CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.RunAway))
                            {
                                RunAway(character.PositionX, character.PositionY);
                                break;
                            }

                            if (Skills != null && ServerManager.RandomNumber(0, 10) > 8)
                            {
                                npcMonsterSkill = Skills
                                    .Where(s => (DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 100 * s.Skill.Cooldown)
                                    .OrderBy(rnd => _random.Next()).FirstOrDefault();
                            }

                            if (npcMonsterSkill?.Skill != null
                                && npcMonsterSkill.Skill.TargetType == 1
                                && npcMonsterSkill.Skill.HitType == 0)
                            {
                                TargetHit(character.Session, npcMonsterSkill);
                            }

                            if (npcMonsterSkill?.Skill != null
                                && CurrentMp >= npcMonsterSkill.Skill.MpCost
                                && Map.GetDistance(MapX, MapY, character.PositionX, character.PositionY) < npcMonsterSkill.Skill.Range)
                            {
                                TargetHit(character.Session, npcMonsterSkill);
                            }
                            else
                            {
                                if (Map.GetDistance(MapX, MapY, character.PositionX, character.PositionY) <= Monster.BasicRange)
                                {
                                    TargetHit(character.Session, null);
                                }
                                else
                                {
                                    FollowTarget(character.Session);
                                }
                            }
                        }
                        break;

                    case UserType.Npc:
                        {
                            Mate mate = MapInstance.Sessions.SelectMany(s => s.Character.Mates)
                                .FirstOrDefault(m => m.Owner != null && m.IsTeamMember && m.MateTransportId == Target);

                            if (mate == null
                                || mate.Owner.IsInvisible
                                || !mate.IsAlive
                                || !IsAlive)
                            {
                                RemoveTarget();
                                return;
                            }

                            if (HasBuff(CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.RunAway))
                            {
                                RunAway(mate.MapX, mate.MapY);
                                break;
                            }

                            if (Skills != null && ServerManager.RandomNumber(0, 10) > 8)
                            {
                                npcMonsterSkill = Skills
                                    .Where(s => (DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 100 * s.Skill.Cooldown)
                                    .OrderBy(rnd => _random.Next()).FirstOrDefault();
                            }

                            if (npcMonsterSkill?.Skill != null
                                && npcMonsterSkill.Skill.TargetType == 1
                                && npcMonsterSkill.Skill.HitType == 0)
                            {
                                TargetHit(mate, npcMonsterSkill);
                            }

                            if (npcMonsterSkill?.Skill != null
                                && CurrentMp >= npcMonsterSkill.Skill.MpCost
                                && Map.GetDistance(MapX, MapY, mate.PositionX, mate.PositionY) < npcMonsterSkill.Skill.Range)
                            {
                                TargetHit(mate, npcMonsterSkill);
                            }
                            else
                            {
                                if (Map.GetDistance(MapX, MapY, mate.PositionX, mate.PositionY) <= Monster.BasicRange)
                                {
                                    TargetHit(mate, null);
                                }
                                else
                                {
                                    FollowTarget(mate);
                                }
                            }
                        }
                        break;

                    case UserType.Monster:
                        {
                            MapMonster mapMonster = MapInstance.Monsters.FirstOrDefault(m => m.MapMonsterId == Target);

                            if (Owner == null
                                || mapMonster == null
                                || !mapMonster.IsAlive
                                || !IsAlive)
                            {
                                RemoveTarget();
                                return;
                            }

                            if (HasBuff(CardType.SpecialActions, (byte)AdditionalTypes.SpecialActions.RunAway))
                            {
                                RunAway(mapMonster.MapX, mapMonster.MapY);
                                break;
                            }

                            if (Skills != null && ServerManager.RandomNumber(0, 10) > 8)
                            {
                                npcMonsterSkill = Skills
                                    .Where(s => (DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 100 * s.Skill.Cooldown)
                                    .OrderBy(rnd => _random.Next()).FirstOrDefault();
                            }

                            if (npcMonsterSkill?.Skill != null
                                && npcMonsterSkill.Skill.TargetType == 1
                                && npcMonsterSkill.Skill.HitType == 0)
                            {
                                TargetHit(mapMonster, npcMonsterSkill);
                            }

                            if (npcMonsterSkill?.Skill != null
                                && CurrentMp >= npcMonsterSkill.Skill.MpCost
                                && Map.GetDistance(MapX, MapY, mapMonster.MapX, mapMonster.MapY) < npcMonsterSkill.Skill.Range)
                            {
                                TargetHit(mapMonster, npcMonsterSkill);
                            }
                            else
                            {
                                if (Map.GetDistance(MapX, MapY, mapMonster.MapX, mapMonster.MapY) <= Monster.BasicRange)
                                {
                                    TargetHit(mapMonster, null);
                                }
                                else
                                {
                                    FollowTarget(mapMonster);
                                }
                            }
                        }
                        break;
                }
            }
        }

        private double WaitingTime(short mapX, short mapY)
        {
            double waitingtime = Map.GetDistance(new MapCell
            {
                X = mapX,
                Y = mapY
            },
                                     new MapCell
                                     {
                                         X = MapX,
                                         Y = MapY
                                     }) / (double)Monster.Speed;
            LastMove = DateTime.Now.AddSeconds(waitingtime > 1 ? 1 : waitingtime);
            return waitingtime;
        }

        private void Move()
        {
            // Normal Move Mode
            if (Monster == null || !IsAlive || _noMove)
            {
                return;
            }

            if (IsMoving && Monster.Speed > 0)
            {
                double time = (DateTime.Now - LastMove).TotalMilliseconds;
                if (Path == null)
                {
                    Path = new List<Node>();
                }

                if (Path.Count > 0) // move back to initial position after following target
                {
                    int timetowalk = 2000 / Monster.Speed;
                    if (time > timetowalk)
                    {
                        int maxindex = Path.Count > Monster.Speed / 2 ? Monster.Speed / 2 : Path.Count;
                        if (Path[maxindex - 1] == null)
                        {
                            return;
                        }

                        short mapX = Path[maxindex - 1].X;
                        short mapY = Path[maxindex - 1].Y;
                        WaitingTime(mapX, mapY);

                        Observable.Timer(TimeSpan.FromMilliseconds(timetowalk)).Subscribe(x =>
                        {
                            MapX = mapX;
                            MapY = mapY;
                            MoveEvent?.Events.ForEach(e => EventHelper.Instance.RunEvent(e, monster: this));
                        });
                        Path.RemoveRange(0, maxindex > Path.Count ? Path.Count : maxindex);
                        MapInstance.Broadcast(new BroadcastPacket(null,
                            PacketFactory.Serialize(StaticPacketHelper.Move(UserType.Monster, MapMonsterId, MapX, MapY,
                                Monster.Speed)), ReceiverType.All, xCoordinate: mapX, yCoordinate: mapY));
                        return;
                    }
                }
                else if (time > _movetime)
                {
                    short mapX = FirstX, mapY = FirstY;
                    if (MapInstance.Map?.GetFreePosition(ref mapX, ref mapY, (byte)ServerManager.RandomNumber(0, 2),
                            (byte)_random.Next(0, 2)) ?? false)
                    {
                        int distance = Map.GetDistance(new MapCell
                        {
                            X = mapX,
                            Y = mapY
                        }, new MapCell
                        {
                            X = MapX,
                            Y = MapY
                        });

                        double value = 1000d * distance / (2 * Monster.Speed);
                        Observable.Timer(TimeSpan.FromMilliseconds(value)).Subscribe(x =>
                        {
                            MapX = mapX;
                            MapY = mapY;
                        });

                        LastMove = DateTime.Now.AddMilliseconds(value);
                        MapInstance.Broadcast(new BroadcastPacket(null,
                            PacketFactory.Serialize(StaticPacketHelper.Move(UserType.Monster, MapMonsterId, MapX, MapY,
                                Monster.Speed)), ReceiverType.All));
                    }
                }
            }

            HostilityTarget();
        }

        public void RemoveBuff(short id)
        {
            Buff indicator = Buff[id];

            if (indicator != null)
            {
                Buff.Remove(id);
                _noAttack &= !indicator.Card.BCards.Any(s =>
                    s.Type == (byte)CardType.SpecialAttack &&
                    s.SubType.Equals((byte)AdditionalTypes.SpecialAttack.NoAttack / 10));
                _noMove &= !indicator.Card.BCards.Any(s =>
                    s.Type == (byte)CardType.Move &&
                    s.SubType.Equals((byte)AdditionalTypes.Move.MovementImpossible / 10));

                indicator.Card.BCards.ForEach(s => DisposeBCard(s.BCardId));
            }
        }

        private void Respawn()
        {
            if (Monster != null)
            {
                DamageList = new Dictionary<long, long>();
                IsAlive = true;
                Target = -1;
                CurrentHp = MaxHp;
                CurrentMp = MaxMp;
                MapX = FirstX;
                MapY = FirstY;
                Path = new List<Node>();
                MapInstance.Broadcast(GenerateIn());
            }
        }

        public void GetDamage(int damage)
        {
            CurrentHp -= damage;

            if (CurrentHp < 0)
            {
                CurrentHp = 0;
            }
        }

        /// <summary>
        /// Hit the Target Character.
        /// </summary>
        /// <param name="targetSession"></param>
        /// <param name="npcMonsterSkill"></param>
        private void TargetHit(ClientSession targetSession, NpcMonsterSkill npcMonsterSkill)
        {
            if (Monster != null && targetSession?.Character != null &&
                ((DateTime.Now - LastSkill).TotalMilliseconds >= 1000 + (Monster.BasicCooldown * 200) ||
                 npcMonsterSkill != null) && !_noAttack)
            {
                if (npcMonsterSkill != null)
                {
                    if (CurrentMp < npcMonsterSkill.Skill.MpCost)
                    {
                        FollowTarget(targetSession);
                        return;
                    }

                    npcMonsterSkill.LastSkillUse = DateTime.Now;
                    CurrentMp -= npcMonsterSkill.Skill.MpCost;
                    MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Monster, MapMonsterId, 1, Target,
                        npcMonsterSkill.Skill.CastAnimation, npcMonsterSkill.Skill.CastEffect,
                        npcMonsterSkill.Skill.SkillVNum));
                }

                LastMove = DateTime.Now;

                // Apply Equipment BCard
                {
                    Character defender = targetSession?.Character;

                    if (defender != null)
                    {
                        defender.GetArmorBCards(CardType.Buff)?.ToList()
                            .ForEach(s => s.ApplyBCards(s.BuffCard?.BuffType == BuffType.Bad ? this : (object)defender, defender));
                    }
                }

                int hitmode = 0;
                bool onyxWings = false;
                int damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(this),
                    new BattleEntity(targetSession.Character, null), npcMonsterSkill?.Skill, ref hitmode,
                    ref onyxWings);

                // deal 0 damage to GM with GodMode
                if (targetSession.Character.HasGodMode)
                {
                    damage = 0;
                }

                // Absorb
                {
                    if (targetSession.Character.HasBuff(CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.TransferAttackPower))
                    {
                        targetSession.Character.Absorb(ref damage, ref hitmode);
                    }
                }

                // Invisible
                {
                    if (damage > 0 && targetSession.Character.Invisible)
                    {
                        targetSession.Character.SetInvisible(false);
                    }
                }

                // HPDecreasedByConsumingMP
                {
                    if (npcMonsterSkill?.Skill != null
                        && HasBuff(CardType.HealingBurningAndCasting, (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP))
                    {
                        CurrentHp -= npcMonsterSkill.Skill.MpCost;
                        MapInstance?.Broadcast(GenerateDm(npcMonsterSkill.Skill.MpCost));
                    }
                }

                // InflictDamageToMP
                {
                    int amount = (int)((damage / 100D) * targetSession.Character.GetBuff(CardType.LightAndShadow, (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP)[0]);

                    targetSession.Character.Mp -= amount;
                    damage -= amount;

                    if (targetSession.Character.Mp < 1)
                    {
                        targetSession.Character.Mp = 1;
                    }
                }

                if (targetSession.Character.IsSitting)
                {
                    targetSession.Character.IsSitting = false;
                    MapInstance.Broadcast(targetSession.Character.GenerateRest());
                }

                int castTime = 0;
                if (npcMonsterSkill != null && npcMonsterSkill.Skill.CastEffect != 0)
                {
                    MapInstance.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId,
                            npcMonsterSkill.Skill.CastEffect), MapX, MapY);
                    castTime = npcMonsterSkill.Skill.CastTime * 100;
                }

                Observable.Timer(TimeSpan.FromMilliseconds(castTime)).Subscribe(o =>
                {
                    if (targetSession.Character != null && targetSession.Character.Hp > 0)
                    {
                        TargetHit2(targetSession, npcMonsterSkill, damage, hitmode);
                    }
                });
            }
        }

        private void TargetHit(Mate mate, NpcMonsterSkill npcMonsterSkill)
        {
            if (Monster != null && mate != null &&
                ((DateTime.Now - LastSkill).TotalMilliseconds >= 1000 + (Monster.BasicCooldown * 200) ||
                 npcMonsterSkill != null) && !_noAttack)
            {
                if (npcMonsterSkill != null)
                {
                    if (CurrentMp < npcMonsterSkill.Skill.MpCost)
                    {
                        FollowTarget(mate);
                        return;
                    }

                    npcMonsterSkill.LastSkillUse = DateTime.Now;
                    CurrentMp -= npcMonsterSkill.Skill.MpCost;
                    MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Monster, MapMonsterId, 2, Target,
                        npcMonsterSkill.Skill.CastAnimation, npcMonsterSkill.Skill.CastEffect,
                        npcMonsterSkill.Skill.SkillVNum));
                }

                LastMove = DateTime.Now;

                int hitmode = 0;
                bool onyxWings = false;
                int damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(this), new BattleEntity(mate),
                    npcMonsterSkill?.Skill, ref hitmode, ref onyxWings);

                // deal 0 damage to GM with GodMode
                if (mate.Owner.HasGodMode)
                {
                    damage = 0;
                }

                int[] manaShield = mate.GetBuff(CardType.LightAndShadow,
                    (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP);
                if (manaShield[0] != 0 && hitmode != 1)
                {
                    int reduce = damage / 100 * manaShield[0];
                    if (mate.Mp < reduce)
                    {
                        mate.Mp = 0;
                    }
                    else
                    {
                        mate.Mp -= reduce;
                    }
                }

                if (mate.IsSitting)
                {
                    mate.IsSitting = false;
                    MapInstance.Broadcast(mate.GenerateRest());
                }

                int castTime = 0;
                if (npcMonsterSkill != null && npcMonsterSkill.Skill.CastEffect != 0)
                {
                    MapInstance.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId,
                            npcMonsterSkill.Skill.CastEffect), MapX, MapY);
                    castTime = npcMonsterSkill.Skill.CastTime * 100;
                }

                Observable.Timer(TimeSpan.FromMilliseconds(castTime)).Subscribe(o =>
                {
                    if (mate.Hp > 0)
                    {
                        TargetHit2(mate, npcMonsterSkill, damage, hitmode);
                    }
                });
            }
        }

        private void TargetHit(MapMonster mapMonster, NpcMonsterSkill npcMonsterSkill)
        {
            if (Monster != null
                && mapMonster != null
                && mapMonster.IsAlive
                && ((DateTime.Now - LastSkill).TotalMilliseconds >= 1000 + (Monster.BasicCooldown * 200) || npcMonsterSkill != null)
                && !_noAttack)
            {
                if (npcMonsterSkill != null)
                {
                    if (CurrentMp < npcMonsterSkill.Skill.MpCost)
                    {
                        FollowTarget(mapMonster);
                        return;
                    }

                    npcMonsterSkill.LastSkillUse = DateTime.Now;

                    CurrentMp -= npcMonsterSkill.Skill.MpCost;

                    MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Monster, MapMonsterId, 3, Target,
                        npcMonsterSkill.Skill.CastAnimation, npcMonsterSkill.Skill.CastEffect, npcMonsterSkill.Skill.SkillVNum));
                }

                LastMove = DateTime.Now;

                int hitMode = 0;
                bool onyxWings = false;

                int damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(this), new BattleEntity(mapMonster),
                    npcMonsterSkill?.Skill, ref hitMode, ref onyxWings);

                int castTime = 0;
                if (npcMonsterSkill != null && npcMonsterSkill.Skill.CastEffect != 0)
                {
                    castTime = npcMonsterSkill.Skill.CastTime * 100;

                    MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId,
                        npcMonsterSkill.Skill.CastEffect), MapX, MapY);
                }

                Observable.Timer(TimeSpan.FromMilliseconds(castTime)).Subscribe(o => TargetHit2(mapMonster, npcMonsterSkill,
                   damage, hitMode));
            }
        }

        private void TargetHit2(ClientSession targetSession, NpcMonsterSkill npcMonsterSkill, int damage, int hitmode)
        {
            lock (targetSession.Character.PVELockObject)
            {
                if (targetSession.Character.Hp > 0)
                {
                    if (damage >= targetSession.Character.Hp &&
                        Monster.BCards.Any(s => s.Type == 39 && s.SubType == 0 && s.ThirdData == 1))
                    {
                        damage = targetSession.Character.Hp - 1;
                    }

                    targetSession.Character.GetDamage(damage);
                    MapInstance.Broadcast(null, targetSession.Character.GenerateStat(), ReceiverType.OnlySomeone,
                        string.Empty, Target);
                    MapInstance.Broadcast(npcMonsterSkill != null
                        ? StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 1, Target,
                            npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                            npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                            targetSession.Character.Hp > 0,
                            (int)(targetSession.Character.Hp / targetSession.Character.HPLoad() * 100), damage,
                            hitmode, 0)
                        : StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 1, Target, 0,
                            Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, targetSession.Character.Hp > 0,
                            (int)(targetSession.Character.Hp / targetSession.Character.HPLoad() * 100), damage,
                            hitmode, 0));
                    npcMonsterSkill?.Skill.BCards.ForEach(s => s.ApplyBCards(this));
                    LastSkill = DateTime.Now;

                    if (IsKamikaze)
                    {
                        SetDeathStatement();
                        MapInstance.Broadcast(StaticPacketHelper.Die(UserType.Monster, MapMonsterId, UserType.Monster, MapMonsterId));
                    }

                    if (targetSession.Character.Hp <= 0)
                    {
                        RemoveTarget();
                        Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                            ServerManager.Instance.AskRevive((long)targetSession.Character?.CharacterId));
                    }
                }
            }

            if (npcMonsterSkill != null && (npcMonsterSkill.Skill.Range > 0 || npcMonsterSkill.Skill.TargetRange > 0))
            {
                foreach (Character characterInRange in MapInstance
                    .GetCharactersInRange(
                        npcMonsterSkill.Skill.TargetRange == 0 ? MapX : targetSession.Character.PositionX,
                        npcMonsterSkill.Skill.TargetRange == 0 ? MapY : targetSession.Character.PositionY,
                        npcMonsterSkill.Skill.TargetRange).Where(s =>
                        s.CharacterId != Target &&
                        (ServerManager.Instance.ChannelId != 51 ||
                         (MonsterVNum - (byte)s.Faction != 678 && MonsterVNum - (byte)s.Faction != 971)) &&
                        s.Hp > 0 && !s.InvisibleGm))
                {
                    if (characterInRange.IsSitting)
                    {
                        characterInRange.IsSitting = false;
                        MapInstance.Broadcast(characterInRange.GenerateRest());
                    }

                    if (characterInRange.HasGodMode)
                    {
                        damage = 0;
                        hitmode = 1;
                    }

                    if (characterInRange.Hp > 0)
                    {
                        characterInRange.GetDamage(damage);
                        MapInstance.Broadcast(null, characterInRange.GenerateStat(), ReceiverType.OnlySomeone,
                            string.Empty, characterInRange.CharacterId);
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 1,
                            characterInRange.CharacterId, 0, Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0,
                            characterInRange.Hp > 0, (int)(characterInRange.Hp / characterInRange.HPLoad() * 100),
                            damage, hitmode, 0));
                        if (characterInRange.Hp <= 0)
                        {
                            Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                            {
                                ServerManager.Instance.AskRevive((long)characterInRange?.CharacterId);
                            });
                        }
                    }
                }

                foreach (Mate mateInRange in MapInstance.Sessions.SelectMany(x => x.Character.Mates).Where(s =>
                    s.IsTeamMember && s.MateTransportId != Target &&
                    (ServerManager.Instance.ChannelId != 51 ||
                     (MonsterVNum - (byte)s.Owner.Faction != 678 && MonsterVNum - (byte)s.Owner.Faction != 971)) &&
                    s.Hp > 0 && !s.Owner.InvisibleGm && Map.GetDistance(
                        new MapCell()
                        {
                            X = npcMonsterSkill.Skill.TargetRange == 0 ? MapX : targetSession.Character.PositionX,
                            Y = npcMonsterSkill.Skill.TargetRange == 0 ? MapY : targetSession.Character.PositionY
                        }, new MapCell() { X = s.PositionX, Y = s.PositionY }) <= npcMonsterSkill.Skill.TargetRange))
                {
                    if (mateInRange.IsSitting)
                    {
                        mateInRange.IsSitting = false;
                        MapInstance.Broadcast(mateInRange.GenerateRest());
                    }

                    if (mateInRange.Owner.HasGodMode)
                    {
                        damage = 0;
                        hitmode = 1;
                    }

                    if (mateInRange.Hp > 0)
                    {
                        mateInRange.GetDamage(damage);
                        mateInRange.Owner.Session.SendPacket(mateInRange.GenerateStatInfo());
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 2,
                            mateInRange.MateTransportId, 0, Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0,
                            mateInRange.Hp > 0, mateInRange.Hp / (mateInRange.MaxHp * 100), damage, hitmode, 0));
                        if (mateInRange.Hp <= 0)
                        {
                            mateInRange.IsAlive = false;
                            mateInRange.IsTeamMember = false;
                            mateInRange.Owner.Session.CurrentMapInstance.Broadcast(mateInRange.GenerateOut());
                            mateInRange.Owner.Session.SendPacket(mateInRange.Owner.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mateInRange.Name), 11));
                            mateInRange.Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mateInRange.Name), 0));

                        }
                    }
                }
            }
        }

        private void TargetHit2(Mate mate, NpcMonsterSkill npcMonsterSkill, int damage, int hitmode)
        {
            lock (mate.PveLockObject)
            {
                if (mate.Hp > 0)
                {
                    if (damage >= mate.Hp &&
                        Monster.BCards.Any(s => s.Type == 39 && s.SubType == 0 && s.ThirdData == 1))
                    {
                        damage = mate.Hp - 1;
                    }

                    mate.GetDamage(damage);
                    mate.Owner.Session.SendPacket(mate.GenerateStatInfo());
                    MapInstance.Broadcast(npcMonsterSkill != null
                        ? StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 2, Target,
                            npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                            npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                            mate.Hp > 0, mate.Hp / (mate.MaxHp * 100), damage, hitmode, 0)
                        : StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 2, Target, 0,
                            Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, mate.Hp > 0,
                            mate.Hp / (mate.MaxHp * 100), damage, hitmode, 0));
                    npcMonsterSkill?.Skill.BCards.ForEach(s => s.ApplyBCards(this));
                    LastSkill = DateTime.Now;

                    if (IsKamikaze)
                    {
                        SetDeathStatement();
                        MapInstance.Broadcast(StaticPacketHelper.Die(UserType.Monster, MapMonsterId, UserType.Monster, MapMonsterId));
                    }

                    if (mate.Hp <= 0)
                    {
                        RemoveTarget();
                        mate.IsAlive = false;
                        mate.IsTeamMember = false;
                        mate.Owner.Session.CurrentMapInstance.Broadcast(mate.GenerateOut());
                        mate.Owner.Session.SendPacket(mate.Owner.GenerateSay(
                            string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mate.Name), 11));
                        mate.Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mate.Name), 0));
                    }
                }
            }

            if (npcMonsterSkill != null && (npcMonsterSkill.Skill.Range > 0 || npcMonsterSkill.Skill.TargetRange > 0))
            {
                foreach (Character characterInRange in MapInstance
                    .GetCharactersInRange(npcMonsterSkill.Skill.TargetRange == 0 ? MapX : mate.PositionX,
                        npcMonsterSkill.Skill.TargetRange == 0 ? MapY : mate.PositionY,
                        npcMonsterSkill.Skill.TargetRange).Where(s =>
                        s.CharacterId != Target &&
                        (ServerManager.Instance.ChannelId != 51 ||
                         (MonsterVNum - (byte)s.Faction != 678 && MonsterVNum - (byte)s.Faction != 971)) &&
                        s.Hp > 0 && !s.InvisibleGm))
                {
                    if (characterInRange.IsSitting)
                    {
                        characterInRange.IsSitting = false;
                        MapInstance.Broadcast(characterInRange.GenerateRest());
                    }

                    if (characterInRange.HasGodMode)
                    {
                        damage = 0;
                        hitmode = 1;
                    }

                    if (characterInRange.Hp > 0)
                    {
                        characterInRange.GetDamage(damage);
                        MapInstance.Broadcast(null, characterInRange.GenerateStat(), ReceiverType.OnlySomeone,
                            string.Empty, characterInRange.CharacterId);
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 1,
                            characterInRange.CharacterId, 0, Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0,
                            characterInRange.Hp > 0, (int)(characterInRange.Hp / characterInRange.HPLoad() * 100),
                            damage, hitmode, 0));
                        if (characterInRange.Hp <= 0)
                        {
                            Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                                ServerManager.Instance.AskRevive((long)characterInRange?.CharacterId));
                        }
                    }
                }

                foreach (Mate mateInRange in MapInstance.Sessions.SelectMany(x => x.Character.Mates).Where(s =>
                    s.IsTeamMember && s.MateTransportId != Target &&
                    (ServerManager.Instance.ChannelId != 51 ||
                     (MonsterVNum - (byte)s.Owner.Faction != 678 && MonsterVNum - (byte)s.Owner.Faction != 971)) &&
                    s.Hp > 0 && !s.Owner.InvisibleGm && Map.GetDistance(
                        new MapCell()
                        {
                            X = npcMonsterSkill.Skill.TargetRange == 0 ? MapX : mate.PositionX,
                            Y = npcMonsterSkill.Skill.TargetRange == 0 ? MapY : mate.PositionY
                        }, new MapCell() { X = s.PositionX, Y = s.PositionY }) <= npcMonsterSkill.Skill.TargetRange))
                {
                    if (mateInRange.IsSitting)
                    {
                        mateInRange.IsSitting = false;
                        MapInstance.Broadcast(mateInRange.GenerateRest());
                    }

                    if (mateInRange.Owner.HasGodMode)
                    {
                        damage = 0;
                        hitmode = 1;
                    }

                    if (mateInRange.Hp > 0)
                    {
                        mateInRange.GetDamage(damage);
                        mateInRange.Owner.Session.SendPacket(mateInRange.GenerateStatInfo());
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 2,
                            mateInRange.MateTransportId, 0, Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0,
                            mateInRange.Hp > 0, mateInRange.Hp / (mateInRange.MaxHp * 100), damage, hitmode, 0));
                        if (mateInRange.Hp <= 0)
                        {
                            mateInRange.IsTeamMember = false;
                            mateInRange.Owner.Session.CurrentMapInstance.Broadcast(mateInRange.GenerateOut());
                            mateInRange.Owner.Session.SendPacket(mateInRange.Owner.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mateInRange.Name), 11));
                            mateInRange.Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mateInRange.Name), 0));

                        }
                    }
                }
            }
        }

        private void TargetHit2(MapMonster mapMonster, NpcMonsterSkill npcMonsterSkill, int damage, int hitmode)
        {
            lock (mapMonster.PveLockObject)
            {
                if (mapMonster.IsAlive)
                {
                    if (damage >= mapMonster.CurrentHp
                        && Monster.BCards.Any(s => s.Type == 39 && s.SubType == 0 && s.ThirdData == 1))
                    {
                        damage = mapMonster.CurrentHp - 1;
                    }

                    mapMonster.GetDamage(damage);

                    MapInstance.Broadcast(npcMonsterSkill != null
                        ? StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 3, Target,
                            npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                            npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, MapX, MapY,
                            mapMonster.CurrentHp > 0, mapMonster.CurrentHp / (mapMonster.MaxHp * 100),
                            damage, hitmode, 0)
                        : StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 3, Target, 0,
                            Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0, mapMonster.CurrentHp > 0,
                            mapMonster.CurrentHp / (mapMonster.MaxHp * 100), damage, hitmode, 0));

                    npcMonsterSkill?.Skill.BCards.ForEach(s => s.ApplyBCards(this));

                    LastSkill = DateTime.Now;

                    if (IsKamikaze)
                    {
                        SetDeathStatement();
                        MapInstance.Broadcast(StaticPacketHelper.Die(UserType.Monster, MapMonsterId, UserType.Monster, MapMonsterId));
                    }

                    if (mapMonster.CurrentHp <= 0)
                    {
                        mapMonster.SetDeathStatement();
                        RemoveTarget();
                        mapMonster.MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Monster, mapMonster.MapMonsterId));
                    }
                }
            }

            if (npcMonsterSkill != null
                && (npcMonsterSkill.Skill.Range > 0 || npcMonsterSkill.Skill.TargetRange > 0))
            {
                foreach (Character characterInRange in MapInstance
                     .GetCharactersInRange(npcMonsterSkill.Skill.TargetRange == 0 ? MapX : mapMonster.MapX,
                         npcMonsterSkill.Skill.TargetRange == 0 ? MapY : mapMonster.MapY,
                         npcMonsterSkill.Skill.TargetRange).Where(s =>
                         s.CharacterId != Target &&
                         (ServerManager.Instance.ChannelId != 51 ||
                          (MonsterVNum - (byte)s.Faction != 678 && MonsterVNum - (byte)s.Faction != 971)) &&
                         s.Hp > 0 && !s.InvisibleGm))
                {
                    if (characterInRange.IsSitting)
                    {
                        characterInRange.IsSitting = false;
                        MapInstance.Broadcast(characterInRange.GenerateRest());
                    }

                    if (characterInRange.HasGodMode)
                    {
                        damage = 0;
                        hitmode = 1;
                    }

                    if (characterInRange.Hp > 0)
                    {
                        characterInRange.GetDamage(damage);
                        MapInstance.Broadcast(null, characterInRange.GenerateStat(), ReceiverType.OnlySomeone,
                            string.Empty, characterInRange.CharacterId);
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 1,
                            characterInRange.CharacterId, 0, Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0,
                            characterInRange.Hp > 0, (int)(characterInRange.Hp / characterInRange.HPLoad() * 100),
                            damage, hitmode, 0));
                        if (characterInRange.Hp <= 0)
                        {
                            Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(o =>
                                ServerManager.Instance.AskRevive((long)characterInRange?.CharacterId));
                        }
                    }
                }

                foreach (Mate mateInRange in MapInstance.Sessions.SelectMany(x => x.Character.Mates).Where(s =>
                    s.IsTeamMember && s.MateTransportId != Target &&
                    (ServerManager.Instance.ChannelId != 51 ||
                     (MonsterVNum - (byte)s.Owner.Faction != 678 && MonsterVNum - (byte)s.Owner.Faction != 971)) &&
                    s.Hp > 0 && !s.Owner.InvisibleGm && Map.GetDistance(
                        new MapCell()
                        {
                            X = npcMonsterSkill.Skill.TargetRange == 0 ? MapX : mapMonster.MapX,
                            Y = npcMonsterSkill.Skill.TargetRange == 0 ? MapY : mapMonster.MapY
                        }, new MapCell() { X = s.PositionX, Y = s.PositionY }) <= npcMonsterSkill.Skill.TargetRange))
                {
                    if (mateInRange.IsSitting)
                    {
                        mateInRange.IsSitting = false;
                        MapInstance.Broadcast(mateInRange.GenerateRest());
                    }

                    if (mateInRange.Owner.HasGodMode)
                    {
                        damage = 0;
                        hitmode = 1;
                    }

                    if (mateInRange.Hp > 0)
                    {
                        mateInRange.GetDamage(damage);
                        mateInRange.Owner.Session.SendPacket(mateInRange.GenerateStatInfo());
                        MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, MapMonsterId, 2,
                            mateInRange.MateTransportId, 0, Monster.BasicCooldown, 11, Monster.BasicSkill, 0, 0,
                            mateInRange.Hp > 0, mateInRange.Hp / (mateInRange.MaxHp * 100), damage, hitmode, 0));
                        if (mateInRange.Hp <= 0)
                        {
                            mateInRange.IsTeamMember = false;
                            mateInRange.Owner.Session.CurrentMapInstance.Broadcast(mateInRange.GenerateOut());
                            mateInRange.Owner.Session.SendPacket(mateInRange.Owner.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mateInRange.Name), 11));
                            mateInRange.Owner.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("PET_DIED"), mateInRange.Name), 0));

                        }
                    }
                }
            }
        }

        #endregion
    }
}