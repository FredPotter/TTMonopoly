using System.Text.Json;
using Core.Wallets;
using Core.Gateway;

namespace Infrastructure.FileWallets;

public class FileWalletGateway : IWalletGateway
{
    private readonly string _storageFolder;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public FileWalletGateway(string storageFolder)
    {
        _storageFolder = storageFolder;
        if (!Directory.Exists(_storageFolder))
            Directory.CreateDirectory(_storageFolder);
    }

    private string GetFilePath(Guid walletId) => Path.Combine(_storageFolder, $"{walletId}.json");

    public async Task<List<Guid>> GetWalletIds(CancellationToken cancellationToken = default)
    {
        var guids = new List<Guid>();
        var files = Directory.GetFiles(_storageFolder, "*.json");

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (Guid.TryParse(fileName, out var id))
            {
                guids.Add(id);
            }
        }

        return await Task.FromResult(guids);
    }

    public async Task<List<Wallet>> GetWallets(CancellationToken cancellationToken = default)
    {
        var wallets = new List<Wallet>();
        var files = Directory.GetFiles(_storageFolder, "*.json");

        foreach (var file in files)
        {
            await using var stream = new FileStream(
                file,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            var wallet = await JsonSerializer.DeserializeAsync<Wallet>(stream, cancellationToken: cancellationToken);
            if (wallet != null)
                wallets.Add(wallet);
        }

        return wallets;
    }
    
    public async Task<Wallet?> FindWalletByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            return null;

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        var wallet = await JsonSerializer.DeserializeAsync<Wallet>(stream, cancellationToken: cancellationToken);
        return wallet;
    }

    public async Task<bool> SaveWalletAsync(Wallet wallet, bool rowVersionSupport = false, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(wallet.Id);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (rowVersionSupport && File.Exists(filePath))
            {
                var existing = await FindWalletByIdAsync(wallet.Id, cancellationToken);
                if (existing == null)
                    return false;

                if (existing.ConcurrencyToken != wallet.ConcurrencyToken)
                    return false;
                wallet.ConcurrencyToken = Guid.NewGuid();
            }
            else if (rowVersionSupport)
            {
                wallet.ConcurrencyToken = Guid.NewGuid();
            }
            
            await using var stream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);

            await JsonSerializer.SerializeAsync(stream, wallet, cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);

            return true;
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
