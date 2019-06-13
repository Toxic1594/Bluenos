using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class ShellEffectMapper
    {
        #region Methods

        public static bool ToShellEffect(ShellEffectDTO input, ShellEffect output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Effect = input.Effect;
            output.EffectLevel = input.EffectLevel;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.ShellEffectId = input.ShellEffectId;
            output.Value = input.Value;
            return true;
        }

        public static bool ToShellEffectDTO(ShellEffect input, ShellEffectDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Effect = input.Effect;
            output.EffectLevel = input.EffectLevel;
            output.EquipmentSerialId = input.EquipmentSerialId;
            output.ShellEffectId = input.ShellEffectId;
            output.Value = input.Value;
            return true;
        }

        #endregion
    }
}