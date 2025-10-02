using FluentValidation;

namespace Application.UseCases.CreateTransaction;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionInput>
{
    public CreateTransactionValidator()
    {
        RuleFor(ct => ct.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("Wallet id is required.");
        RuleFor(ct => ct.Description).NotNull().WithMessage("Description must not be null");
    }
}