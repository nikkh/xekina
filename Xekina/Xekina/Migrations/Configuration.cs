namespace Xekina.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Xekina.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<Xekina.DataAccess.XekinaWebContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(Xekina.DataAccess.XekinaWebContext context)
        {
            Request r = new Request
            {
                ProjectName = "mularky", ProjectDescription = "This is project Mularky", ResourceGroupLocation="UK South", RequestedBy="Nick",
                 SubscriptionId = "subscriptionid", DateRequested = new DateTimeOffset(System.DateTime.Now), Status = RequestStatus.Created
            };
            context.Requests.AddOrUpdate(r);
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
        }
    }
}
