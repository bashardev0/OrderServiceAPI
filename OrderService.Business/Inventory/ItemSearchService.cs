using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Responses;
using OrderService.Persistence.Repositories;

namespace OrderService.Business.Inventory
{
    public interface IItemSearchService
    {
        Task<BaseResponse<object>> SearchAsync(string query, CancellationToken ct);
    }

    public sealed class ItemSearchService : IItemSearchService
    {
        private readonly IItemSearchRepository _repo;
        private readonly ILogger<ItemSearchService> _logger;

        public ItemSearchService(IItemSearchRepository repo, ILogger<ItemSearchService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<BaseResponse<object>> SearchAsync(string query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BaseResponse<object>.Fail("Query cannot be empty", 400);

            var json = await _repo.SearchItemsAsync(query, ct);

            try
            {
                var data = JsonSerializer.Deserialize<object>(json);
                return BaseResponse<object>.Ok(data, "Search results");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON returned from repository");
                return BaseResponse<object>.Fail("Invalid JSON from repository", 500);
            }
        }
    }
}
