namespace Payment.SpeedPay;
public record BrandPaymentMerchantInfo(
    string MerchantNo,
    string Version,
    string Key,
    string Timezone);
