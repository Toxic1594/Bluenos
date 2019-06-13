using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class ShopSkillMapper
    {
        #region Methods

        public static bool ToShopSkill(ShopSkillDTO input, ShopSkill output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.ShopId = input.ShopId;
            output.ShopSkillId = input.ShopSkillId;
            output.SkillVNum = input.SkillVNum;
            output.Slot = input.Slot;
            output.Type = input.Type;
            return true;
        }

        public static bool ToShopSkillDTO(ShopSkill input, ShopSkillDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.ShopId = input.ShopId;
            output.ShopSkillId = input.ShopSkillId;
            output.SkillVNum = input.SkillVNum;
            output.Slot = input.Slot;
            output.Type = input.Type;
            return true;
        }

        #endregion
    }
}