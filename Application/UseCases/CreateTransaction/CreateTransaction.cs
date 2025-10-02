using Application.Exceptions;
using Core.Gateway;
using Core.Interfaces;
using Core.Transactions;
using Core.Wallets;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.CreateTransaction;

public class CreateTransaction : IUseCase<CreateTransactionResponse, CreateTransactionInput>
{
    private readonly IWalletGateway _walletGateway;
    private readonly IValidator<CreateTransactionInput> _validator;
    private readonly ILogger<CreateTransaction> _logger;

    public CreateTransaction(IWalletGateway walletGateway, IValidator<CreateTransactionInput> validator,
        ILogger<CreateTransaction> logger)
    {
        _walletGateway = walletGateway;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<CreateTransactionResponse> Handle(CreateTransactionInput input, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(input, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogError("Create transaction validation failed for wallet with id {WalletId}", input.WalletId);
            throw new Exceptions.ValidationException("Create transaction validation failed", validationResult.Errors);
        }

        _logger.LogInformation("Start creating transaction for wallet with id {walletId}",  input.WalletId);
        
        var wallet = await _walletGateway.FindWalletByIdAsync(input.WalletId, cancellationToken);
        if (wallet is null)
        {
            _logger.LogError("Failed to find wallet with id {WalletId}", input.WalletId);
            throw new NotFoundException($"Wallet with id {input.WalletId} not found");
        }

        if (wallet.Currency != input.Currency)
        {
            return new CreateTransactionResponse(
        false, $"Failed to create transaction" + $" because of currency of the wallet {wallet.Id}." +
               $" The {Enum.GetName(wallet.Currency)} currency was expected," + 
               $" but {Enum.GetName(input.Currency)} received"
            );
        }

        var response = await ApplyTransaction(wallet, input, cancellationToken);
        _logger.LogInformation("{message}", response.Message);
        return response;
    }

    private async Task<CreateTransactionResponse> ApplyTransaction(Wallet wallet, CreateTransactionInput input,
        CancellationToken cancellationToken)
    {
        var transaction = new Transaction()
        {
            Id = Guid.NewGuid(),
            TransactionDate = DateTime.UtcNow,
            Amount = input.Amount,
            Type = TransactionType.Expense,
            Description = input.Description,
        };
        var response = input.TransactionType switch
        {
            TransactionType.Expense => await TryWithdrawMoney(wallet, transaction, cancellationToken),
            TransactionType.Income => await TryDepositMoney(wallet, transaction, cancellationToken),
            _ => throw new ArgumentOutOfRangeException($"TransactionType out of range " +
                                                       $"while create transaction")
        };
        return response;
    }

    private async Task<CreateTransactionResponse> TryWithdrawMoney(Wallet wallet, Transaction transaction,
        CancellationToken cancellationToken)
    {
        if (wallet.GetCurrentBalance() < transaction.Amount)
        {
            return new CreateTransactionResponse(false, "Not enough money");
        }
        wallet.Transactions.Add(transaction);
        var saveResult = await _walletGateway.SaveWalletAsync(wallet, true, cancellationToken);
        if (saveResult) 
            return new CreateTransactionResponse(true, $"The money has been successfully written off" +
                                                                   $" from the wallet {wallet.Id}");
        return new CreateTransactionResponse(false, $"Failed to Withdraw money");
    }

    private async Task<CreateTransactionResponse> TryDepositMoney(Wallet wallet, Transaction transaction,
        CancellationToken cancellationToken)
    {
        wallet.Transactions.Add(transaction);
        await _walletGateway.SaveWalletAsync(wallet, false, cancellationToken);
        return new CreateTransactionResponse(true, $"The money has been successfully credited" +
                                                                   $" to the wallet {wallet.Id}");
    }
}