using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Web.Repositories
{
    public interface IInventoryRepository
    {
        Task<int> GetAvailableQuantityAsync(string product, CancellationToken cancellationToken);
        Task NewAsync(Contracts.Inventory inventory, CancellationToken cancellationToken);
        Task UpdateAsync(string product, int orderedQuantity, CancellationToken cancellationToken);
    }
}
