using System.Data.Entity.Migrations;

namespace OpenNos.DAL.EF.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<OpenNosContext>
    {
        #region Instantiation

        public Configuration() => AutomaticMigrationsEnabled = true;

        #endregion
    }
}