using System.Linq.Expressions;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Specifications;

public class CustomerByTypeSpecification : Specification<CustomerEntity>
{
    private readonly CustomerType _customerType;

    public CustomerByTypeSpecification(CustomerType customerType)
    {
        _customerType = customerType;
    }

    public override Expression<Func<CustomerEntity, bool>> ToExpression()
    {
        return customer => customer.CustomerType == _customerType;
    }
}
