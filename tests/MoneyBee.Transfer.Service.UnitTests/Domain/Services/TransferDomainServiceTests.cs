using FluentAssertions;
using MoneyBee.Common.Enums;
using MoneyBee.Transfer.Service.Domain.Services;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.UnitTests.Domain.Services;

public class TransferDomainServiceTests
{
    private readonly TransferDomainService _domainService;

    public TransferDomainServiceTests()
    {
        _domainService = new TransferDomainService();
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(999.99, false)]
    [InlineData(1000, false)]
    [InlineData(1000.01, true)]
    [InlineData(5000, true)]
    [InlineData(10000, true)]
    public void RequiresApprovalWait_WithVariousAmounts_ShouldReturnCorrectResult(decimal amount, bool expected)
    {
        // Act
        var requiresApproval = _domainService.RequiresApprovalWait(amount);

        // Assert
        requiresApproval.Should().Be(expected);
    }

    [Fact]
    public void CalculateApprovalWaitTime_WithHighAmount_ShouldReturn5MinutesFromNow()
    {
        // Arrange
        var highAmount = 1500m;
        var beforeCalculation = DateTime.UtcNow;

        // Act
        var approvalTime = _domainService.CalculateApprovalWaitTime(highAmount);

        // Assert
        approvalTime.Should().NotBeNull();
        approvalTime!.Value.Should().BeCloseTo(beforeCalculation.AddMinutes(5), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CalculateApprovalWaitTime_WithLowAmount_ShouldReturnNull()
    {
        // Arrange
        var lowAmount = 500m;

        // Act
        var approvalTime = _domainService.CalculateApprovalWaitTime(lowAmount);

        // Assert
        approvalTime.Should().BeNull();
    }

    [Fact]
    public void ValidateTransferForCompletion_WithValidPendingTransfer_ShouldNotThrow()
    {
        // Arrange
        var transfer = TransferEntity.Create(
            senderId: Guid.NewGuid(),
            receiverId: Guid.NewGuid(),
            amount: 500m,
            currency: Currency.TRY,
            amountInTRY: 500m,
            exchangeRate: null,
            transactionFee: 0m,
            transactionCode: "TXN123456",
            riskLevel: RiskLevel.Low,
            idempotencyKey: null,
            approvalRequiredUntil: null,
            senderNationalId: "12345678901",
            receiverNationalId: "98765432109"
        );

        // Act
        var act = () => _domainService.ValidateTransferForCompletion(transfer, "98765432109");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTransferForCompletion_WithCompletedTransfer_ShouldThrow()
    {
        // Arrange
        var transfer = TransferEntity.Create(
            senderId: Guid.NewGuid(),
            receiverId: Guid.NewGuid(),
            amount: 500m,
            currency: Currency.TRY,
            amountInTRY: 500m,
            exchangeRate: null,
            transactionFee: 0m,
            transactionCode: "TXN123456",
            riskLevel: RiskLevel.Low,
            idempotencyKey: null,
            approvalRequiredUntil: null,
            senderNationalId: "12345678901",
            receiverNationalId: "98765432109"
        );
        // Complete the transfer
        transfer.Complete();

        // Act
        var act = () => _domainService.ValidateTransferForCompletion(transfer, "98765432109");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Transfer cannot be completed*");
    }

    [Fact]
    public void ValidateTransferForCompletion_WithWrongReceiver_ShouldThrow()
    {
        // Arrange
        var transfer = TransferEntity.Create(
            senderId: Guid.NewGuid(),
            receiverId: Guid.NewGuid(),
            amount: 500m,
            currency: Currency.TRY,
            amountInTRY: 500m,
            exchangeRate: null,
            transactionFee: 0m,
            transactionCode: "TXN123456",
            riskLevel: RiskLevel.Low,
            idempotencyKey: null,
            approvalRequiredUntil: null,
            senderNationalId: "12345678901",
            receiverNationalId: "98765432109"
        );

        // Act
        var act = () => _domainService.ValidateTransferForCompletion(transfer, "11111111111");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Receiver identity verification failed");
    }

    [Fact]
    public void ValidateDailyLimit_WithinLimit_ShouldNotThrow()
    {
        // Arrange
        var currentTotal = 5000m;
        var newAmount = 2000m;
        var dailyLimit = 10000m;

        // Act
        var act = () => _domainService.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateDailyLimit_ExceedingLimit_ShouldThrow()
    {
        // Arrange
        var currentTotal = 8000m;
        var newAmount = 3000m;
        var dailyLimit = 10000m;

        // Act
        var act = () => _domainService.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Daily transfer limit exceeded*");
    }

    [Fact]
    public void ValidateDailyLimit_ExactlyAtLimit_ShouldNotThrow()
    {
        // Arrange
        var currentTotal = 7000m;
        var newAmount = 3000m;
        var dailyLimit = 10000m;

        // Act
        var act = () => _domainService.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(RiskLevel.Low, false)]
    [InlineData(RiskLevel.Medium, false)]
    [InlineData(RiskLevel.High, true)]
    [InlineData(null, false)]
    public void ShouldRejectTransfer_WithVariousRiskLevels_ShouldReturnCorrectResult(RiskLevel? riskLevel, bool expected)
    {
        // Act
        var shouldReject = _domainService.ShouldRejectTransfer(riskLevel);

        // Assert
        shouldReject.Should().Be(expected);
    }
}
