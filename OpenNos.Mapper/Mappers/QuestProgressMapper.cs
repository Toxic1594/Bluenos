using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class QuestProgressMapper
    {
        #region Methods

        public static bool ToQuestProgress(QuestProgressDTO input, QuestProgress output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.CharacterId = input.CharacterId;
            output.IsFinished = input.IsFinished;
            output.QuestData = input.QuestData;
            output.QuestId = input.QuestId;
            output.QuestProgressId = input.QuestProgressId;
            return true;
        }

        public static bool ToQuestProgressDTO(QuestProgress input, QuestProgressDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.CharacterId = input.CharacterId;
            output.IsFinished = input.IsFinished;
            output.QuestData = input.QuestData;
            output.QuestId = input.QuestId;
            output.QuestProgressId = input.QuestProgressId;
            return true;
        }

        #endregion
    }
}