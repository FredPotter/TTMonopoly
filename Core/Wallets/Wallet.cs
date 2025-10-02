using System.Reflection;
using Core.Transactions;

namespace Core.Wallets;
    
public class Wallet
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public Currency Currency { get; set; }
    public List<Transaction> Transactions { get; set; } = [];
    public decimal InitialBalance { get; set; }
    public Guid ConcurrencyToken { get; set; }
    
    public decimal GetCurrentBalance()
    {
        decimal incomes, expenses;
        (incomes, expenses) = GetAllIncomesAndExpenses();
        return InitialBalance + incomes - expenses;
    }
    
    // If specify externalTransactions when calc Incomes and Expenses only for them
    public (decimal, decimal) GetAllIncomesAndExpenses(List<Transaction>? externalTransactions = null)
    {
        var transactions = externalTransactions ?? this.Transactions;
        decimal totalIncomes = 0;
        decimal totalExpenses = 0;
        foreach (var tc in transactions)
        {
            if (tc.Type == TransactionType.Income)
            {
                totalIncomes += tc.Amount;
                continue;
            }
            totalExpenses += tc.Amount;
        }
        return (totalIncomes, totalExpenses);
    }
}