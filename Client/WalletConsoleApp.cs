using Core.Gateway;
using Application.UseCases.GetTransactionsGrouped;
using Application.UseCases.GetTransactionsGrouped.Dtos;
using Core.Common;
using Core.Transactions;

namespace Client
{
    public class WalletConsoleApp
    {
        private readonly IWalletGateway _walletGateway;
        private readonly GetTransactionsGroupedHandler _handler;

        public WalletConsoleApp(IWalletGateway walletGateway, GetTransactionsGroupedHandler handler)
        {
            _walletGateway = walletGateway;
            _handler = handler;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=== Wallet Transactions Console ===");

            var walletIds = await _walletGateway.GetWalletIds();

            if (!walletIds.Any())
            {
                Console.WriteLine("No wallets found in storage.");
                return;
            }

            var selectedWalletId = SelectWallet(walletIds);
            var (dateFrom, dateTo) = GetMonthYear();

            await ShowGroupedTransactions(selectedWalletId, dateFrom, dateTo);
            await ShowTop3Expenses(dateFrom, dateTo);

            Console.WriteLine("\n=== Done ===");
        }

        private Guid SelectWallet(List<Guid> walletIds)
        {
            Console.WriteLine("Select a wallet by number:");
            for (int i = 0; i < walletIds.Count; i++)
                Console.WriteLine($"{i + 1}: {walletIds[i]}");

            int selectedIndex;
            while (true)
            {
                Console.Write("Enter wallet number: ");
                var inputStr = Console.ReadLine();
                if (int.TryParse(inputStr, out selectedIndex) &&
                    selectedIndex >= 1 && selectedIndex <= walletIds.Count)
                    break;
                Console.WriteLine("Invalid input. Try again.");
            }

            return walletIds[selectedIndex - 1];
        }

        private (DateTime dateFrom, DateTime dateTo) GetMonthYear()
        {
            Console.Write("Enter year (e.g., 2025): ");
            int year = int.Parse(Console.ReadLine()!);

            Console.Write("Enter month (1-12): ");
            int month = int.Parse(Console.ReadLine()!);

            var dateFrom = new DateTime(year, month, 1);
            var dateTo = dateFrom.AddMonths(1).AddTicks(-1);
            return (dateFrom, dateTo);
        }

        private async Task ShowGroupedTransactions(Guid walletId, DateTime dateFrom, DateTime dateTo)
        {
            var input = new GetTransactionsGroupedInput(
                WalletIds: new[] { walletId },
                DateFrom: dateFrom,
                DateTo: dateTo,
                TypeFilter: null,
                TopNPerGroup: null,
                GroupSort: new[]
                {
                    new SortDescriptor<TransactionGroupDto>(g => Math.Abs(g.TotalAmount), SortDirection.Descending)
                },
                TransactionSort: new[]
                {
                    new SortDescriptor<Transaction>(t => t.TransactionDate, SortDirection.Ascending)
                }
            );

            var result = await _handler.Handle(input, CancellationToken.None);

            Console.WriteLine("\n=== Grouped Transactions ===");
            foreach (var group in result.GroupDtos)
            {
                Console.WriteLine($"\nType: {group.Type}, TotalAmount: {group.TotalAmount}");
                foreach (var tx in group.Transactions)
                {
                    Console.WriteLine($"  {tx.TransactionDate:yyyy-MM-dd}: {tx.Amount} ({tx.Description})");
                }
            }
        }

        private async Task ShowTop3Expenses(DateTime dateFrom, DateTime dateTo)
        {
            var input = new GetTransactionsGroupedInput(
                WalletIds: null,
                DateFrom: dateFrom,
                DateTo: dateTo,
                TypeFilter: TransactionType.Expense,
                TopNPerGroup: 3
            );

            var result = await _handler.Handle(input);

            Console.WriteLine("\n=== Top 3 Expenses per Wallet ===");
            foreach (var group in result.GroupDtos)
            {
                Console.WriteLine($"\nWallet: {group.Type}");
                foreach (var tx in group.Transactions)
                {
                    Console.WriteLine($"  {tx.TransactionDate:yyyy-MM-dd}: {tx.Amount} ({tx.Description})");
                }
            }
        }
    }
}
