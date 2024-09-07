namespace Payment.SpeedPay;
public class SpeedPayOptions
{
    public required Uri CallbackBaseUrl { get; set; }

    public required Uri PaymentApiUrl { get; set; }


    public required Uri PaymentServiceBaseUrl { get; set; }
}
