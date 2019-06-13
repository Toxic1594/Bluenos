using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class RollGeneratedItemMapper
    {
        #region Methods

        public static bool ToRollGeneratedItem(RollGeneratedItemDTO input, RollGeneratedItem output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.IsRareRandom = input.IsRareRandom;
            output.ItemGeneratedAmount = input.ItemGeneratedAmount;
            output.ItemGeneratedVNum = input.ItemGeneratedVNum;
            output.MaximumOriginalItemRare = input.MaximumOriginalItemRare;
            output.MinimumOriginalItemRare = input.MinimumOriginalItemRare;
            output.OriginalItemDesign = input.OriginalItemDesign;
            output.OriginalItemVNum = input.OriginalItemVNum;
            output.Probability = input.Probability;
            output.RollGeneratedItemId = input.RollGeneratedItemId;
            return true;
        }

        public static bool ToRollGeneratedItemDTO(RollGeneratedItem input, RollGeneratedItemDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.IsRareRandom = input.IsRareRandom;
            output.ItemGeneratedAmount = input.ItemGeneratedAmount;
            output.ItemGeneratedVNum = input.ItemGeneratedVNum;
            output.MaximumOriginalItemRare = input.MaximumOriginalItemRare;
            output.MinimumOriginalItemRare = input.MinimumOriginalItemRare;
            output.OriginalItemDesign = input.OriginalItemDesign;
            output.OriginalItemVNum = input.OriginalItemVNum;
            output.Probability = input.Probability;
            output.RollGeneratedItemId = input.RollGeneratedItemId;
            return true;
        }

        #endregion
    }
}