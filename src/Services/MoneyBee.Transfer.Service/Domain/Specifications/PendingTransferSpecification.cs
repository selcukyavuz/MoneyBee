using System.Linq.Expressions;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Specifications;

public class PendingTransferSpecification : Specification<TransferEntity>
{
    public override Expression<Func<TransferEntity, bool>> ToExpression()
    {
        return transfer => transfer.Status == TransferStatus.Pending;
    }
}
