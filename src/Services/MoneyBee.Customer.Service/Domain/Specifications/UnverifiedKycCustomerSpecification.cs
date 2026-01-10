using System.Linq.Expressions;
using MoneyBee.Common.DDD;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Specifications;

public class UnverifiedKycCustomerSpecification : Specification<CustomerEntity>
{
    private readonly int _hoursThreshold;

    public UnverifiedKycCustomerSpecification(int hoursThreshold)
    {
        _hoursThreshold = hoursThreshold;
    }

    public override Expression<Func<CustomerEntity, bool>> ToExpression()
    {
        var thresholdDate = DateTime.UtcNow.AddHours(-_hoursThreshold);
        return customer => !customer.KycVerified && customer.CreatedAt < thresholdDate;
    }
}
