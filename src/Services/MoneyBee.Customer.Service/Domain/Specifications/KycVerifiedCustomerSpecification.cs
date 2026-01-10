using System.Linq.Expressions;
using MoneyBee.Common.DDD;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Specifications;

public class KycVerifiedCustomerSpecification : Specification<CustomerEntity>
{
    public override Expression<Func<CustomerEntity, bool>> ToExpression()
    {
        return customer => customer.KycVerified;
    }
}
