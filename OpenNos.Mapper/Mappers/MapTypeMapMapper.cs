using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class MapTypeMapMapper
    {
        #region Methods

        public static bool ToMapTypeMap(MapTypeMapDTO input, MapTypeMap output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.MapId = input.MapId;
            output.MapTypeId = input.MapTypeId;
            return true;
        }

        public static bool ToMapTypeMapDTO(MapTypeMap input, MapTypeMapDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.MapId = input.MapId;
            output.MapTypeId = input.MapTypeId;
            return true;
        }

        #endregion
    }
}