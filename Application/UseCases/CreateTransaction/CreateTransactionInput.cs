using Core.Transactions;
using Core.Wallets;

namespace Application.UseCases.CreateTransaction;

public record CreateTransactionInput(Guid WalletId, Currency Currency, decimal Amount,
    TransactionType  TransactionType, string? Description);