using System;
using System.Threading.Tasks;

namespace Sprint2.Interfaces
{
    /// <summary>
    /// Define el contrato para las operaciones de caché en la aplicación.
    /// Proporciona métodos para almacenar, recuperar y eliminar datos en caché.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Recupera un valor de la caché utilizando la clave especificada.
        /// </summary>
        /// <typeparam name="T">El tipo de dato a recuperar.</typeparam>
        /// <param name="key">La clave única que identifica el valor en la caché.</param>
        /// <returns>El valor almacenado en caché, o null si no existe.</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Almacena un valor en la caché con la clave especificada.
        /// </summary>
        /// <typeparam name="T">El tipo de dato a almacenar.</typeparam>
        /// <param name="key">La clave única para identificar el valor en la caché.</param>
        /// <param name="value">El valor a almacenar en la caché.</param>
        /// <param name="expirationTime">El tiempo de expiración opcional para el valor en caché. Si es null, se utilizará el tiempo predeterminado.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null);

        /// <summary>
        /// Elimina un valor de la caché utilizando la clave especificada.
        /// </summary>
        /// <param name="key">La clave única que identifica el valor a eliminar.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        Task RemoveAsync(string key);
    }
}
