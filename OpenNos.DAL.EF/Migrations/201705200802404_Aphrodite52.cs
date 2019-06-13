namespace OpenNos.DAL.EF.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Aphrodite52 : DbMigration
    {
        #region Methods

        public override void Down()
        {
            AddColumn("dbo.BCard", "Delayed", c => c.Boolean(nullable: false));
            DropColumn("dbo.BCard", "Delay");
            DropColumn("dbo.BCard", "IsDelayed");
        }

        public override void Up()
        {
            AddColumn("dbo.BCard", "IsDelayed", c => c.Boolean(nullable: false));
            AddColumn("dbo.BCard", "Delay", c => c.Short(nullable: false));
            DropColumn("dbo.BCard", "Delayed");
        }

        #endregion
    }
}