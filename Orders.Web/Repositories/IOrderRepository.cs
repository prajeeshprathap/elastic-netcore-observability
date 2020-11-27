using Orders.Web.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Web.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> GetyByIdAsync(Guid id, CancellationToken cancellationToken);
        Task NewAsync(Order order, CancellationToken cancellationToken);
        Task ProcessOrderAsync(Guid id, CancellationToken cancellationToken);
        Task CancelOrderAsync(Guid id, CancellationToken cancellationToken);
    }
}
