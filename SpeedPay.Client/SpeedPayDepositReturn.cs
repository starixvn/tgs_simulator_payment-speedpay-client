namespace Payment.SpeedPay.Client;
public record SpeedPayDepositReturn
{
    public string Transaction { get; set; } = default!;

    public decimal OrderAmount { get; set; }

    public decimal RealAmount { get; set; }

    public Guid FormId { get; set; }

    public string StatusCode { get; set; } = default!;

    public string StatusMessage { get; set; } = default!;

    public string Key { get; set; } = default!;

    public string ExtendData { get; set; } = default!;
}
