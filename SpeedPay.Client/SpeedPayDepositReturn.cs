namespace Payment.SpeedPay.Client;
public record SpeedPayDepositReturn
{
    public string Transaction { get; set; } = default!;

    public decimal OrderAmount { get; set; }

    public decimal RealAmount { get; set; }

    public Guid FormId { get; set; }

    public string Status { get; set; } = default!;

    public string SenderBankName { get; set; } = default!;

    public string SenderBankAccount { get; set; } = default!;

    public string ReceiveAccountName { get; set; } = default!;

    public string ReceiveBankAccount { get; set; } = default!;

    public string ExtendData { get; set; } = default!;

    public string RawResponse { get; set; } = default!;
}
