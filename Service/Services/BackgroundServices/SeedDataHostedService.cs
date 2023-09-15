using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Nest;
using Service.Hubs;
using Service.Models;

namespace Service.Services.BackgroundServices
{
    public class SeedDataHostedService : BackgroundService
    {
        private readonly Random _random = new();
        private readonly ElasticsearchService _elasticsearchService;
        private readonly IHubContext<SaelHub> _hubContext;
        private const string saleIndex = "sale-index";

        public SeedDataHostedService(IElasticClient client, IHubContext<SaelHub> hubContext)
        {
            _elasticsearchService = new ElasticsearchService(client, saleIndex);
            _hubContext = hubContext;
            _elasticsearchService.CreateIndexIfNotExistsAsync<Sale>(saleIndex).Wait();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var sale = await GenerateRandomSaleAsync();

                // Elasticsearch'e Sale nesnesini ekleyin
                var response = await _elasticsearchService.AddOrUpdateAsync(sale);

                if (response)
                {
                    Console.WriteLine($"Sale added");
                }

                var elasticSale = await _elasticsearchService.GetLastAddedAsync<Sale>();
                if (elasticSale != null)
                {
                    await _hubContext.Clients.All.SendAsync("updateMarker", elasticSale.Latitude, elasticSale.Longitude, elasticSale.Name);
                }

                // 1 saniyede bir çalışmasını sağlamak için bekleme
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        private async Task<Sale> GenerateRandomSaleAsync()
        {
            int id = (await _elasticsearchService.GetLastAddedAsync<Sale>())?.Id + 1 ?? 1;
            // Rastgele bir Sale nesnesi oluşturun
            var sale = new Sale
            {
                Id = id,
                Name = GenerateRandomString(10),
                Description = GenerateRandomString(50),
                Amount = (decimal)_random.NextDouble(),
                Latitude = (_random.Next(-90, 91) + _random.NextDouble()) * (_random.Next(0, 2) == 0 ? -1 : 1), // -90 ile 90 aralığında
                Longitude = (_random.Next(-180, 181) + _random.NextDouble()) * (_random.Next(0, 2) == 0 ? -1 : 1), // -180 ile 180 aralığında
                CreatedDate = DateTime.UtcNow
            };
            return sale;
        }



        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)])
                .ToArray());
        }
    }
}
