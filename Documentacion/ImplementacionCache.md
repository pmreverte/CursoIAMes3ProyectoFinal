# Implementación de Caché en la Aplicación

## Resumen

Se ha implementado un sistema de caché distribuida utilizando Redis para mejorar el rendimiento de la aplicación, reduciendo la carga en la base de datos y mejorando los tiempos de respuesta para datos frecuentemente accedidos.

## Componentes Implementados

### 1. Dependencias Añadidas

Se ha añadido la dependencia de Redis para .NET en el archivo `Sprint2.csproj`:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.0.2" />
```

### 2. Configuración de Redis

Se ha configurado Redis en el archivo `appsettings.json` añadiendo la cadena de conexión:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskApp;Trusted_Connection=True;MultipleActiveResultSets=true",
  "Redis": "localhost:6379"
}
```

Y se ha registrado el servicio de Redis en `Program.cs`:

```csharp
// Configurar Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "Sprint2_";
});
```

### 3. Interfaz de Caché

Se ha creado una interfaz `ICacheService` en `Interfaces/ICacheService.cs` para abstraer las operaciones de caché:

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null);
    Task RemoveAsync(string key);
}
```

### 4. Implementación de Caché con Redis y Fallback a Memoria Optimizado

Se ha implementado el servicio de caché en `Services/RedisCacheService.cs` utilizando un enfoque híbrido que combina `IMemoryCache` local para acceso rápido y `IDistributedCache` (Redis) para distribución, con mecanismos de fallback y recuperación automática:

```csharp
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

    // Constructor y configuración...

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
        
        // Intentamos obtener de Redis con un timeout corto
        try
        {
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

    // Otros métodos implementados con el mismo patrón optimizado
}
```

Esta implementación proporciona alta disponibilidad y rendimiento optimizado:

1. **Acceso en dos niveles**: Primero verifica la memoria local (acceso inmediato) antes de intentar Redis
2. **Timeouts cortos**: Limita el tiempo de espera para operaciones de Redis a 1 segundo para evitar bloqueos
3. **Verificación inteligente de conexión**: Comprueba la disponibilidad de Redis periódicamente sin afectar el rendimiento
4. **Recuperación automática**: Intenta reconectarse a Redis cada cierto tiempo si estaba caído
5. **Estado compartido**: Usa variables estáticas para que todas las instancias del servicio compartan el estado de la conexión

### 5. Integración en el Servicio de Tareas

Se ha modificado `TaskService` para utilizar el caché en las operaciones más frecuentes:

- **GetTaskById**: Ahora primero busca la tarea en el caché y solo accede a la base de datos si no está en caché.
- **GetTaskList**: Implementa caché para listas de tareas con claves basadas en los parámetros de filtrado y paginación.
- **GetCategories**: Ahora almacena en caché la lista de categorías para reducir consultas a la base de datos.
- **CreateTask/UpdateTask/DeleteTask**: Invalidan el caché cuando se modifican datos para mantener la consistencia.

La implementación de caché para `GetTaskList` es especialmente importante ya que es una de las operaciones más utilizadas:

```csharp
public TaskListViewModel GetTaskList(TaskFilter filter, int page, int pageSize)
{
    // Generamos una clave de caché basada en los parámetros
    string cacheKey = $"tasklist_{filter?.Status}_{filter?.Priority}_{filter?.Category}_{filter?.SearchTerm}_{page}_{pageSize}";
    
    // Intentamos obtener del caché
    var cachedResult = _cacheService.GetAsync<TaskListViewModel>(cacheKey).Result;
    if (cachedResult != null)
    {
        return cachedResult;
    }
    
    // Si no está en caché, obtenemos de la base de datos
    var (tasks, totalCount) = _taskRepository.GetAll(filter, page, pageSize);

    var result = new TaskListViewModel
    {
        Tasks = tasks,
        Filter = filter,
        Pagination = new PaginationInfo
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalCount
        }
    };
    
    // Guardamos en caché con un tiempo de expiración corto (30 segundos)
    _cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(30));
    
    return result;
}
```

Esta implementación utiliza un tiempo de expiración corto (30 segundos) para las listas de tareas, ya que estos datos pueden cambiar con frecuencia. Además, se implementa un mecanismo de invalidación de caché que elimina todas las entradas relacionadas con listas de tareas cuando se crea, actualiza o elimina una tarea.

## Beneficios

1. **Reducción de carga en la base de datos**: Las consultas frecuentes como obtener una tarea por ID o listar categorías ahora se sirven desde el caché.
2. **Mejora en tiempos de respuesta**: Los datos en caché se recuperan más rápidamente que desde la base de datos.
3. **Escalabilidad mejorada**: La aplicación puede manejar más usuarios concurrentes al reducir la presión sobre la base de datos.
4. **Consistencia de datos**: El sistema invalida automáticamente el caché cuando los datos cambian.

## Consideraciones para Producción

1. **Configuración de Redis**: En un entorno de producción, se debe configurar un servidor Redis dedicado o un clúster para alta disponibilidad.
2. **Monitorización**: Implementar monitorización para el servidor Redis para detectar problemas de rendimiento o memoria.
3. **Seguridad**: Configurar autenticación y cifrado para la conexión a Redis en entornos de producción.
4. **Ajuste de tiempos de expiración**: Ajustar los tiempos de expiración del caché según los patrones de uso y la frecuencia de actualización de los datos.
