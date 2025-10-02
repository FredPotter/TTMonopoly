using Application.UseCases.GetTransactionsGrouped;
using Application.UseCases.GetTransactionsGrouped.Dtos;
using Core.Common;
using Core.Gateway;
using Core.Transactions;
using Core.Wallets;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApplicationTests.UseCases
{
    public class GetTransactionsGroupedTests
    {
        private readonly GetTransactionsGroupedHandler _sut;
        
        private readonly Mock<IWalletGateway> _walletGatewayMock;
        private readonly Mock<IValidator<GetTransactionsGroupedInput>> _validatorMock;
        private readonly Mock<ILogger<GetTransactionsGroupedHandler>> _loggerMock;

        public GetTransactionsGroupedTests()
        {
            _walletGatewayMock = new Mock<IWalletGateway>();
            _validatorMock = new Mock<IValidator<GetTransactionsGroupedInput>>();
            _loggerMock = new Mock<ILogger<GetTransactionsGroupedHandler>>();
            
            _sut = new GetTransactionsGroupedHandler(
                _walletGatewayMock.Object,
                _validatorMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldThrowValidationException_WhenInputIsInvalid()
        {
            // Arrange
            var input = GenerateInput(DateTime.Now, DateTime.Now);
            var failures = new[] { new ValidationFailure("WalletIds", "WalletId cannot be an empty GUID") };
            _validatorMock
                .Setup(v => v.ValidateAsync(input, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            // Act & Assert
            await Assert.ThrowsAsync<Application.Exceptions.ValidationException>(() => _sut.Handle(input));
        }

        [Fact]
        public async Task Handle_ShouldReturnFilteredTransactions_WhenDateAndTypeFiltersApplied()
        {
            // Arrange
            var wallet = GenerateTestWallet(200);
            wallet.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = 100,
                Type = TransactionType.Expense,
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Description = "test"
            });
            wallet.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = 50,
                Type = TransactionType.Income,
                TransactionDate = DateTime.UtcNow,
                Description = "test"
            });

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<GetTransactionsGroupedInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _walletGatewayMock
                .Setup(w => w.FindWalletByIdAsync(wallet.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(wallet);

            var input = GenerateInput(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow, [wallet.Id], TransactionType.Income);
            
            // Act
            var response = await _sut.Handle(input);

            // Assert
            Assert.Single(response.GroupDtos);
            Assert.All(response.GroupDtos.SelectMany(g => g.Transactions), t =>
            {
                Assert.Equal(TransactionType.Income, t.Type);
                Assert.True(t.TransactionDate >= input.DateFrom.Value);
            });
        }

        [Fact]
        public async Task Handle_ShouldReturnTopNTransactionsPerGroup_WhenTopNPerGroupIsSet()
        {
            // Arrange
            var wallet = GenerateTestWallet(500);
            for (var i = 0; i < 5; i++)
            {
                wallet.Transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(),
                    Amount = 10 * (i + 1),
                    Type = TransactionType.Expense,
                    TransactionDate = DateTime.UtcNow.AddDays(-i),
                    Description = "test expenses"
                });
            }

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<GetTransactionsGroupedInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _walletGatewayMock
                .Setup(w => w.FindWalletByIdAsync(wallet.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(wallet);

            var input = GenerateInput(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow, [wallet.Id],
                null, 3);

            // Act
            var response = await _sut.Handle(input);

            // Assert
            Assert.Single(response.GroupDtos);
            Assert.Equal(3, response.GroupDtos.First().Transactions.Count);
        }

        private GetTransactionsGroupedInput GenerateInput(DateTime dateFrom, DateTime dateTo, IEnumerable<Guid>? walletId = null, 
            TransactionType? type = null, int? topNPerGroup = null)
        {
            return new GetTransactionsGroupedInput(
                WalletIds: walletId,
                DateFrom: dateFrom,
                DateTo: dateTo,
                TypeFilter: type,
                TopNPerGroup: topNPerGroup,
                GroupSort: new[]
                {
                    new SortDescriptor<TransactionGroupDto>(g => Math.Abs(g.TotalAmount), SortDirection.Descending)
                },
                TransactionSort: new[]
                {
                    new SortDescriptor<Transaction>(t => t.TransactionDate, SortDirection.Ascending)
                }
            );
        }
        
        private Wallet GenerateTestWallet(decimal initialBalance, Currency currency = Currency.USD)
        {
            return new Wallet()
            {
                Id = Guid.NewGuid(),
                Name = "Test Wallet",
                Currency = currency,
                InitialBalance = initialBalance,
                ConcurrencyToken = Guid.NewGuid(),
            };
        }
    }
}
