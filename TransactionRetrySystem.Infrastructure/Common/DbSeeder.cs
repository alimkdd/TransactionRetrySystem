using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransactionRetrySystem.Infrastructure.Context;

namespace TransactionRetrySystem.Infrastructure.Common;

public static class DbSeeder
{
    public static async Task Initialize(AppDbContext context, IHostEnvironment environment, IConfiguration configuration)
    {
        string databaseDirectory;

        if (environment.IsDevelopment())
        {
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var solutionDirectory = Path.Combine(currentDirectory, @"../../../../");
            databaseDirectory = Path.GetFullPath(Path.Combine(solutionDirectory, "TransactionRetrySystem.Infrastructure"));
        }
        else
        {
            databaseDirectory = Directory.GetCurrentDirectory();
        }

        string dataDir = Path.Combine(databaseDirectory, "Data");

        if (!Directory.Exists(dataDir))
        {
            Console.WriteLine("Data folder not found. Skipping normal table seeding.");
            return;
        }

        var seedingOrder = new string[]
        {
            "Status.json",
            "ErrorType.json",
            "User.json",
            "TransactionAttempt.json"
        };

        // Seed ordered files first
        foreach (var fileName in seedingOrder)
        {
            var filePath = Path.Combine(dataDir, fileName);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"{fileName} not found. Skipping.");
                continue;
            }

            await SeedJsonFile(context, dataDir, fileName);
        }

        // Seed remaining files dynamically
        var allFiles = Directory.GetFiles(dataDir, "*.json").Select(Path.GetFileName);
        var remainingFiles = allFiles.Except(seedingOrder);

        foreach (var fileName in remainingFiles)
        {
            await SeedJsonFile(context, dataDir, fileName);
        }
    }

    private static async Task SeedJsonFile(AppDbContext context, string dataDir, string fileName)
    {
        var entityName = Path.GetFileNameWithoutExtension(fileName);

        var entityAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetTypes().Any(t => t.Namespace?.StartsWith("TransactionRetrySystem.Domain.Models") == true));

        var entityType = entityAssembly?
            .GetTypes()
            .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        if (entityType == null)
        {
            Console.WriteLine($"Entity class '{entityName}' not found. Skipping {fileName}.");
            return;
        }

        if (entityType.IsEnum || entityType.Namespace?.Contains(".Enums") == true)
        {
            Console.WriteLine($"{entityType.Name} is an enum or in Enums folder. Skipping seeding.");
            return;
        }

        var method = typeof(DbSeeder).GetMethod(nameof(SeedTable), BindingFlags.NonPublic | BindingFlags.Static);
        var generic = method.MakeGenericMethod(entityType);
        await (Task)generic.Invoke(null, new object[] { context, dataDir, fileName });
    }

    private static async Task SeedTable<T>(AppDbContext context, string databaseDirectory, string fileName) where T : class
    {
        if (context.Set<T>().Any())
        {
            Console.WriteLine($"{typeof(T).Name} table already has data. Skipping.");
            return;
        }

        var jsonFilePath = Path.Combine(databaseDirectory, fileName);

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"{fileName} not found. Skipping.");
            return;
        }

        var jsonData = await File.ReadAllTextAsync(jsonFilePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var data = JsonSerializer.Deserialize<List<T>>(jsonData, options);

        if (data == null || !data.Any()) return;

        foreach (var item in data)
        {
            var createdOnProp = item.GetType().GetProperty("CreatedOn");
            if (createdOnProp != null && createdOnProp.CanWrite)
                createdOnProp.SetValue(item, DateTime.UtcNow);

            var isDeletedProp = item.GetType().GetProperty("IsDeleted");
            if (isDeletedProp != null && isDeletedProp.CanWrite)
                isDeletedProp.SetValue(item, false);
        }

        await context.Set<T>().AddRangeAsync(data);
        await context.SaveChangesAsync();

        Console.WriteLine($"{data.Count} records inserted into {typeof(T).Name} table.");
    }
}