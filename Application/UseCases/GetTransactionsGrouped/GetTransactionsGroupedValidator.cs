using FluentValidation;

namespace Application.UseCases.GetTransactionsGrouped;

public class GetTransactionsGroupedValidator: AbstractValidator<GetTransactionsGroupedInput>
{
    public GetTransactionsGroupedValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom must be less than or equal to DateTo");
        
        When(x => x.TopNPerGroup.HasValue, () =>
        {
            RuleFor(x => x.TopNPerGroup.Value)
                .GreaterThan(0)
                .WithMessage("TopNPerGroup must be greater than 0");
        });
        
        When(x => x.WalletIds != null, () =>
        {
            RuleForEach(x => x.WalletIds)
                .NotEqual(Guid.Empty)
                .WithMessage("WalletId cannot be an empty GUID");
        });
        
        When(x => x.GroupSort != null, () =>
        {
            RuleForEach(x => x.GroupSort)
                .NotNull()
                .WithMessage("Group SortDescriptor cannot be null");
        });
        
        When(x => x.TransactionSort != null, () =>
        {
            RuleForEach(x => x.TransactionSort)
                .NotNull()
                .WithMessage("Transaction SortDescriptor cannot be null");
        });
    }
}