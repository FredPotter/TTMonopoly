using Application.UseCases.CreateTransaction;
using Core.Gateway;
using FluentValidation;
using Infrastructure.FileWallets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string storagePath)
    {
        services.AddScoped<IWalletGateway>(provider => new FileWalletGateway(storagePath));

        // LOGGING
        services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Information);
        });
        
        return services;
    }
}