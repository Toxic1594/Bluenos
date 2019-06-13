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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenNos.DAL.EF
{
    public class Skill
    {
        #region Instantiation

        public Skill()
        {
            CharacterSkill = new HashSet<CharacterSkill>();
            Combo = new HashSet<Combo>();
            NpcMonsterSkill = new HashSet<NpcMonsterSkill>();
            ShopSkill = new HashSet<ShopSkill>();
            BCards = new HashSet<BCard>();
        }

        #endregion

        #region Properties

        public short AttackAnimation { get; set; }

        public short CastAnimation { get; set; }

        public short CastEffect { get; set; }

        public short CastId { get; set; }

        public short CastTime { get; set; }

        public virtual ICollection<CharacterSkill> CharacterSkill { get; set; }

        public byte Class { get; set; }

        public virtual ICollection<Combo> Combo { get; set; }

        public short Cooldown { get; set; }

        public byte CPCost { get; set; }

        public short Duration { get; set; }

        public short Effect { get; set; }

        public byte Element { get; set; }

        public byte HitType { get; set; }

        public short ItemVNum { get; set; }

        public byte Level { get; set; }

        public byte LevelMinimum { get; set; }

        public byte MinimumAdventurerLevel { get; set; }

        public byte MinimumArcherLevel { get; set; }

        public byte MinimumMagicianLevel { get; set; }

        public byte MinimumSwordmanLevel { get; set; }

        public short MpCost { get; set; }

        [MaxLength(255)]
        public string Name { get; set; }

        public virtual ICollection<NpcMonsterSkill> NpcMonsterSkill { get; set; }

        public int Price { get; set; }

        public byte Range { get; set; }
        
        public virtual ICollection<ShopSkill> ShopSkill { get; set; }

        public byte SkillType { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short SkillVNum { get; set; }

        public byte TargetRange { get; set; }

        public byte TargetType { get; set; }

        public byte Type { get; set; }

        public short UpgradeSkill { get; set; }

        public short UpgradeType { get; set; }

        public virtual ICollection<BCard> BCards { get; set; }

        #endregion
    }
}