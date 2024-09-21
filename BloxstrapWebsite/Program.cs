using BloxstrapWebsite.Models.Configuration;
using Coravel;

namespace BloxstrapWebsite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<Credentials>(builder.Configuration.GetSection("Credentials"));

            builder.Services.AddSingleton<IStatsService, StatsService>();

            builder.Services.AddScheduler();
            builder.Services.AddTransient<StatsJobInvocable>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            app.Services.UseScheduler(scheduler => scheduler.Schedule<StatsJobInvocable>().Hourly());

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
