using Core.Transactions;

namespace Application.UseCases.GetTransactionsGrouped.Dtos;

public record TransactionGroupDto(TransactionType Type, decimal TotalAmount, List<Transaction> Transactions);