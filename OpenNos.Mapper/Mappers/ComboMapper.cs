using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class ComboMapper
    {
        #region Methods

        public static bool ToCombo(ComboDTO input, Combo output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Animation = input.Animation;
            output.ComboId = input.ComboId;
            output.Effect = input.Effect;
            output.Hit = input.Hit;
            output.SkillVNum = input.SkillVNum;
            return true;
        }

        public static bool ToComboDTO(Combo input, ComboDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Animation = input.Animation;
            output.ComboId = input.ComboId;
            output.Effect = input.Effect;
            output.Hit = input.Hit;
            output.SkillVNum = input.SkillVNum;
            return true;
        }

        #endregion
    }
}