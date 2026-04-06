using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;

namespace Order.Application.Abstractions;

public interface IOrderDbContext
{
    DbSet<Order.Domain.Entities.Order> Orders { get; }

    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}