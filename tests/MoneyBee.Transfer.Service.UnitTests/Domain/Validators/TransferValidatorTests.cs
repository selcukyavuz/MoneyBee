using FluentAssertions;
using MoneyBee.Common.Enums;
using MoneyBee.Transfer.Service.Domain.Transfers;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Transfers.Transfer;

namespace MoneyBee.Transfer.Service.UnitTests.Domain.Transfers;

/// <summary>
/// Tests for Transfer entity business rules and validation methods
/// </summary>
public class TransferValidatorTests
{

    [Theory]
    [InlineData(500, false)]
    [InlineData(999.99, false)]
    [InlineData(1000, false)]
    [InlineData(1000.01, true)]
    [InlineData(5000, true)]
    [InlineData(10000, true)]
    public void RequiresApprovalWait_WithVariousAmounts_ShouldReturnCorrectResult(decimal amount, bool expected)
    {
        // Arrange
        const decimal highAmountThreshold = 1000m;

        // Act
        var requiresApproval = TransferEntity.RequiresApprovalWait(amount, highAmountThreshold);

        // Assert
        requiresApproval.Should().Be(expected);
    }

    [Fact]
    public void CalculateApprovalWaitTime_WithHighAmount_ShouldReturn5MinutesFromNow()
    {
        // Arrange
        var highAmount = 1500m;
        const decimal highAmountThreshold = 1000m;
        const int approvalWaitMinutes = 5;
        var beforeCalculation = DateTime.UtcNow;

        // Act
        var approvalTime = TransferEntity.CalculateApprovalWaitTime(highAmount, highAmountThreshold, approvalWaitMinutes);

        // Assert
        approvalTime.Should().NotBeNull();
        approvalTime!.Value.Should().BeCloseTo(beforeCalculation.AddMinutes(5), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CalculateApprovalWaitTime_WithLowAmount_ShouldReturnNull()
    {
        // Arrange
        var lowAmount = 500m;
        const decimal highAmountThreshold = 1000m;
        const int approvalWaitMinutes = 5;

        // Act
        var approvalTime = TransferEntity.CalculateApprovalWaitTime(lowAmount, highAmountThreshold, approvalWaitMinutes);

        // Assert
        approvalTime.Should().BeNull();
    }

    [Fact]
    public void ValidateForCompletion_WithValidPendingTransfer_ShouldSucceed()
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
        var result = transfer.ValidateForCompletion("98765432109");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateForCompletion_WithCompletedTransfer_ShouldFail()
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
        var result = transfer.ValidateForCompletion("98765432109");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Transfer cannot be completed");
    }

    [Fact]
    public void ValidateForCompletion_WithWrongReceiver_ShouldFail()
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
        var result = transfer.ValidateForCompletion("11111111111");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Receiver identity verification failed");
    }

    [Fact]
    public void ValidateDailyLimit_WithinLimit_ShouldSucceed()
    {
        // Arrange
        var currentTotal = 5000m;
        var newAmount = 2000m;
        var dailyLimit = 10000m;

        // Act
        var result = TransferEntity.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateDailyLimit_ExceedingLimit_ShouldFail()
    {
        // Arrange
        var currentTotal = 8000m;
        var newAmount = 3000m;
        var dailyLimit = 10000m;

        // Act
        var result = TransferEntity.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Daily transfer limit exceeded");
    }

    [Fact]
    public void ValidateDailyLimit_ExactlyAtLimit_ShouldSucceed()
    {
        // Arrange
        var currentTotal = 7000m;
        var newAmount = 3000m;
        var dailyLimit = 10000m;

        // Act
        var result = TransferEntity.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeRejectedDueToFraud_WithHighRisk_ShouldReturnTrue()
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
            riskLevel: RiskLevel.High,
            idempotencyKey: null,
            approvalRequiredUntil: null,
            senderNationalId: "12345678901",
            receiverNationalId: "98765432109"
        );

        // Act
        var shouldReject = transfer.ShouldBeRejectedDueToFraud();

        // Assert
        shouldReject.Should().BeTrue();
    }

    [Theory]
    [InlineData(RiskLevel.Low, false)]
    [InlineData(RiskLevel.Medium, false)]
    [InlineData(null, false)]
    public void ShouldBeRejectedDueToFraud_WithLowOrMediumRisk_ShouldReturnFalse(RiskLevel? riskLevel, bool expected)
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
            riskLevel: riskLevel,
            idempotencyKey: null,
            approvalRequiredUntil: null,
            senderNationalId: "12345678901",
            receiverNationalId: "98765432109"
        );

        // Act
        var shouldReject = transfer.ShouldBeRejectedDueToFraud();

        // Assert
        shouldReject.Should().Be(expected);
    }
}
