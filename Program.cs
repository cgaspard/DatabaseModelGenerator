using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        var configuration = LoadConfiguration();
        string connectionString = configuration.GetConnectionString("DefaultConnection");
        string outputDirectory = "./Models";
        string schema = configuration["Schema"];

        var sqlClientFactory = SqlClientFactory.Instance;
        var generator = new DatabaseModelGenerator(connectionString, sqlClientFactory, schema);
        generator.GenerateModels(outputDirectory);

        Console.WriteLine("C# models generated successfully!");
    }

    static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        return builder.Build();
    }
}
