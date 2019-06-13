namespace OpenNos.DAL.EF.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Aphrodite77 : DbMigration
    {
        public override void Up() => AlterColumn("dbo.Family", "MaxSize", c => c.Short(nullable: false));

        public override void Down() => AlterColumn("dbo.Family", "MaxSize", c => c.Byte(nullable: false));
    }
}
