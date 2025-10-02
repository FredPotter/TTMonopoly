using Application.UseCases.GetTransactionsGrouped.Dtos;
using Core.Gateway;
using Core.Common;
using Core.Interfaces;
using Core.Wallets;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.UseCases.GetTransactionsGrouped;

public class GetTransactionsGroupedHandler : IUseCase<GetTransactionsGroupedResponse, GetTransactionsGroupedInput>
{
    private readonly IWalletGateway _walletGateway;
    private readonly IValidator<GetTransactionsGroupedInput> _validator;
    private readonly ILogger<GetTransactionsGroupedHandler> _logger;

    public GetTransactionsGroupedHandler(IWalletGateway walletGateway, IValidator<GetTransactionsGroupedInput> validator,
        ILogger<GetTransactionsGroupedHandler> logger)
    {
        _walletGateway = walletGateway;
        _validator = validator;
        _logger = logger;
    }

    public async Task<GetTransactionsGroupedResponse> Handle(GetTransactionsGroupedInput input, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(input, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogError("Get transactions grouped validation failed");
            throw new ValidationException("Get transactions grouped validation failed", validationResult.Errors);
        }
        
        _logger.LogInformation("Start get transactions grouped");
        var wallets = (input.WalletIds == null || !input.WalletIds.Any())
            ? await _walletGateway.GetWallets(cancellationToken)
            : await Task.WhenAll(input.WalletIds.Select(id => _walletGateway.FindWalletByIdAsync(id, cancellationToken)))
                .ContinueWith(t => t.Result.Where(w => w != null).Cast<Wallet>().ToList(), cancellationToken);

        var allTransactions = wallets
            .SelectMany(w => w.Transactions)
            .Where(t => (!input.DateFrom.HasValue || t.TransactionDate >= input.DateFrom.Value)
                        && (!input.DateTo.HasValue || t.TransactionDate <= input.DateTo.Value)
                        && (!input.TypeFilter.HasValue || t.Type == input.TypeFilter.Value));

        var grouped = allTransactions
            .GroupBy(t => t.Type)
            .Select(g => new TransactionGroupDto(
                Type: g.Key,
                TotalAmount: g.Sum(t => t.Amount),
                Transactions: input.TransactionSort != null && input.TransactionSort.Any()
                    ? g.ApplySort(input.TransactionSort.First(), input.TransactionSort.Skip(1).ToArray()).ToList()
                    : g.OrderBy(t => t.TransactionDate).ToList()
            ));

        var result = input.GroupSort != null && input.GroupSort.Any()
            ? grouped.ApplySort(input.GroupSort.First(), input.GroupSort.Skip(1).ToArray()).ToList()
            : grouped.OrderByDescending(g => Math.Abs(g.TotalAmount)).ToList();
        
        if (input.TopNPerGroup.HasValue)
        {
            result = result.Select(g => g with { Transactions = g.Transactions.Take(input.TopNPerGroup.Value).ToList() })
                .ToList();
        }
        
        _logger.LogInformation("Finish get transactions grouped");
        var response = new GetTransactionsGroupedResponse(result);
        return response;
    }
}