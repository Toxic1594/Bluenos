namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _ItemAmount2 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.NpcMonster", "AmountRequired", c => c.Int(nullable: false));
            AlterColumn("dbo.Recipe", "Amount", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Recipe", "Amount", c => c.Byte(nullable: false));
            AlterColumn("dbo.NpcMonster", "AmountRequired", c => c.Byte(nullable: false));
        }
    }
}
