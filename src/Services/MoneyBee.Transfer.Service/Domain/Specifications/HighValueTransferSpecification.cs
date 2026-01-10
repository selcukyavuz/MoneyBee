using System.Linq.Expressions;
using MoneyBee.Common.DDD;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;

namespace MoneyBee.Transfer.Service.Domain.Specifications;

public class HighValueTransferSpecification : Specification<TransferEntity>
{
    private readonly decimal _threshold;

    public HighValueTransferSpecification(decimal threshold = 1000m)
    {
        _threshold = threshold;
    }

    public override Expression<Func<TransferEntity, bool>> ToExpression()
    {
        return transfer => transfer.AmountInTRY > _threshold;
    }
}
