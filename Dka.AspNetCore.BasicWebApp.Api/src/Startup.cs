using System;
using System.Reflection;
using Dka.AspNetCore.BasicWebApp.Api.Services.ServiceCollection;
using Dka.AspNetCore.BasicWebApp.Common.Models.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using DbUp;
using Dka.AspNetCore.BasicWebApp.Api.Models.ExceptionProcessing;
using Microsoft.Extensions.Logging;

namespace Dka.AspNetCore.BasicWebApp.Api
{
    public class Startup
    {
        private readonly string _appName;
        private readonly IConfiguration _configuration;
        private readonly DatabaseConfiguration _databaseConfiguration;
        
        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            _appName = environment.ApplicationName;
            _configuration = configuration;
            _databaseConfiguration = new DatabaseConfiguration();
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            _configuration.GetSection($"{_appName}:BaseWebAppContext").Bind(_databaseConfiguration);

            services.AddSingleton(_databaseConfiguration);
            services.AddDatabaseClasses(_databaseConfiguration);
            services
                .AddHealthChecks()
                .AddSqlServer(_databaseConfiguration.ConnectionString, null, "Database", null, new[] {"db-status-check"}, null)
                .AddCheck("Memory", () => HealthCheckResult.Healthy(), new [] { "memory-status-check" });
            
            
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> logger)
        {
            RunDbMigrations(logger);
            
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseRouting();
            
            app.UseEndpoints(configure =>
            {
                configure.MapControllerRoute("administration", "Administration/{controller=Home}/{action=Index}/{id?}");
                configure.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                configure.MapHealthChecks("/health");
                configure.MapHealthChecks("/health/database", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("db-status-check")
                });
                configure.MapHealthChecks("/health/memory", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("memory-status-check")
                });
                configure.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("db-status-check") && check.Tags.Contains("memory-status-check") 
                });
                configure.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = _ => false
                });
                configure.MapRazorPages();
            });
        }

        private void RunDbMigrations(ILogger<Startup> logger)
        {
            EnsureDatabase.For.SqlDatabase(_databaseConfiguration.ConnectionString);
            
            var upgrader =
                DeployChanges.To
                    .SqlDatabase(_databaseConfiguration.ConnectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .LogToConsole()
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                logger.LogError(result.Error.Message);
                
                throw new ApiDbRunMigrationsException("Database migrations run failure", result.Error);
            }

            logger.LogInformation("Database migrations run success.");
        }
    }
}