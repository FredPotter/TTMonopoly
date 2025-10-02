using Application.UseCases.GetTransactionsGrouped.Dtos;

namespace Application.UseCases.GetTransactionsGrouped;

public record GetTransactionsGroupedResponse(List<TransactionGroupDto> GroupDtos);