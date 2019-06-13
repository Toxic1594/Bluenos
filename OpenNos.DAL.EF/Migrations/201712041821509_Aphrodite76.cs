namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class Aphrodite76 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Character", "LastFamilyLeave", c => c.Long(nullable: false));
            AddColumn("dbo.Family", "LastFactionChange", c => c.Long(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Family", "LastFactionChange");
            DropColumn("dbo.Character", "LastFamilyLeave");
        }
    }
}
