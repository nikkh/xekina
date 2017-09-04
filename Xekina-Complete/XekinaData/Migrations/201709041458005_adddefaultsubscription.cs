namespace Xekina.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class adddefaultsubscription : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserDefaults", "DefaultSubscription", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserDefaults", "DefaultSubscription");
        }
    }
}
