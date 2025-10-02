using Application.UseCases.GetTransactionsGrouped.Dtos;
using Core.Common;
using Core.Transactions;

namespace Application.UseCases.GetTransactionsGrouped;

public record GetTransactionsGroupedInput(
    IEnumerable<Guid>? WalletIds = null, // if null - all wallets
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    TransactionType? TypeFilter = null,
    IEnumerable<SortDescriptor<TransactionGroupDto>>? GroupSort = null,
    IEnumerable<SortDescriptor<Transaction>>? TransactionSort = null,
    int? TopNPerGroup = null
);
