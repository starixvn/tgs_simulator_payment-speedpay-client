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
public class DepositController(ILogger<DepositController> logger, TgsLogHandler logHandler) : ControllerBase
{

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes(MediaTypeNames.Application.FormUrlEncoded)]
    public async Task<IActionResult> DepositCallbackAsync(
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromServices] NotifyHandler notifyHandler,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromForm] NotifyViewModel source)
    {
        var content = notifyHandler.ProduceCallbackViewModel(source);
        var response = await HandleDepositRequestAsync(logger, getService, notifyHandler, paymentServiceClient, paymentClient, source, content).ConfigureAwait(false);      
        
        logHandler.LogApi(HttpContext, content, response);
        
        return Ok(response);
    }

    private async Task<string> HandleDepositRequestAsync(
        ILogger<DepositController> logger, 
        IGetService<GetDepositFormQuery, DepositForm?> getService, 
        NotifyHandler notifyHandler, IPaymentServiceClient paymentServiceClient, 
        IPaymentClient paymentClient, 
        NotifyViewModel source, 
        CallbackViewModel content)
    {
        var formId = Guid.Parse(content.OrderNo);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogWarning("Can't found deposit form({Id}).", formId);
            return $"Can't found deposit form({formId}).";
        }

        if (!notifyHandler.IsValidSign(source, depositForm!.ApplyInfo!.Settings.Key))
            return "Invalid Sign";

        var realAmount = depositForm.ApplyInfo.Response.Amount;

        var paymentReturn = new SpeedPayDepositReturn
        {
            FormId = formId,
            OrderAmount = depositForm.Amount,
            RealAmount = realAmount,
            Transaction = content.GbpOrderNo,
            Status = content.Status,
            ReceiveAccountName = "SpeedPay",
            ReceiveBankAccount = depositForm.ApplyInfo.Settings.MerchantNo,
            SenderBankAccount = content.BankCode ?? "",
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
                    logger.LogInformation("Approve deposit form ({Id}).", formId);
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

            case "FAIL":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);
                    logger.LogInformation("Reject deposit form ({Id}).", formId);
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
            default:
                logger.LogWarning("FormId:{FormId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{Status}",
                    formId,
                    paymentReturn.Transaction,
                    content.Status);
                return "Error when save form";
        }

        return "OK";
    }
}
