namespace Xekina.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class githubpatinuserdefaults : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserDefaults", "GitHubPersonalAccessToken", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserDefaults", "GitHubPersonalAccessToken");
        }
    }
}
