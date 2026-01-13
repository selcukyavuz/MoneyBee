namespace MoneyBee.Transfer.Service.Application.Options;

/// <summary>
/// Transaction fee calculation settings
/// </summary>
public class FeeSettings
{
    public decimal BaseFee { get; set; }
    public decimal FeePercentage { get; set; }
}
