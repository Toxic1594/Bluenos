using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class RecipeItemMapper
    {
        #region Methods

        public static bool ToRecipeItem(RecipeItemDTO input, RecipeItem output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Amount = input.Amount;
            output.ItemVNum = input.ItemVNum;
            output.RecipeId = input.RecipeId;
            output.RecipeItemId = input.RecipeItemId;
            return true;
        }

        public static bool ToRecipeItemDTO(RecipeItem input, RecipeItemDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.Amount = input.Amount;
            output.ItemVNum = input.ItemVNum;
            output.RecipeId = input.RecipeId;
            output.RecipeItemId = input.RecipeItemId;
            return true;
        }

        #endregion
    }
}