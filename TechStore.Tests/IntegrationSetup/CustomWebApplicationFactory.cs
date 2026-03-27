using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using TechStore.Models;

namespace TechStore.Tests.IntegrationSetup
{
    public class CustomWebApplicationFactory<TProgram>
        : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Xóa DbContext thật (SQL Server)
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TechStoreContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Thêm InMemory Database
                services.AddDbContext<TechStoreContext>(options =>
                {
                    options.UseInMemoryDatabase("TechStoreTestDb");
                });

                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<TechStoreContext>();
                    db.Database.EnsureCreated();
                }
            });
        }
    }
}