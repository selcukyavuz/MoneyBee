using MoneyBee.Common.Enums;
using MoneyBee.Customer.Service.Domain.Specifications;
using MoneyBee.Transfer.Service.Domain.Specifications;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Examples;

/// <summary>
/// Examples demonstrating how to use Specification Pattern in practice
/// </summary>
public class SpecificationUsageExamples
{
    // ==================== CUSTOMER SPECIFICATIONS ====================

    /// <summary>
    /// Example 1: Find all active customers
    /// </summary>
    public async Task FindActiveCustomersExample(ICustomerRepository repository)
    {
        var spec = new ActiveCustomerSpecification();
        var activeCustomers = await repository.FindAsync(spec);
        
        // SQL: SELECT * FROM Customers WHERE Status = 'Active'
    }

    /// <summary>
    /// Example 2: Find active AND KYC verified customers
    /// </summary>
    public async Task FindActiveAndVerifiedCustomersExample(ICustomerRepository repository)
    {
        var activeSpec = new ActiveCustomerSpecification();
        var kycSpec = new KycVerifiedCustomerSpecification();
        
        // Combine with AND operator (&)
        var combinedSpec = activeSpec & kycSpec;
        var customers = await repository.FindAsync(combinedSpec);
        
        // SQL: SELECT * FROM Customers 
        //      WHERE Status = 'Active' AND KycVerified = true
    }

    /// <summary>
    /// Example 3: Find active, verified, corporate customers
    /// </summary>
    public async Task FindPremiumCustomersExample(ICustomerRepository repository)
    {
        var spec = new ActiveCustomerSpecification()
                 & new KycVerifiedCustomerSpecification()
                 & new CustomerByTypeSpecification(CustomerType.Corporate);
        
        var premiumCustomers = await repository.FindAsync(spec);
        
        // SQL: SELECT * FROM Customers 
        //      WHERE Status = 'Active' 
        //        AND KycVerified = true 
        //        AND CustomerType = 'Corporate'
    }

    /// <summary>
    /// Example 4: Find customers needing KYC retry (used in background service)
    /// </summary>
    public async Task FindCustomersNeedingKycRetryExample(ICustomerRepository repository)
    {
        // Find customers created more than 24 hours ago who are still unverified
        var spec = new UnverifiedKycCustomerSpecification(hoursThreshold: 24);
        var customersToRetry = await repository.FindAsync(spec);
        
        foreach (var customer in customersToRetry)
        {
            // Retry KYC verification...
        }
    }

    /// <summary>
    /// Example 5: Complex query - (Active AND Verified) OR Corporate
    /// </summary>
    public async Task ComplexCustomerQueryExample(ICustomerRepository repository)
    {
        var activeAndVerified = new ActiveCustomerSpecification() 
                              & new KycVerifiedCustomerSpecification();
        var corporate = new CustomerByTypeSpecification(CustomerType.Corporate);
        
        // Use OR operator (|)
        var spec = activeAndVerified | corporate;
        var customers = await repository.FindAsync(spec);
        
        // SQL: SELECT * FROM Customers 
        //      WHERE (Status = 'Active' AND KycVerified = true) 
        //         OR CustomerType = 'Corporate'
    }

    // ==================== TRANSFER SPECIFICATIONS ====================

    /// <summary>
    /// Example 6: Find pending transfers
    /// </summary>
    public async Task FindPendingTransfersExample(ITransferRepository repository)
    {
        var spec = new PendingTransferSpecification();
        var pendingTransfers = await repository.FindAsync(spec);
        
        // SQL: SELECT * FROM Transfers WHERE Status = 'Pending'
    }

    /// <summary>
    /// Example 7: Find high-value pending transfers (risk monitoring)
    /// </summary>
    public async Task FindHighValuePendingTransfersExample(ITransferRepository repository)
    {
        var highValueSpec = new HighValueTransferSpecification(threshold: 1000m);
        var pendingSpec = new PendingTransferSpecification();
        
        var criticalTransfers = await repository.FindAsync(highValueSpec & pendingSpec);
        
        // SQL: SELECT * FROM Transfers 
        //      WHERE AmountInTRY > 1000 AND Status = 'Pending'
        
        // Use case: Alert admin for manual review
    }

