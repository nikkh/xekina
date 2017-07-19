using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xekina.Web.Models;

namespace Xekina.Web.Data
{
    public class XekinaWebContext : DbContext
    {
        public XekinaWebContext (DbContextOptions<XekinaWebContext> options)
            : base(options)
        {
        }

        
        public DbSet<Profile> Profile { get; set; }
        public DbSet<SubscriptionLink> SubscriptionLink { get; set; }
    }
}
