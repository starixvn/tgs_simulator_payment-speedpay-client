using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Payment.SpeedPay.ApplicationServices;
using Payment.SpeedPay.Client;
using Payment.SpeedPay.ViewModels;
using Tgs.ServiceUtility;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;

namespace Payment.SpeedPay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController(ILogger<DepositController> logger) : ControllerBase
{

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes(MediaTypeNames.Application.FormUrlEncoded)]
    public async Task<string> DepositCallbackAsync(
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
            return "fail";
        }

        if (!notifyHandler.IsValidSign(source, depositForm!.ApplyInfo!.Settings.Key))
            return "fail";

        var paymentReturn = new SpeedPayDepositReturn
        {
            FormId = formId,
            OrderAmount = depositForm.Amount,
            RealAmount = decimal.Parse(content.Amount),
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
                        handlingFee: depositForm.Amount - paymentReturn.RealAmount,
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
                return "fail";

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

        return "success";
    }
}
