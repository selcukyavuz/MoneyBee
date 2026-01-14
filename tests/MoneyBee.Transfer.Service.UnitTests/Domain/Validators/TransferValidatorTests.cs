using FluentAssertions;
using MoneyBee.Common.Enums;
using MoneyBee.Transfer.Service.Domain.Transfers;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Transfers.Transfer;

namespace MoneyBee.Transfer.Service.UnitTests.Domain.Transfers;

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
        var requiresApproval = TransferValidator.RequiresApprovalWait(amount, highAmountThreshold);

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
        var approvalTime = TransferValidator.CalculateApprovalWaitTime(highAmount, highAmountThreshold, approvalWaitMinutes);

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
        var approvalTime = TransferValidator.CalculateApprovalWaitTime(lowAmount, highAmountThreshold, approvalWaitMinutes);

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
        var result = TransferValidator.ValidateTransferForCompletion(transfer, "98765432109");

        // Assert
        result.IsSuccess.Should().BeTrue();
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
        var result = TransferValidator.ValidateTransferForCompletion(transfer, "98765432109");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Transfer cannot be completed");
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
        var result = TransferValidator.ValidateTransferForCompletion(transfer, "11111111111");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Receiver identity verification failed");
    }

    [Fact]
    public void ValidateDailyLimit_WithinLimit_ShouldNotThrow()
    {
        // Arrange
        var currentTotal = 5000m;
        var newAmount = 2000m;
        var dailyLimit = 10000m;

        // Act
        var result = TransferValidator.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateDailyLimit_ExceedingLimit_ShouldThrow()
    {
        // Arrange
        var currentTotal = 8000m;
        var newAmount = 3000m;
        var dailyLimit = 10000m;

        // Act
        var result = TransferValidator.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Daily transfer limit exceeded");
    }

    [Fact]
    public void ValidateDailyLimit_ExactlyAtLimit_ShouldNotThrow()
    {
        // Arrange
        var currentTotal = 7000m;
        var newAmount = 3000m;
        var dailyLimit = 10000m;

        // Act
        var result = TransferValidator.ValidateDailyLimit(currentTotal, newAmount, dailyLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(RiskLevel.Low, false)]
    [InlineData(RiskLevel.Medium, false)]
    [InlineData(RiskLevel.High, true)]
    [InlineData(null, false)]
    public void ShouldRejectTransfer_WithVariousRiskLevels_ShouldReturnCorrectResult(RiskLevel? riskLevel, bool expected)
    {
        // Act
        var shouldReject = TransferValidator.ShouldRejectTransfer(riskLevel);

        // Assert
        shouldReject.Should().Be(expected);
    }
}
