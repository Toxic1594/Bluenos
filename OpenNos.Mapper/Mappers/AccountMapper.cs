using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class AccountMapper
    {
        #region Methods

        public static bool ToAccount(AccountDTO input, Account output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.AccountId = input.AccountId;
            output.Authority = input.Authority;
            output.Email = input.Email;
            output.Name = input.Name;
            output.Password = input.Password;
            output.ReferrerId = input.ReferrerId;
            output.RegistrationIP = input.RegistrationIP;
            output.VerificationToken = input.VerificationToken;
            return true;
        }

        public static bool ToAccountDTO(Account input, AccountDTO output)
        {
            if (input == null)
            {
                output = null;
                return false;
            }
            output.AccountId = input.AccountId;
            output.Authority = input.Authority;
            output.Email = input.Email;
            output.Name = input.Name;
            output.Password = input.Password;
            output.ReferrerId = input.ReferrerId;
            output.RegistrationIP = input.RegistrationIP;
            output.VerificationToken = input.VerificationToken;
            return true;
        }

        #endregion
    }
}