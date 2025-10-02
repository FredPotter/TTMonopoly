using Application.UseCases.CreateTransaction;
using Application.UseCases.GetTransactionsGrouped;
using Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateTransactionInput>, CreateTransactionValidator>();
        services.AddScoped<IUseCase<CreateTransactionResponse, CreateTransactionInput>, CreateTransaction>();
        services.AddScoped<IValidator<GetTransactionsGroupedInput>, GetTransactionsGroupedValidator>();
        services.AddScoped<IUseCase<GetTransactionsGroupedResponse, GetTransactionsGroupedInput>, GetTransactionsGroupedHandler>();
        return services;
    }
}