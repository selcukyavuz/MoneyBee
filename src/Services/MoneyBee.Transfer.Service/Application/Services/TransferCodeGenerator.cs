using MoneyBee.Transfer.Service.Domain.Interfaces;
using MoneyBee.Transfer.Service.Helpers;

namespace MoneyBee.Transfer.Service.Application.Services;

public class TransferCodeGenerator(ITransferRepository repository)
{
    public async Task<string> GenerateUniqueCodeAsync()
    {
        string code;
        bool exists;

        do
        {
            code = TransactionCodeGenerator.Generate();
            exists = await repository.TransactionCodeExistsAsync(code);
        }
        while (exists);

        return code;
    }
}
