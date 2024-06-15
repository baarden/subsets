using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using Npgsql;

namespace CsvToPostgres;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Host=localhost;Username=admin;Password=T9Bt4M7tSB!r;Database=subsets2";

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
