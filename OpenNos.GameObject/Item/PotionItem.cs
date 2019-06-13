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

using OpenNos.Data;
using System;
using System.Linq;
using OpenNos.GameObject.Networking;

namespace OpenNos.GameObject
{
    public class PotionItem : Item
    {
        #region Instantiation

        public PotionItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods



        public override void Use(ClientSession session, ref ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            if (!session.HasCurrentMapInstance)
            {
                return;
            }
            if ((DateTime.Now - session.Character.LastPotion).TotalMilliseconds < (session.CurrentMapInstance.Map.MapTypes.OrderByDescending(s => s.PotionDelay).FirstOrDefault()?.PotionDelay ?? 750))
            {
                return;
            }
            session.Character.LastPotion = DateTime.Now;
            switch (Effect)
            {
                default:
                    if (session.CurrentMapInstance.IsPVP)
                        return;
                    int hpLoad = (int)session.Character.HPLoad();
                    int mpLoad = (int)session.Character.MPLoad();
                    if ((session.Character.Hp == hpLoad && session.Character.Mp == mpLoad) || session.Character.Hp <= 0)
                    {
                        return;
                    }
                    if (session.Character.MapId == 134 || session.Character.MapId == 153 || session.Character.MapId == 132 || session.Character.MapId == 151 || session.Character.MapId == 133 || session.Character.MapId == 152)
                    {
                        if (inv.ItemVNum == 1244 || inv.ItemVNum == 1243 || inv.ItemVNum == 1242 )
                        {
                            return;
                        }
                    }
                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                    if (hpLoad - session.Character.Hp < Hp)
                    {
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateRc(hpLoad - session.Character.Hp));
                    }
                    else if (hpLoad - session.Character.Hp > Hp)
                    {
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateRc(Hp));
                    }
                    session.Character.Mp += Mp;
                    session.Character.Hp += Hp;
                    if (session.Character.Mp > mpLoad)
                    {
                        session.Character.Mp = mpLoad;
                    }
                    if (session.Character.Hp > hpLoad)
                    {
                        session.Character.Hp = hpLoad;
                    }
                    if (ServerManager.Instance.ChannelId != 51 || session.Character.MapId == 130 || session.Character.MapId == 131)
                    {
                        if (inv.ItemVNum == 1242 || inv.ItemVNum == 5582)
                        {
                            if (session.CurrentMapInstance.IsPVP)
                                return;
                            session.CurrentMapInstance?.Broadcast(session.Character.GenerateRc(hpLoad - session.Character.Hp));
                            session.Character.Hp = hpLoad;
                        }
                        else if (inv.ItemVNum == 1243 || inv.ItemVNum == 5583)
                        {
                            if (session.CurrentMapInstance.IsPVP)
                                return;
                            session.Character.Mp = mpLoad;
                        }
                        else if (inv.ItemVNum == 1244 || inv.ItemVNum == 5584)
                        {
                            if (session.CurrentMapInstance.IsPVP)
                                return;

                                 session.CurrentMapInstance?.Broadcast(session.Character.GenerateRc(hpLoad - session.Character.Hp));
                            session.Character.Hp = hpLoad;
                            session.Character.Mp = mpLoad;
                        }
                    }
                    session.SendPacket(session.Character.GenerateStat());

                    foreach (Mate mate in session.Character.Mates.Where(s => s.IsTeamMember))
                    {
                        hpLoad = mate.MaxHp;
                        mpLoad = mate.MaxMp;
                        if ((mate.Hp == hpLoad && mate.Mp == mpLoad) || mate.Hp <= 0)
                        {
                            return;
                        }

                        if (hpLoad - mate.Hp < Hp)
                        {
                            session.CurrentMapInstance?.Broadcast(mate.GenerateRc(hpLoad - mate.Hp));
                        }
                        else if (hpLoad - mate.Hp > Hp)
                        {
                            session.CurrentMapInstance?.Broadcast(mate.GenerateRc(Hp));
                        }

                        mate.Mp += Mp;
                        mate.Hp += Hp;
                        if (mate.Mp > mpLoad)
                        {
                            mate.Mp = mpLoad;
                        }

                        if (mate.Hp > hpLoad)
                        {
                            mate.Hp = hpLoad;
                        }

                        if (ServerManager.Instance.ChannelId != 51 || session.Character.MapId == 130 ||
                            session.Character.MapId == 131)
                        {
                            if (inv.ItemVNum == 1242 || inv.ItemVNum == 5582)
                            {
                                if (session.CurrentMapInstance.IsPVP)
                                    return;
                                session.CurrentMapInstance?.Broadcast(
                                    mate.GenerateRc(hpLoad - mate.Hp));
                                mate.Hp = hpLoad;
                            }
                            else if (inv.ItemVNum == 1243 || inv.ItemVNum == 5583)
                            {
                                if (session.CurrentMapInstance.IsPVP)
                                    return;
                                mate.Mp = mpLoad;
                            }
                            else if (inv.ItemVNum == 1244 || inv.ItemVNum == 5584)
                            {
                                if (session.CurrentMapInstance.IsPVP)
                                    return;
                                session.CurrentMapInstance?.Broadcast(
                                    mate.GenerateRc(hpLoad - mate.Hp));
                                mate.Hp = hpLoad;
                                mate.Mp = mpLoad;
                            }
                        }

                        session.SendPacket(mate.GenerateStatInfo());
                    }

                    break;
            }
        }

        #endregion
    }
}