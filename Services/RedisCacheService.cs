using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Sprint2.Interfaces;

namespace Sprint2.Services
{
    /// <summary>
    /// Implementación del servicio de caché que utiliza Redis como almacenamiento distribuido
    /// con un fallback a memoria local para alta disponibilidad y rendimiento.
    /// </summary>
    /// <remarks>
    /// Esta implementación proporciona un enfoque híbrido que combina:
    /// <list type="bullet">
    /// <item><description>Caché en memoria local para acceso rápido</description></item>
    /// <item><description>Caché distribuida con Redis para escalabilidad</description></item>
    /// <item><description>Mecanismo de fallback automático a memoria si Redis no está disponible</description></item>
    /// <item><description>Recuperación automática cuando Redis vuelve a estar disponible</description></item>
    /// <item><description>Timeouts cortos para evitar bloqueos en caso de problemas con Redis</description></item>
    /// </list>
    /// </remarks>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly DistributedCacheEntryOptions _defaultOptions;
        private readonly MemoryCacheEntryOptions _defaultMemoryOptions;
        private static bool _useMemoryCache = false;
        private static readonly SemaphoreSlim _connectionCheckLock = new SemaphoreSlim(1, 1);
        private static DateTime _lastConnectionAttempt = DateTime.MinValue;
        private static readonly TimeSpan _connectionRetryInterval = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="RedisCacheService"/>.
        /// </summary>
        /// <param name="distributedCache">El servicio de caché distribuida (Redis).</param>
        /// <param name="memoryCache">El servicio de caché en memoria local.</param>
        /// <param name="logger">El servicio de registro.</param>
        public RedisCacheService(
            IDistributedCache distributedCache, 
            IMemoryCache memoryCache,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _logger = logger;
            
            _defaultOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            
            _defaultMemoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
        }

        /// <summary>
        /// Recupera un valor de la caché utilizando la clave especificada.
        /// </summary>
        /// <typeparam name="T">El tipo de dato a recuperar.</typeparam>
        /// <param name="key">La clave única que identifica el valor en la caché.</param>
        /// <returns>El valor almacenado en caché, o null si no existe.</returns>
        /// <remarks>
        /// Este método implementa una estrategia de caché en dos niveles:
        /// <list type="number">
        /// <item><description>Primero intenta recuperar el valor de la memoria local para un acceso más rápido</description></item>
        /// <item><description>Si no se encuentra en memoria local, intenta recuperarlo de Redis</description></item>
        /// <item><description>Si se recupera de Redis, también lo almacena en memoria local para futuros accesos</description></item>
        /// <item><description>Si Redis no está disponible, cambia automáticamente a modo memoria</description></item>
        /// </list>
        /// </remarks>
        public async Task<T?> GetAsync<T>(string key)
        {
            // Primero intentamos obtener de la memoria local (es más rápido)
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                return cachedValue;
            }
            
            // Si estamos en modo memoria, no intentamos acceder a Redis
            if (_useMemoryCache)
            {
                return default;
            }
            
            // Verificamos si debemos intentar reconectar a Redis
            await CheckRedisConnectionAsync();
            
            // Si seguimos en modo memoria después de la verificación, retornamos null
            if (_useMemoryCache)
            {
                return default;
            }
            
