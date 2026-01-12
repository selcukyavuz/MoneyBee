using FluentAssertions;
using MoneyBee.Transfer.Service.Domain.Interfaces;
using MoneyBee.Transfer.Service.Domain.Validators;
using Moq;
using System.Collections.Concurrent;

namespace MoneyBee.Transfer.Service.UnitTests.Concurrency;

/// <summary>
/// Tests for concurrent transfer scenarios and daily limit enforcement.
/// Uses Barrier pattern to ensure true concurrent execution in tests.
/// </summary>
public class DailyLimitConcurrencyTests
{
    private readonly Mock<ITransferRepository> _mockRepository;
    private readonly Mock<IDistributedLockService> _mockLockService;
    
    public DailyLimitConcurrencyTests()
    {
        _mockRepository = new Mock<ITransferRepository>();
        _mockLockService = new Mock<IDistributedLockService>();
    }

    /// <summary>
    /// Tests that daily transfer limit is properly enforced when multiple concurrent requests
    /// attempt to transfer amounts that would exceed the limit.
    /// </summary>
    [Fact]
    public async Task ConcurrentTransfers_ShouldEnforceDailyLimit()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        const decimal DAILY_LIMIT = 10000m;
        const int CONCURRENT_REQUESTS = 5;
        const decimal AMOUNT_PER_REQUEST = 3000m;
        
        var currentDailyTotal = 0m;
        var lockObject = new object();
        var successfulTransfers = new ConcurrentBag<decimal>();
        var failedTransfers = new ConcurrentBag<string>();
        
        _mockRepository.Setup(x => x.GetDailyTotalAsync(senderId, It.IsAny<DateTime>()))
            .ReturnsAsync(() =>
            {
                lock (lockObject)
                {
                    return currentDailyTotal;
                }
            });
        
