namespace MoneyBee.Common.Enums;

public enum CustomerType
{
    Individual = 1,
    Corporate = 2
}

public enum CustomerStatus
{
    Active = 1,
    Passive = 2,
    Blocked = 3
}

public enum TransferStatus
{
    Pending = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum Currency
{
    TRY = 1,
    USD = 2,
    EUR = 3,
    GBP = 4
}
