using Customers.Web.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Web.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task NewAsync(Customer customer, CancellationToken cancellationToken);
    }
}
