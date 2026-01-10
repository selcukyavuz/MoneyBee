using System.Linq.Expressions;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Specifications;

public class ActiveCustomerSpecification : Specification<CustomerEntity>
{
    public override Expression<Func<CustomerEntity, bool>> ToExpression()
    {
        return customer => customer.Status == CustomerStatus.Active;
    }
}
