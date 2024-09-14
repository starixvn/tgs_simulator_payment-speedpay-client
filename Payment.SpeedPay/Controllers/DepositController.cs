using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Payment.SpeedPay.ApplicationServices;
using Payment.SpeedPay.Client;
using Payment.SpeedPay.ViewModels;
using Tgs.ServiceUtility;
using System.Net.Mime;

namespace Payment.SpeedPay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController(ILogger<DepositController> logger) : ControllerBase
{

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes(MediaTypeNames.Application.FormUrlEncoded)]
    public async Task<IActionResult> DepositCallbackAsync(
        [FromServices]IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromServices] NotifyHandler notifyHandler,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromForm] NotifyViewModel source)
    {
        var content = notifyHandler.ProduceCallbackViewModel(source);
        var formId = Guid.Parse(content.OrderNo);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogWarning("Can't found deposit form({id}).", formId);
            return Problem($"Can't found deposit form({formId}).");
        }

        if (!notifyHandler.IsValidSign(source, depositForm!.ApplyInfo!.Settings.Key))
            return Problem("Invalid Sign");

        var realAmount = depositForm.ApplyInfo.Response.Amount;

        var paymentReturn = new SpeedPayDepositReturn
        {
            FormId = formId,
            OrderAmount = depositForm.Amount,
            RealAmount = realAmount,
            Transaction = content.GbpOrderNo,
            Status = content.Status,
            ExtendData = JsonSerializer.Serialize(content),
            RawResponse = JsonSerializer.Serialize(source)
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (content.Status)
        {
            case "SUCCESS":
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            SpeedPayUtility.SerializerOptions),
                        actualAmount: paymentReturn.RealAmount,
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.RealAmount,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    paymentReturn.Transaction,
                    content.Status);
                return Problem("Error when save form");

            case "FAIL":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            SpeedPayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
        }

        return Ok("OK");
    }
}
