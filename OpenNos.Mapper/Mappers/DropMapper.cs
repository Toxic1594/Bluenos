using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class DropMapper
    {
        #region Methods

        public static bool ToDrop(DropDTO input, Drop output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Amount = input.Amount;
            output.DropChance = input.DropChance;
            output.DropId = input.DropId;
            output.ItemVNum = input.ItemVNum;
            output.MapTypeId = input.MapTypeId;
            output.MonsterVNum = input.MonsterVNum;
            return true;
        }

        public static bool ToDropDTO(Drop input, DropDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Amount = input.Amount;
            output.DropChance = input.DropChance;
            output.DropId = input.DropId;
            output.ItemVNum = input.ItemVNum;
            output.MapTypeId = input.MapTypeId;
            output.MonsterVNum = input.MonsterVNum;
            return true;
        }

        #endregion
    }
}