using System;
using System.Text.Json;
using System.Text.Json.Serialization; // Necessário para [JsonPropertyName]
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace fnGeradorBoletos
{
    public class fnSalvaBoleto
    {
        private readonly ILogger _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public fnSalvaBoleto(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<fnSalvaBoleto>();

            // Puxa a string que você pegou no Portal Azure (aba Keys)
            string cosmosConnection = Environment.GetEnvironmentVariable("CosmosDBConnection");

            _cosmosClient = new CosmosClient(cosmosConnection);
            _container = _cosmosClient.GetContainer("boletosdb", "boletos");
        }

        [Function("fnSalvaBoleto")]
        public async Task Run(
    [ServiceBusTrigger("gerador-codigo-barras", Connection = "ServiceBusConnectionString")] string message)
        {
            try
            {
                _logger.LogInformation($"Processando mensagem: {message}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var boleto = JsonSerializer.Deserialize<Boleto>(message, options);

                if (boleto != null)
                {
                    // Forçamos a criação de um ID caso não exista
                    string idFinal = string.IsNullOrEmpty(boleto.Id) ? Guid.NewGuid().ToString() : boleto.Id;

                    // CRIAMOS UM OBJETO ANÔNIMO PARA GARANTIR OS NOMES DOS CAMPOS
                    // O Cosmos DB lerá "id" exatamente assim.
                    var documentoParaSalvar = new
                    {
                        id = idFinal, // OBRIGATÓRIO ser minúsculo aqui
                        barcode = boleto.Barcode,
                        valorOriginal = boleto.ValorOriginal,
                        dataVencimento = boleto.DataVencimento,
                        imagemBase64 = boleto.ImagemBase64
                    };

                    // Salva o objeto anônimo
                    await _container.CreateItemAsync(documentoParaSalvar, new PartitionKey(idFinal));

                    _logger.LogInformation($"Boleto salvo com sucesso no Cosmos! ID: {idFinal}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao persistir no Cosmos DB: {ex.Message}");
            }
        }
    }

    public class Boleto
    {
        [JsonPropertyName("id")] // Isso força o Cosmos a entender que este é o ID único
        public string Id { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; }

        [JsonPropertyName("valorOriginal")]
        public decimal ValorOriginal { get; set; }

        [JsonPropertyName("dataVencimento")]
        public DateTime DataVencimento { get; set; }

        [JsonPropertyName("imagemBase64")]
        public string ImagemBase64 { get; set; }
    }
}