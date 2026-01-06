namespace CatalogOrderApi.Services;

public interface IConcurrencyService
{
    Task<T> ExecuteWithSemaphoreAsync<T>(string resourceKey, Func<Task<T>> operation);
    Task ExecuteWithSemaphoreAsync(string resourceKey, Func<Task> operation);
}

public class SemaphoreConcurrencyService : IConcurrencyService
{
    private readonly Dictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly SemaphoreSlim _semaphoreCreationLock = new(1, 1);
    private readonly int _maxConcurrentRequests;

    public SemaphoreConcurrencyService(int maxConcurrentRequests = 10)
    {
        _maxConcurrentRequests = maxConcurrentRequests;
    }

    private async Task<SemaphoreSlim> GetOrCreateSemaphoreAsync(string resourceKey)
    {
        await _semaphoreCreationLock.WaitAsync();
        try
        {
            if (!_semaphores.ContainsKey(resourceKey))
            {
                _semaphores[resourceKey] = new SemaphoreSlim(_maxConcurrentRequests, _maxConcurrentRequests);
            }
            return _semaphores[resourceKey];
        }
        finally
        {
            _semaphoreCreationLock.Release();
        }
    }

    public async Task<T> ExecuteWithSemaphoreAsync<T>(string resourceKey, Func<Task<T>> operation)
    {
        var semaphore = await GetOrCreateSemaphoreAsync(resourceKey);
        await semaphore.WaitAsync();
        try
        {
            return await operation();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task ExecuteWithSemaphoreAsync(string resourceKey, Func<Task> operation)
    {
        var semaphore = await GetOrCreateSemaphoreAsync(resourceKey);
        await semaphore.WaitAsync();
        try
        {
            await operation();
        }
        finally
        {
            semaphore.Release();
        }
    }
}
