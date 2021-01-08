using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SoapyBackend
{
    public class DbMigratorHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _lifetime;

        public DbMigratorHostedService(IServiceProvider serviceProvider,
            IHostApplicationLifetime lifetime)
        {
            _serviceProvider = serviceProvider;
            _lifetime = lifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {


            using var scope = _serviceProvider.CreateScope();
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
                var timeout = 50;
                var tries = 0;
                var canConnect = false;
                while (!canConnect)
                {
                    if (tries > timeout)
                    {
                        Log.Fatal("Failed to contact DB, exiting application");
                        Environment.ExitCode = 1;
                        _lifetime.StopApplication();
                        break;
                    }

                    Log.Information("Attempting to contact database: Attempt #{@Tries}", tries);
                    try
                    {
                        canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Log.Information("Cannot Connect: {@Message}", e.Message);
                    }

                    tries++;
                }

                Log.Information("Contacted Database Successfully!");

                await dbContext.Database.MigrateAsync(cancellationToken);
                Log.Information("Database migrated successfully.");
            }
            catch (Exception e)
            {
                Log.Fatal("Database migration failed: {@Message}", e.Message);
                Log.Fatal(e.StackTrace);
                Environment.ExitCode = 1;
                _lifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}