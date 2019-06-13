﻿using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.GameObject.Event
{
    public class IceBreaker
    {
        public const int MaxAllowedPlayers = 50;

        private static readonly int[] GoldRewards =
        {
            100,
            1000,
            3000,
            5000,
            10000,
            20000
        };

        private static readonly Tuple<int, int>[] LevelBrackets =
        {
            new Tuple<int, int>(1, 25),
            new Tuple<int, int>(20, 40),
            new Tuple<int, int>(35, 55),
            new Tuple<int, int>(50, 70),
            new Tuple<int, int>(65, 85),
            new Tuple<int, int>(80, 99)
        };

        private static int _currentBracket;

        public static List<ClientSession> AlreadyFrozenPlayers { get; set; }

        public static List<ClientSession> FrozenPlayers { get; set; }

        public static MapInstance Map { get; private set; }

        public static void GenerateIceBreaker(bool useTimer = true)
        {
            AlreadyFrozenPlayers = new List<ClientSession>();
            Map = ServerManager.Instance.GenerateMapInstance(2005, MapInstanceType.IceBreakerInstance, new InstanceBag());
            if (useTimer)
            {
                ServerManager.Instance.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(
                    string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_MINUTES"), 5, LevelBrackets[_currentBracket].Item1, LevelBrackets[_currentBracket].Item2), 1));
                Thread.Sleep(5 * 60 * 1000);
                ServerManager.Instance.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(
                    string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_MINUTES"), 1, LevelBrackets[_currentBracket].Item1, LevelBrackets[_currentBracket].Item2), 1));
                Thread.Sleep(1 * 60 * 1000);
                ServerManager.Instance.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(
                    string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_SECONDS"), 30, LevelBrackets[_currentBracket].Item1, LevelBrackets[_currentBracket].Item2), 1));
                Thread.Sleep(30 * 1000);
                ServerManager.Instance.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(
                    string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_SECONDS"), 10, LevelBrackets[_currentBracket].Item1, LevelBrackets[_currentBracket].Item2), 1));
                Thread.Sleep(10 * 1000);
            }
            ServerManager.Instance.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_STARTED"), 1));
            ServerManager.Instance.IceBreakerInWaiting = true;
            ServerManager.Instance.Sessions.Where(x => x.Character.Level >= LevelBrackets[_currentBracket].Item1 && x.Character.Level <= LevelBrackets[_currentBracket].Item2 && x.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance).ToList().ForEach(x => x.SendPacket($"qnaml 2 #guri^501 {string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_ASK"), 500)}"));
            _currentBracket++;
            if (_currentBracket > 5)
            {
                _currentBracket = 0;
            }
            Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(c =>
            {
                ServerManager.Instance.StartedEvents.Remove(EventType.ICEBREAKER);
                ServerManager.Instance.IceBreakerInWaiting = false;
                if (Map.Sessions.Count() <= 1)
                {
                    Map.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_WIN"), 0));
                    Map.Sessions.ToList().ForEach(x =>
                    {
                        x.Character.GetReput(x.Character.Level * 10);
                        if (x.Character.Dignity < 100)
                        {
                            x.Character.Dignity = 100;
                        }
                        x.Character.Gold += GoldRewards[_currentBracket];
                        x.Character.Gold = x.Character.Gold > ServerManager.Instance.MaxGold ? ServerManager.Instance.MaxGold : x.Character.Gold;
                        x.SendPacket(x.Character.GenerateFd());
                        x.SendPacket(x.Character.GenerateGold());
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_MONEY"), GoldRewards[_currentBracket]), 10));
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), x.Character.Level * 10), 10));
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("DIGNITY_RESTORED"), x.Character.Level * 10), 10));
                    });
                    Thread.Sleep(5000);
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(Map, EventActionType.DISPOSEMAP, null));
                }
                else
                {
                    Map.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_WARN"), 0));
                    Thread.Sleep(6000);
                    Map.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_WARN"), 0));
                    Thread.Sleep(7000);
                    Map.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_WARN"), 0));
                    Thread.Sleep(1000);
                    Map.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_FIGHT_START"), 0));
                    Map.IsPVP = true;
                    while (Map.Sessions.Count() > 1 || AlreadyFrozenPlayers.Count() != Map.Sessions.Count())
                    {
                        Thread.Sleep(1000);
                    }
                    Map.Broadcast(UserInterfaceHelper.Instance.GenerateMsg(Language.Instance.GetMessageFromKey("ICEBREAKER_WIN"), 0));
                    Map.Sessions.ToList().ForEach(x =>
                    {
                        x.Character.GetReput(x.Character.Level * 10);
                        if (x.Character.Dignity < 100)
                        {
                            x.Character.Dignity = 100;
                        }
                        x.Character.Gold += GoldRewards[_currentBracket];
                        x.Character.Gold = x.Character.Gold > ServerManager.Instance.MaxGold ? ServerManager.Instance.MaxGold : x.Character.Gold;
                        x.SendPacket(x.Character.GenerateFd());
                        x.SendPacket(x.Character.GenerateGold());
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_MONEY"), GoldRewards[_currentBracket]), 10));
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("WIN_REPUT"), x.Character.Level * 10), 10));
                        x.SendPacket(x.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("DIGNITY_RESTORED"), x.Character.Level * 10), 10));
                    });
                    EventHelper.Instance.ScheduleEvent(TimeSpan.FromSeconds(10), new EventContainer(Map, EventActionType.DISPOSEMAP, null));
                }
            });
        }
    }
}
