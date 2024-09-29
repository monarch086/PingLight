using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using PingLight.WebApi.Data;

namespace PingLight.WebApi.ServiceExtensions
{
    public static class AuthExtensions
    {
        public static void AddOauthAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentityApiEndpoints<IdentityUser>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.User.RequireUniqueEmail = true;
                opt.Password.RequireNonAlphanumeric = false;
                opt.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("WeatherPolicy", policy =>
                    policy.RequireRole("WeatherUser"));
            });

            services.AddDataProtection()
                .PersistKeysToDbContext<DataContext>();
        }
    }
}
