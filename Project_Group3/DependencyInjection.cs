using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project_Group3.Models;
using Project_Group3.Services;

namespace Project_Group3;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnectionStringDB")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnectionStringDB' not found.");

        services.AddDbContext<CloneEbayDbContext>(options => options.UseSqlServer(connectionString));

        services.AddRepositoriesFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();

        return services;
    }
    

    private static IServiceCollection AddRepositoriesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var repositoryTypes = assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Repository", StringComparison.Ordinal));

        foreach (var implementationType in repositoryTypes)
        {
            // Convention: UserRepository -> IUserRepository
            // Works even if interface is placed in a sub-namespace/folder (e.g. Repository.Interfaces)
            var baseName = implementationType.Name[..^"Repository".Length];
            var expectedInterfaceName = $"I{baseName}Repository";

            var serviceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => string.Equals(i.Name, expectedInterfaceName, StringComparison.Ordinal));

            if (serviceType is null)
            {
                continue;
            }

            services.AddScoped(serviceType, implementationType);
        }

        return services;
    }
}
