using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Seeders
{
    public static class EventSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Check if data already exists
            if (context.Events.Any())
            {
                return; // DB has been seeded
            }

            // Seed Events
            var events = new[]
            {
                new Event { EventName = "Hackathon", IsActive = true },
                new Event { EventName = "CI Academy", IsActive = true },
            };
            context.Events.AddRange(events);
            context.SaveChanges();
        }
    }
}
