namespace Xekina.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class requestlogone2many : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.RequestLogs", "Request_RequestID", "dbo.Requests");
            DropIndex("dbo.RequestLogs", new[] { "Request_RequestID" });
            RenameColumn(table: "dbo.RequestLogs", name: "Request_RequestID", newName: "RequestRefId");
            AlterColumn("dbo.RequestLogs", "RequestRefId", c => c.Int(nullable: false));
            CreateIndex("dbo.RequestLogs", "RequestRefId");
            AddForeignKey("dbo.RequestLogs", "RequestRefId", "dbo.Requests", "RequestID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RequestLogs", "RequestRefId", "dbo.Requests");
            DropIndex("dbo.RequestLogs", new[] { "RequestRefId" });
            AlterColumn("dbo.RequestLogs", "RequestRefId", c => c.Int());
            RenameColumn(table: "dbo.RequestLogs", name: "RequestRefId", newName: "Request_RequestID");
            CreateIndex("dbo.RequestLogs", "Request_RequestID");
            AddForeignKey("dbo.RequestLogs", "Request_RequestID", "dbo.Requests", "RequestID");
        }
    }
}