            try
            {
                // Usamos un timeout corto para la operación de Redis
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var value = await _distributedCache.GetStringAsync(key, cts.Token);
                
                if (value == null)
                {
                    return default;
                }
                
                var result = JsonSerializer.Deserialize<T>(value);
                
                // También almacenamos en memoria local para acceso más rápido
                _memoryCache.Set(key, result, _defaultMemoryOptions);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al acceder a Redis. Cambiando a caché en memoria.");
                _useMemoryCache = true;
                return default;
            }
        }
        
        /// <summary>
        /// Verifica la conexión con Redis y actualiza el estado de la caché.
        /// </summary>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        /// <remarks>
        /// Este método implementa una estrategia inteligente para verificar la disponibilidad de Redis:
        /// <list type="bullet">
        /// <item><description>Utiliza un intervalo de reintento para no verificar constantemente</description></item>
        /// <item><description>Implementa un mecanismo de bloqueo para evitar verificaciones simultáneas</description></item>
        /// <item><description>Utiliza un timeout corto para evitar bloqueos</description></item>
        /// <item><description>Actualiza automáticamente el modo de caché según la disponibilidad de Redis</description></item>
        /// </list>
        /// </remarks>
        private async Task CheckRedisConnectionAsync()
        {
            // Si ya estamos usando memoria o el último intento fue reciente, no hacemos nada
            if (_useMemoryCache && DateTime.UtcNow - _lastConnectionAttempt < _connectionRetryInterval)
            {
                return;
            }
            
            // Usamos un lock para evitar que múltiples hilos intenten verificar la conexión simultáneamente
            if (await _connectionCheckLock.WaitAsync(0))
            {
                try
                {
                    // Verificamos de nuevo dentro del lock
                    if (_useMemoryCache && DateTime.UtcNow - _lastConnectionAttempt < _connectionRetryInterval)
                    {
                        return;
                    }
                    
                    _lastConnectionAttempt = DateTime.UtcNow;
                    
                    try
                    {
                        // Intentamos una operación simple en Redis con un timeout corto
                        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                        await _distributedCache.GetStringAsync("connection_test", cts.Token);
                        
                        // Si llegamos aquí, Redis está disponible
                        if (_useMemoryCache)
                        {
                            _logger.LogInformation("Conexión a Redis restaurada. Volviendo a usar Redis como caché principal.");
                            _useMemoryCache = false;
                        }
                    }
                    catch
                    {
                        // Si hay un error, seguimos usando memoria
                        _useMemoryCache = true;
                    }
                }
                finally
                {
                    _connectionCheckLock.Release();
                }
            }
        }

        /// <summary>
        /// Almacena un valor en la caché con la clave especificada.
        /// </summary>
        /// <typeparam name="T">El tipo de dato a almacenar.</typeparam>
        /// <param name="key">La clave única para identificar el valor en la caché.</param>
        /// <param name="value">El valor a almacenar en la caché.</param>
        /// <param name="expirationTime">El tiempo de expiración opcional para el valor en caché. Si es null, se utilizará el tiempo predeterminado.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        /// <remarks>
        /// Este método implementa una estrategia de caché en dos niveles:
        /// <list type="number">
        /// <item><description>Siempre almacena el valor en memoria local para un acceso más rápido</description></item>
        /// <item><description>Si Redis está disponible, también almacena el valor en Redis</description></item>
        /// <item><description>Si Redis no está disponible, cambia automáticamente a modo memoria</description></item>
        /// </list>
        /// </remarks>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            // Siempre almacenamos en memoria local para acceso rápido
            var memOptions = expirationTime.HasValue
                ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expirationTime }
                : _defaultMemoryOptions;
            
            _memoryCache.Set(key, value, memOptions);
            
            // Si estamos en modo memoria, no intentamos acceder a Redis
            if (_useMemoryCache)
            {
                return;
            }
            
            try
            {
                // Usamos un timeout corto para la operación de Redis
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                
                var redisOptions = expirationTime.HasValue
                    ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expirationTime }
                    : _defaultOptions;

                var jsonValue = JsonSerializer.Serialize(value);
                await _distributedCache.SetStringAsync(key, jsonValue, redisOptions, cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al acceder a Redis. Cambiando a caché en memoria.");
                _useMemoryCache = true;
            }
        }

        /// <summary>
        /// Elimina un valor de la caché utilizando la clave especificada.
        /// </summary>
        /// <param name="key">La clave única que identifica el valor a eliminar.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        /// <remarks>
        /// Este método implementa una estrategia de eliminación en dos niveles:
        /// <list type="number">
        /// <item><description>Siempre elimina el valor de la memoria local</description></item>
        /// <item><description>Si Redis está disponible, también elimina el valor de Redis</description></item>
        /// <item><description>Si Redis no está disponible, cambia automáticamente a modo memoria</description></item>
        /// </list>
        /// </remarks>
        public async Task RemoveAsync(string key)
        {
            _logger.LogInformation($"Eliminando clave de caché: {key}");
            
            // Verificar si es un patrón con comodín
            if (key.Contains("*"))
            {
                // Para patrones con comodín, eliminamos todas las claves que coincidan del caché en memoria
                var pattern = key.Replace("*", "");
                var memoryKeys = _memoryCache.GetType()
                    .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_memoryCache) as System.Collections.ICollection;
                
                if (memoryKeys != null)
                {
                    var keysToRemove = new List<string>();
                    
                    foreach (var cacheItem in memoryKeys)
                    {
                        var cacheItemKey = cacheItem.GetType().GetProperty("Key")?.GetValue(cacheItem)?.ToString();
                        if (cacheItemKey != null && cacheItemKey.StartsWith(pattern))
                        {
                            keysToRemove.Add(cacheItemKey);
                        }
                    }
                    
                    _logger.LogInformation($"Eliminando {keysToRemove.Count} claves que coinciden con el patrón '{pattern}*'");
                    
                    foreach (var keyToRemove in keysToRemove)
                    {
                        _memoryCache.Remove(keyToRemove);
                    }
                }
                
                // Si estamos en modo memoria, no intentamos acceder a Redis
                if (_useMemoryCache)
                {
                    return;
                }
                
                // Para Redis, no podemos usar directamente patrones en RemoveAsync
                // En una implementación real, usaríamos SCAN + DEL para eliminar claves por patrón
                // Pero para simplificar, solo registramos que se debería implementar
                _logger.LogWarning($"La eliminación de claves por patrón '{key}' en Redis no está implementada completamente");
                
                return;
            }
            
            // Para claves simples, eliminamos directamente
            _memoryCache.Remove(key);
            
            // Si estamos en modo memoria, no intentamos acceder a Redis
            if (_useMemoryCache)
            {
                return;
            }
            
            try
            {
                // Usamos un timeout corto para la operación de Redis
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await _distributedCache.RemoveAsync(key, cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al acceder a Redis. Cambiando a caché en memoria.");
                _useMemoryCache = true;
            }
        }
    }
}
