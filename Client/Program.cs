using Application;
using Microsoft.Extensions.DependencyInjection;
using Core.Gateway;
using Infrastructure.FileWallets;
using Application.UseCases.GetTransactionsGrouped;
using Infrastructure;

namespace Client
{
    class Program
    {
        static async Task Main()
        {
            var services = new ServiceCollection();

            // Start appsettings
            string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wallets_storage");
            // End appsettings
            
            services.AddSingleton<IWalletGateway>(provider => new FileWalletGateway(storagePath));
            services.AddTransient<GetTransactionsGroupedHandler>();
            services.AddApplication();
            services.AddInfrastructure(storagePath);
            services.AddTransient<WalletConsoleApp>();

            var serviceProvider = services.BuildServiceProvider();
            
            var app = serviceProvider.GetRequiredService<WalletConsoleApp>();
            await app.RunAsync();
        }
    }
}