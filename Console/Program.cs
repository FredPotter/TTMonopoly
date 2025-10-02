using System;
using System.Globalization;
using Core.Gateway;
using Core.Wallets;
using Infrastructure.FileWallets;
using Application.UseCases.GetTransactionsGrouped;
using Application.UseCases.GetTransactionsGrouped.Dtos;

namespace Console;

class Program
{
    static async Task Main()
    {
        string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wallets");
        IWalletGateway walletGateway = new FileWalletGateway(storagePath);

        Console.WriteLine("=== Wallet Transactions Console ===");

        // Получаем все кошельки
        var wallets = await walletGateway.GetWallets();

        if (!wallets.Any())
        {
            Console.WriteLine("No wallets found in storage.");
            return;
        }

        // Выводим список кошельков и просим выбрать
        Console.WriteLine("Select a wallet by number:");
        for (int i = 0; i < wallets.Count; i++)
        {
            Console.WriteLine($"{i + 1}: {wallets[i].Name} ({wallets[i].Id})");
        }

        int selectedIndex;
        while (true)
        {
            Console.Write("Enter wallet number: ");
            var inputStr = Console.ReadLine();
            if (int.TryParse(inputStr, out selectedIndex) &&
                selectedIndex >= 1 && selectedIndex <= wallets.Count)
                break;
            Console.WriteLine("Invalid input. Try again.");
        }

        var selectedWallet = wallets[selectedIndex - 1];

        // Запрашиваем месяц и год
        Console.Write("Enter year (e.g., 2025): ");
        int year = int.Parse(Console.ReadLine()!);

        Console.Write("Enter month (1-12): ");
        int month = int.Parse(Console.ReadLine()!);

        var dateFrom = new DateTime(year, month, 1);
        var dateTo = dateFrom.AddMonths(1).AddTicks(-1); // конец месяца

        Console.WriteLine("\n=== Scenario 1: Grouped Transactions ===");

        // Фильтруем транзакции по выбранному кошельку и месяцу
        var transactions = selectedWallet.Transactions
            .Where(t => t.Timestamp >= dateFrom && t.Timestamp <= dateTo)
            .ToList();

        var grouped = transactions
            .GroupBy(t => t.Type)
            .Select(g => new TransactionGroupDto(
                Type: g.Key,
                TotalAmount: g.Sum(t => t.Amount),
                Transactions: g.OrderBy(t => t.Timestamp).ToList()
            ))
            .OrderByDescending(g => Math.Abs(g.TotalAmount))
            .ToList();

        foreach (var group in grouped)
        {
            Console.WriteLine($"\nType: {group.Type}, Total Amount: {group.TotalAmount}");
            foreach (var tx in group.Transactions)
            {
                Console.WriteLine($"  {tx.Timestamp:yyyy-MM-dd}: {tx.Amount} ({tx.Description})");
            }
        }

        Console.WriteLine("\n=== Scenario 2: Top 3 Expenses for each wallet ===");

        foreach (var wallet in wallets)
        {
            var topExpenses = wallet.Transactions
                .Where(t => t.Amount < 0 &&
                            t.Timestamp >= dateFrom && t.Timestamp <= dateTo)
                .OrderByDescending(t => Math.Abs(t.Amount))
                .Take(3)
                .ToList();

            Console.WriteLine($"\nWallet: {wallet.Name} ({wallet.Id})");
            if (!topExpenses.Any())
            {
                Console.WriteLine("  No expenses for this month.");
                continue;
            }

            for (int i = 0; i < topExpenses.Count; i++)
            {
                var tx = topExpenses[i];
                Console.WriteLine($"  {i + 1}. {tx.Timestamp:yyyy-MM-dd}: {tx.Amount} ({tx.Description})");
            }
        }

        Console.WriteLine("\n=== Done ===");
    }
}
