using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class TeleporterMapper
    {
        #region Methods

        public static bool ToTeleporter(TeleporterDTO input, Teleporter output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Index = input.Index;
            output.MapId = input.MapId;
            output.MapNpcId = input.MapNpcId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.TeleporterId = input.TeleporterId;
            return true;
        }

        public static bool ToTeleporterDTO(Teleporter input, TeleporterDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Index = input.Index;
            output.MapId = input.MapId;
            output.MapNpcId = input.MapNpcId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.TeleporterId = input.TeleporterId;
            return true;
        }

        #endregion
    }
}