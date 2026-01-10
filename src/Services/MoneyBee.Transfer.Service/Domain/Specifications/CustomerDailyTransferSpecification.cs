using System.Linq.Expressions;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Specifications;

public class CustomerDailyTransferSpecification : Specification<TransferEntity>
{
    private readonly Guid _customerId;
    private readonly DateTime _date;

    public CustomerDailyTransferSpecification(Guid customerId, DateTime date)
    {
        _customerId = customerId;
        _date = date.Date;
    }

    public override Expression<Func<TransferEntity, bool>> ToExpression()
    {
        return transfer => transfer.SenderId == _customerId &&
                          transfer.CreatedAt >= _date &&
                          (transfer.Status == TransferStatus.Pending || transfer.Status == TransferStatus.Completed);
    }
}
