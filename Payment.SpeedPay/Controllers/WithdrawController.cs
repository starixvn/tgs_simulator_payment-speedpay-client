using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Payment.SpeedPay.ApplicationServices;
using Payment.SpeedPay.Client;
using Payment.SpeedPay.ViewModels;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace Payment.SpeedPay.Controllers;

[Route("[controller]")]
[ApiController]
public class WithdrawController(ILogger<WithdrawController> logger) : ControllerBase
{
    [HttpPost("callback", Name = "NotifyWithdraw")]
    [Consumes(MediaTypeNames.Application.FormUrlEncoded)]
    public async Task<IActionResult> WithdrawCallbackAsync(
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromServices] NotifyHandler notifyHandler,
        [FromForm] NotifyViewModel source)
    {
        var content = notifyHandler.ProduceCallbackViewModel(source);
        var formId = Guid.Parse(content.OrderNo);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            logger.LogWarning("Can't found withdraw form({id}).", formId);
            return Problem($"Can't found deposit form({formId}).");
        }

        if (!notifyHandler.IsValidSign(source, withdrawForm!.ApplyInfo!.Settings.Key))
            return Problem("Invalid Sign");

        var amount = decimal.Parse(content.Amount);

        var paymentReturn = new SpeedPayWithdrawReturn()
        {
            FormId = formId,
            ActualAmount = amount,
            ThirdPartyOrderNo = content.GbpOrderNo,
            Status = content.Status,
            ExtendData = JsonSerializer.Serialize(content),
            RawResponse = JsonSerializer.Serialize(source)
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (content.Status)
        {
            case "SUCCESS":
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            SpeedPayUtility.SerializerOptions),
                        actualAmount: amount,
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.PaidWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId).ConfigureAwait(false);
                }

                break;

            case "FAIL":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            SpeedPayUtility.SerializerOptions),
                        actualAmount: decimal.Parse(content.Amount),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    paymentReturn.ThirdPartyOrderNo,
                    paymentReturn.Status);
                return Problem("Error when save form");
        }

        return Ok("success");
    }
}
