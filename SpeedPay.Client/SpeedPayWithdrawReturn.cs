namespace Payment.SpeedPay.Client;
public record SpeedPayWithdrawReturn
{
    public string ThirdPartyOrderNo { get; set; } = default!;

    public decimal ActualAmount { get; set; }

    public Guid FormId { get; set; }

    public string Status { get; set; } = default!;

    public string ExtendData { get; set; } = default!;
    public string RawResponse { get; set; } = default!;
}
