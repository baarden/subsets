using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CsvToPostgres;

class Program
{
    static void Main(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .Build();

        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: Connection string 'DefaultConnection' not found in configuration.");
            Console.WriteLine($"Current environment: {environment}");
            return;
        }

        RunMigrations(connectionString);
    }

    static void RunMigrations(string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        UpgradeEngineBuilder builder =
            DeployChanges.To
                .PostgresqlDatabase(connectionString)
                .WithScriptsAndCodeEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .WithTransactionPerScript()
                .LogToConsole();

        builder.Configure(c =>
        {
            c.ScriptExecutor.ExecutionTimeoutSeconds = 60 * 60; // 60 minutes in seconds
        });

        UpgradeEngine upgrader = builder.Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.WriteLine(result.Error);
        }
    }

}
