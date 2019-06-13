using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class QuestMapper
    {
        #region Methods

        public static bool ToQuest(QuestDTO input, Quest output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.QuestData = input.QuestData;
            output.QuestId = input.QuestId;
            return true;
        }

        public static bool ToQuestDTO(Quest input, QuestDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.QuestData = input.QuestData;
            output.QuestId = input.QuestId;
            return true;
        }

        #endregion
    }
}