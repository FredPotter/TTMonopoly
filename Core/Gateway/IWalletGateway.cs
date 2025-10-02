using Core.Wallets;

namespace Core.Gateway;

public interface IWalletGateway
{
    Task<List<Wallet>> GetWallets(CancellationToken cancellationToken = default);
    Task<List<Guid>> GetWalletIds(CancellationToken cancellationToken = default);
    Task<Wallet?> FindWalletByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SaveWalletAsync(Wallet wallet, bool rowVersionSupport = false, CancellationToken cancellationToken = default);
}