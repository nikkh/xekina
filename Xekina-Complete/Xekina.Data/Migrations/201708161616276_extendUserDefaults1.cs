namespace Xekina.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class extendUserDefaults1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserDefaults", "ResourceGroupLocation", c => c.String());
            AddColumn("dbo.UserDefaults", "ArtifactRepoUri", c => c.String());
            AddColumn("dbo.UserDefaults", "ArtifactRepoFolder", c => c.String());
            AddColumn("dbo.UserDefaults", "ArtifactRepoBranch", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserDefaults", "ArtifactRepoBranch");
            DropColumn("dbo.UserDefaults", "ArtifactRepoFolder");
            DropColumn("dbo.UserDefaults", "ArtifactRepoUri");
            DropColumn("dbo.UserDefaults", "ResourceGroupLocation");
        }
    }
}
