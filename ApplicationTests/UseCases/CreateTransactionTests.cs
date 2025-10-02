using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.UseCases.CreateTransaction;
using Core.Gateway;
using Core.Transactions;
using Core.Wallets;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApplicationTests.UseCases

{
    public class CreateTransactionTests
    {
        private readonly Application.UseCases.CreateTransaction.CreateTransaction _sut;
        
        private readonly Mock<IWalletGateway> _walletGatewayMock;
        private readonly Mock<IValidator<CreateTransactionInput>> _validatorMock;
        private readonly Mock<ILogger<Application.UseCases.CreateTransaction.CreateTransaction>> _loggerMock;

        public CreateTransactionTests()
        {
            _walletGatewayMock = new Mock<IWalletGateway>();
            _validatorMock = new Mock<IValidator<CreateTransactionInput>>();
            _loggerMock = new Mock<ILogger<Application.UseCases.CreateTransaction.CreateTransaction>>();
            
            _sut = new Application.UseCases.CreateTransaction.CreateTransaction(
                _walletGatewayMock.Object,
                _validatorMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenTransactionIsValidAndCurrencyMatches()
        {
            // Arrange
            var input = new CreateTransactionInput(Guid.NewGuid(), Currency.USD, 100, 
                TransactionType.Expense, "test expense");
            
            var wallet = GenerateTestWallet(200);

            _validatorMock
                .Setup(v => v.ValidateAsync(input, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _walletGatewayMock
                .Setup(w => w.FindWalletByIdAsync(input.WalletId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(wallet);

            _walletGatewayMock
                .Setup(w => w.SaveWalletAsync(wallet, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            _walletGatewayMock
                .Setup(w => w.SaveWalletAsync(wallet, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.Handle(input);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("successfully written off", result.Message);
        }

        [Fact]
        public async Task Handle_ShouldThrowValidationException_WhenInputIsInvalid()
        {
            // Arrange
            var input = new CreateTransactionInput(Guid.NewGuid(), Currency.USD, -3, 
                TransactionType.Expense, "test expense");
            
            var failures = new[]
            {
                new ValidationFailure("Amount", "Amount must be greater than zero")
            };
            
            _validatorMock
                .Setup(v => v.ValidateAsync(input, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            // Act & Assert
            await Assert.ThrowsAsync<Application.Exceptions.ValidationException>(() => _sut.Handle(input));
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenWalletNotFound()
        {
            // Arrange
            var input = new CreateTransactionInput(Guid.NewGuid(), Currency.USD, 100, 
                TransactionType.Expense, "test expense");

            _validatorMock
                .Setup(v => v.ValidateAsync(input, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _walletGatewayMock
                .Setup(w => w.FindWalletByIdAsync(input.WalletId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Wallet)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.Handle(input));
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenCurrencyMismatch()
        {
            // Arrange
            var input = new CreateTransactionInput(Guid.NewGuid(), Currency.EUR, 100, 
                TransactionType.Expense, "test expense");
            
            var wallet = GenerateTestWallet(200, Currency.USD);

            _validatorMock
                .Setup(v => v.ValidateAsync(input, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _walletGatewayMock
                .Setup(w => w.FindWalletByIdAsync(input.WalletId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(wallet);

            // Act
            var result = await _sut.Handle(input);

            // Assert
            Assert.False(result.Success);
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
