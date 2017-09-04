namespace Xekina.Data.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Xekina.Data.Models;

    public sealed class Configuration : DbMigrationsConfiguration<Xekina.Data.XekinaWebContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(Xekina.Data.XekinaWebContext context)
        {
            
            if (context.Requests.Where(i => i.ProjectName.ToUpper() == "MULARKY").Count() == 0)
            {
                Request r = new Request
                {
                    ProjectName = "mularky",
                    ProjectDescription = "This is project Mularky",
                    ResourceGroupLocation = "UK South",
                    RequestedBy = "Nick",
                    SubscriptionId = "subscriptionid",
                    DateRequested = new DateTimeOffset(System.DateTime.Now),
                    Status = RequestStatus.Created
                };
                context.Requests.Add(r);
            }
        }
    }
}
