namespace Xekina.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class requestlog1 : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.Requests");
            DropColumn("dbo.Requests", "ID");
            CreateTable(
                "dbo.RequestLogs",
                c => new
                    {
                        RequestLogID = c.Int(nullable: false, identity: true),
                        Data = c.String(),
                        Request_RequestID = c.Int(),
                    })
                .PrimaryKey(t => t.RequestLogID)
                .ForeignKey("dbo.Requests", t => t.Request_RequestID)
                .Index(t => t.Request_RequestID);
            
            AddColumn("dbo.Requests", "RequestID", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.Requests", "RequestID");
           
        }
        
        public override void Down()
        {
            AddColumn("dbo.Requests", "ID", c => c.Int(nullable: false, identity: true));
            DropForeignKey("dbo.RequestLogs", "Request_RequestID", "dbo.Requests");
            DropIndex("dbo.RequestLogs", new[] { "Request_RequestID" });
            DropPrimaryKey("dbo.Requests");
            DropColumn("dbo.Requests", "RequestID");
            DropTable("dbo.RequestLogs");
            AddPrimaryKey("dbo.Requests", "ID");
        }
    }
}
