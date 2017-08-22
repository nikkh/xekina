namespace Xekina.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Requests",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        ProjectName = c.String(),
                        ProjectDescription = c.String(),
                        SubscriptionId = c.String(),
                        ResourceGroupLocation = c.String(),
                        DateRequested = c.DateTimeOffset(nullable: false, precision: 7),
                        RequestedBy = c.String(),
                        Status = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Requests");
        }
    }
}
