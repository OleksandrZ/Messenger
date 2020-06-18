namespace MessengerServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _1 : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Messages", "IsDelivered");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Messages", "IsDelivered", c => c.Boolean(nullable: false));
        }
    }
}
