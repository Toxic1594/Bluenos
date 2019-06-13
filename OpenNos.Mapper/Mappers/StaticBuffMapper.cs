using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class StaticBuffMapper
    {
        #region Methods

        public static bool ToStaticBuff(StaticBuffDTO input, StaticBuff output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.CardId = input.CardId;
            output.CharacterId = input.CharacterId;
            output.RemainingTime = input.RemainingTime;
            output.StaticBuffId = input.StaticBuffId;
            return true;
        }

        public static bool ToStaticBuffDTO(StaticBuff input, StaticBuffDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.CardId = input.CardId;
            output.CharacterId = input.CharacterId;
            output.RemainingTime = input.RemainingTime;
            output.StaticBuffId = input.StaticBuffId;
            return true;
        }

        #endregion
    }
}