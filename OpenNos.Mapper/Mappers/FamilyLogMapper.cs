using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class FamilyLogMapper
    {
        #region Methods

        public static bool ToFamilyLog(FamilyLogDTO input, FamilyLog output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.FamilyId = input.FamilyId;
            output.FamilyLogData = input.FamilyLogData;
            output.FamilyLogId = input.FamilyLogId;
            output.FamilyLogType = input.FamilyLogType;
            output.Timestamp = input.Timestamp;
            return true;
        }

        public static bool ToFamilyLogDTO(FamilyLog input, FamilyLogDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.FamilyId = input.FamilyId;
            output.FamilyLogData = input.FamilyLogData;
            output.FamilyLogId = input.FamilyLogId;
            output.FamilyLogType = input.FamilyLogType;
            output.Timestamp = input.Timestamp;
            return true;
        }

        #endregion
    }
}