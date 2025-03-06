using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrueText.Models;
using WIA;

namespace TrueText.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<TrueText.Models.Device> Devices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=TrueText.db");

        public static void AddDevice(TrueText.Models.Device device)
        {
            using (var db = new AppDbContext())
            {
                db.Devices.Add(device);
                db.SaveChanges();
            }
        }

        public static List<TrueText.Models.Device> GetDevices()
        {
            using (var db = new AppDbContext())
            {
                return db.Devices.ToList();
            }
        }
    }
}
