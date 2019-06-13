using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class ScriptedInstanceMapper
    {
        #region Methods

        public static bool ToScriptedInstance(ScriptedInstanceDTO input, ScriptedInstance output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.MapId = input.MapId;
            output.PositionX = input.PositionX;
            output.PositionY = input.PositionY;
            output.Script = input.Script;
            output.ScriptedInstanceId = input.ScriptedInstanceId;
            output.Type = input.Type;
            return true;
        }

        public static bool ToScriptedInstanceDTO(ScriptedInstance input, ScriptedInstanceDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.MapId = input.MapId;
            output.PositionX = input.PositionX;
            output.PositionY = input.PositionY;
            output.Script = input.Script;
            output.ScriptedInstanceId = input.ScriptedInstanceId;
            output.Type = input.Type;
            return true;
        }

        #endregion
    }
}