using System.Data.Entity.Migrations;

namespace OpenNos.DAL.EF.Migrations
{
    public partial class Aphrodite34 : DbMigration
    {
        #region Methods

        public override void Down()
        {
            DropForeignKey("dbo.Nosmate", "CharacterId", "dbo.Character");
            DropForeignKey("dbo.Nosmate", "NpcMonsterVNum", "dbo.NpcMonster");
            DropIndex("dbo.Nosmate", new[] { "NpcMonsterVNum" });
            DropIndex("dbo.Nosmate", new[] { "CharacterId" });
            DropTable("dbo.Nosmate");
        }

        public override void Up()
        {
            CreateTable(
                "dbo.Nosmate",
                c => new
                {
                    NosmateId = c.Long(nullable: false, identity: true),
                    Attack = c.Byte(nullable: false),
                    CanPickUp = c.Boolean(nullable: false),
                    CharacterId = c.Long(nullable: false),
                    NpcMonsterVNum = c.Short(nullable: false),
                    Defence = c.Byte(nullable: false),
                    Experience = c.Long(nullable: false),
                    HasSkin = c.Boolean(nullable: false),
                    IsSummonable = c.Boolean(nullable: false),
                    Level = c.Byte(nullable: false),
                    Loyalty = c.Short(nullable: false),
                    MateType = c.Byte(nullable: false),
                    Name = c.String(maxLength: 255)
                })
                .PrimaryKey(t => t.NosmateId)
                .ForeignKey("dbo.NpcMonster", t => t.NpcMonsterVNum)
                .ForeignKey("dbo.Character", t => t.CharacterId)
                .Index(t => t.CharacterId)
                .Index(t => t.NpcMonsterVNum);
        }

        #endregion
    }
}