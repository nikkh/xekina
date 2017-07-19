using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xekina.Web.Data;

namespace Xekina.Web.Migrations
{
    [DbContext(typeof(XekinaWebContext))]
    [Migration("20170718121726_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Xekina.Web.Models.Movie", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Genre");

                    b.Property<decimal>("Price");

                    b.Property<DateTime>("ReleaseDate");

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.ToTable("Movie");
                });

            modelBuilder.Entity("Xekina.Web.Models.Profile", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.HasKey("ID");

                    b.ToTable("Profile");
                });

            modelBuilder.Entity("Xekina.Web.Models.SubscriptionLink", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("ProfileID");

                    b.Property<string>("SubscriptionId");

                    b.Property<string>("SubscriptionName");

                    b.Property<bool>("Validated");

                    b.HasKey("ID");

                    b.HasIndex("ProfileID");

                    b.ToTable("SubscriptionLink");
                });

            modelBuilder.Entity("Xekina.Web.Models.SubscriptionLink", b =>
                {
                    b.HasOne("Xekina.Web.Models.Profile")
                        .WithMany("Subscriptions")
                        .HasForeignKey("ProfileID");
                });
        }
    }
}
