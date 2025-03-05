using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Sprint2.Models;

namespace Sprint2.Controllers
{
    /// <summary>
    /// Controlador para la página de inicio y otras páginas públicas.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="HomeController"/>.
        /// </summary>
        /// <param name="logger">El logger.</param>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Muestra la página de inicio.
        /// </summary>
        /// <returns>La vista de la página de inicio.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Muestra la página de privacidad.
        /// </summary>
        /// <returns>La vista de la página de privacidad.</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Muestra la página de términos y condiciones.
        /// </summary>
        /// <returns>La vista de la página de términos y condiciones.</returns>
        public IActionResult Terms()
        {
            return View();
        }

        /// <summary>
        /// Muestra la página de acerca de.
        /// </summary>
        /// <returns>La vista de la página de acerca de.</returns>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Muestra la página de contacto.
        /// </summary>
        /// <returns>La vista de la página de contacto.</returns>
        public IActionResult Contact()
        {
            return View();
        }

        /// <summary>
        /// Muestra la página de error.
        /// </summary>
        /// <returns>La vista de la página de error.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Modelo de vista para la página de error.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Obtiene o establece el ID de la solicitud.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Obtiene un valor que indica si se debe mostrar el ID de la solicitud.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
