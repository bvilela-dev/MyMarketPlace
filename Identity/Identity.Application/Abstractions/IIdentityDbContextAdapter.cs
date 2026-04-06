namespace Identity.Application.Abstractions;

/// <summary>
/// Marks the infrastructure Identity DbContext implementation exposed to the application layer.
/// </summary>
public interface IIdentityDbContextAdapter : IIdentityDbContext;