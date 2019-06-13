namespace OpenNos.DAL.EF.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Aphrodite75 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MinigameLog",
                c => new
                    {
                        MinigameLogId = c.Long(nullable: false, identity: true),
                        StartTime = c.Long(nullable: false),
                        EndTime = c.Long(nullable: false),
                        Score = c.Int(nullable: false),
                        Minigame = c.Byte(nullable: false),
                        CharacterId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.MinigameLogId)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId);
        }

        public override void Down()
        {
            DropForeignKey("dbo.MinigameLog", "CharacterId", "dbo.Character");
            DropIndex("dbo.MinigameLog", new[] { "CharacterId" });
            DropTable("dbo.MinigameLog");
        }
    }
}
