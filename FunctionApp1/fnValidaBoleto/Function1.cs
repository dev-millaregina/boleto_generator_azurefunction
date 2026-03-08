using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fnValidaBoleto;
public class Function1
{
    private readonly ILogger<Function1> _logger;

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function("barcode-validade")]

    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", "options")] HttpRequest req)
    {
        req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.Method == "OPTIONS")
        {
            req.HttpContext.Response.StatusCode = 200;
            return new OkResult();
        }
    
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        string barcodeData = data?.barcode;

        if (string.IsNullOrEmpty(barcodeData))
        {
            return new BadRequestObjectResult("O campo barcode é obrigatório.");
        }

        if (barcodeData.Length != 44 || !barcodeData.All(char.IsDigit))
        {
            var result = new {
                valido = false, mensagem = "O campo barcode deve conter exatamente 44 dígitos numéricos."
            };
            return new BadRequestObjectResult(result);
        }

        string datePart = barcodeData.Substring(3, 8);
        if (!DateTime.TryParseExact(datePart, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dateObj))
        {
            var result = new {
                valido = false, mensagem = "O campo barcode deve conter uma data válida no formato YYYYMMDD."
            };
            return new BadRequestObjectResult(result);
        }

        var resulOk = new
        {
            valido = true, mensagem = "O campo barcode é válido.", vencimento = dateObj.ToString("dd-MM-yyyy")
        };
        return new OkObjectResult(resulOk);
    }
}