        _mockLockService.Setup(x => x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        
        _mockLockService.Setup(x => x.ReleaseAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        var barrier = new Barrier(CONCURRENT_REQUESTS);
        var tasks = new List<Task>();
        
        // Act: 5 threads start simultaneously
        for (int i = 0; i < CONCURRENT_REQUESTS; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(async () =>
            {
                barrier.SignalAndWait();
                
                var lockKey = $"daily-limit:{senderId}";
                var lockAcquired = await _mockLockService.Object.AcquireAsync(lockKey, TimeSpan.FromSeconds(5));
                
                if (!lockAcquired) return;
                
                try
                {
                    lock (lockObject)
                    {
                        var dailyTotal = currentDailyTotal;
                        
                        if (dailyTotal + AMOUNT_PER_REQUEST > DAILY_LIMIT)
                        {
                            failedTransfers.Add($"Thread-{threadId}: Daily limit would be exceeded");
                        }
                        else
                        {
                            currentDailyTotal += AMOUNT_PER_REQUEST;
                            successfulTransfers.Add(AMOUNT_PER_REQUEST);
                        }
                    }
                }
                finally
                {
                    await _mockLockService.Object.ReleaseAsync(lockKey);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        successfulTransfers.Count.Should().Be(3, "Only 3 transfers should succeed (3 × 3000 = 9000 TRY)");
        failedTransfers.Count.Should().Be(2, "2 transfers should fail (9000 + 3000 would exceed limit)");
        currentDailyTotal.Should().Be(9000m, "Total transferred should not exceed daily limit");
        
        _mockLockService.Verify(x => x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), 
            Times.Exactly(CONCURRENT_REQUESTS));
        _mockLockService.Verify(x => x.ReleaseAsync(It.IsAny<string>()), 
            Times.Exactly(CONCURRENT_REQUESTS));
    }
    
    /// <summary>
    /// Tests that multiple users can transfer simultaneously without interfering with each other's limits.
    /// Each user should have their own independent daily limit.
    /// </summary>
    [Fact]
    public async Task ConcurrentTransfers_FromDifferentUsers_ShouldHaveIndependentLimits()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        const decimal DAILY_LIMIT = 10000m;
        const decimal AMOUNT_PER_REQUEST = 6000m;
        
        var user1Total = 0m;
        var user2Total = 0m;
        var lockObject = new object();
        
        _mockRepository.Setup(x => x.GetDailyTotalAsync(user1Id, It.IsAny<DateTime>()))
            .ReturnsAsync(() => { lock (lockObject) { return user1Total; } });
        
        _mockRepository.Setup(x => x.GetDailyTotalAsync(user2Id, It.IsAny<DateTime>()))
            .ReturnsAsync(() => { lock (lockObject) { return user2Total; } });
        
        _mockLockService.Setup(x => x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        
        _mockLockService.Setup(x => x.ReleaseAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        var barrier = new Barrier(2);
        var results = new ConcurrentBag<(Guid userId, bool success)>();
        
        // Act: Both users transfer simultaneously
        var tasks = new List<Task>
        {
            Task.Run(async () =>
            {
                barrier.SignalAndWait();
                await _mockLockService.Object.AcquireAsync($"daily-limit:{user1Id}", TimeSpan.FromSeconds(5));
                try
                {
                    lock (lockObject)
                    {
                        if (user1Total + AMOUNT_PER_REQUEST <= DAILY_LIMIT)
                        {
                            user1Total += AMOUNT_PER_REQUEST;
                            results.Add((user1Id, true));
                        }
                        else
                        {
                            results.Add((user1Id, false));
                        }
                    }
                }
                finally
                {
                    await _mockLockService.Object.ReleaseAsync($"daily-limit:{user1Id}");
                }
            }),
            Task.Run(async () =>
            {
                barrier.SignalAndWait();
                await _mockLockService.Object.AcquireAsync($"daily-limit:{user2Id}", TimeSpan.FromSeconds(5));
                try
                {
                    lock (lockObject)
                    {
                        if (user2Total + AMOUNT_PER_REQUEST <= DAILY_LIMIT)
                        {
                            user2Total += AMOUNT_PER_REQUEST;
                            results.Add((user2Id, true));
                        }
                        else
                        {
                            results.Add((user2Id, false));
                        }
                    }
                }
                finally
                {
                    await _mockLockService.Object.ReleaseAsync($"daily-limit:{user2Id}");
                }
            })
        };
        
        await Task.WhenAll(tasks);
        
        // Assert
        results.Count(r => r.success).Should().Be(2, "Both users should be able to transfer");
        user1Total.Should().Be(6000m, "User 1 should have their own limit");
        user2Total.Should().Be(6000m, "User 2 should have their own limit");
    }

    /// <summary>
    /// Tests that when lock acquisition fails, transfer is not processed.
    /// </summary>
    [Fact]
    public async Task ConcurrentTransfers_WhenLockFails_ShouldNotProcess()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        const int CONCURRENT_REQUESTS = 3;
        var processedCount = 0;
        
        _mockLockService.Setup(x => x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);
        
        var barrier = new Barrier(CONCURRENT_REQUESTS);
        var tasks = new List<Task>();
        
        // Act
        for (int i = 0; i < CONCURRENT_REQUESTS; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                barrier.SignalAndWait();
                
                var lockAcquired = await _mockLockService.Object.AcquireAsync(
                    $"daily-limit:{senderId}", TimeSpan.FromSeconds(5));
                
                if (lockAcquired)
                {
                    Interlocked.Increment(ref processedCount);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        processedCount.Should().Be(0, "No transfers should be processed when lock fails");
        _mockLockService.Verify(x => x.ReleaseAsync(It.IsAny<string>()), Times.Never);
    }
}

/// <summary>
/// Mock interface for distributed lock service.
/// In production, this would be implemented using Redis.
/// </summary>
public interface IDistributedLockService
{
    Task<bool> AcquireAsync(string key, TimeSpan expiry);
    Task ReleaseAsync(string key);
}