    /// <summary>
    /// Example 8: Find today's transfers for a customer (daily limit check)
    /// </summary>
    public async Task CheckCustomerDailyTransfersExample(
        ITransferRepository repository, 
        Guid customerId)
    {
        var spec = new CustomerDailyTransferSpecification(customerId, DateTime.Today);
        var todaysTransfers = await repository.FindAsync(spec);
        
        var totalToday = todaysTransfers.Sum(t => t.AmountInTRY);
        var remainingLimit = 10000m - totalToday;
        
        // SQL: SELECT * FROM Transfers 
        //      WHERE SenderId = @customerId 
        //        AND CreatedAt >= @today 
        //        AND Status IN ('Pending', 'Completed')
    }

    /// <summary>
    /// Example 9: Find suspicious high-value transfers from yesterday
    /// </summary>
    public async Task FindSuspiciousTransfersExample(ITransferRepository repository)
    {
        var highValueSpec = new HighValueTransferSpecification(threshold: 5000m);
        
        // We can add more complex conditions by creating custom specifications
        var suspiciousTransfers = await repository.FindAsync(highValueSpec);
        
        var yesterday = suspiciousTransfers.Where(t => 
            t.CreatedAt >= DateTime.Today.AddDays(-1) && 
            t.CreatedAt < DateTime.Today);
        
        // Use case: Generate daily risk report
    }

    // ==================== BUSINESS LOGIC WITH SPECIFICATIONS ====================

    /// <summary>
    /// Example 10: Real business logic - Check if customer can send transfer
    /// </summary>
    public async Task<bool> CanCustomerSendTransferExample(
        ICustomerRepository repository,
        string nationalId)
    {
        // Find customer
        var customer = await repository.GetByNationalIdAsync(nationalId);
        if (customer == null)
            return false;

        // Use specifications to check eligibility
        var activeSpec = new ActiveCustomerSpecification();
        var kycVerifiedSpec = new KycVerifiedCustomerSpecification();
        
        var eligibilitySpec = activeSpec & kycVerifiedSpec;
        
        // Check if customer satisfies the specification
        return eligibilitySpec.IsSatisfiedBy(customer);
    }

    /// <summary>
    /// Example 11: Find eligible customers for promotional campaign
    /// </summary>
    public async Task FindPromotionEligibleCustomersExample(ICustomerRepository repository)
    {
        // Campaign rules: Active, KYC verified, individual customers
        var spec = new ActiveCustomerSpecification()
                 & new KycVerifiedCustomerSpecification()
                 & new CustomerByTypeSpecification(CustomerType.Individual);
        
        var eligibleCustomers = await repository.FindAsync(spec);
        
        // Send promotional emails to eligible customers
        foreach (var customer in eligibleCustomers)
        {
            // SendPromotionalEmail(customer.Email);
        }
    }

    /// <summary>
    /// Example 12: Audit - Find all high-value transfers in date range
    /// </summary>
    public async Task AuditHighValueTransfersExample(
        ITransferRepository repository,
        DateTime startDate,
        DateTime endDate)
    {
        var highValueSpec = new HighValueTransferSpecification(threshold: 10000m);
        var allHighValue = await repository.FindAsync(highValueSpec);
        
        // Filter by date range (could also be a specification)
        var auditTransfers = allHighValue
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.AmountInTRY);
        
        // Generate audit report
    }

    /// <summary>
    /// Example 13: Combining NOT operator
    /// </summary>
    public async Task FindUnverifiedActiveCustomersExample(ICustomerRepository repository)
    {
        var activeSpec = new ActiveCustomerSpecification();
        var kycVerifiedSpec = new KycVerifiedCustomerSpecification();
        
        // NOT operator (!)
        var notVerifiedSpec = !kycVerifiedSpec;
        
        var spec = activeSpec & notVerifiedSpec;
        var customers = await repository.FindAsync(spec);
        
        // SQL: SELECT * FROM Customers 
        //      WHERE Status = 'Active' AND KycVerified = false
        
        // Use case: Send KYC reminder emails
    }
}

// ==================== FAKE REPOSITORIES FOR DEMO ====================
public interface ICustomerRepository
{
    Task<CustomerEntity?> GetByNationalIdAsync(string nationalId);
    Task<IEnumerable<CustomerEntity>> FindAsync(ISpecification<CustomerEntity> specification);
}

public interface ITransferRepository
{
    Task<IEnumerable<TransferEntity>> FindAsync(ISpecification<TransferEntity> specification);
}

public interface ISpecification<T>
{
    System.Linq.Expressions.Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
}
