using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class MapMonsterMapper
    {
        #region Methods

        public static bool ToMapMonster(MapMonsterDTO input, MapMonster output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.IsDisabled = input.IsDisabled;
            output.IsMoving = input.IsMoving;
            output.MapId = input.MapId;
            output.MapMonsterId = input.MapMonsterId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MonsterVNum = input.MonsterVNum;
            output.Position = input.Position;
            return true;
        }

        public static bool ToMapMonsterDTO(MapMonster input, MapMonsterDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.IsDisabled = input.IsDisabled;
            output.IsMoving = input.IsMoving;
            output.MapId = input.MapId;
            output.MapMonsterId = input.MapMonsterId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MonsterVNum = input.MonsterVNum;
            output.Position = input.Position;
            return true;
        }

        #endregion
    }
}