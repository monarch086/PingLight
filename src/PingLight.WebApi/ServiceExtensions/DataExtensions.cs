using Microsoft.EntityFrameworkCore;
using PingLight.WebApi.Data;

namespace PingLight.WebApi.ServiceExtensions
{
    public static class DataExtensions
    {
        public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DataContext"))
                    .EnableSensitiveDataLogging());
        }

        public static void ApplyMigrations(this IHost webApp)
        {
            using var scope = webApp.Services.CreateScope();

            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during applying migrations to the DB: {ex.Message}");
                throw;
            }
        }
    }
}
