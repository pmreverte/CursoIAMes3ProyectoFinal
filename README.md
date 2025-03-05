# 📝 Aplicación de Gestión de Tareas (Sprint3)

## Descripción

Este proyecto es una aplicación web de gestión de tareas construida usando ASP.NET Core MVC (.NET 9.0). Permite a los usuarios crear, leer, actualizar y eliminar tareas, con múltiples vistas para visualizar y organizar las tareas de manera eficiente. La aplicación también admite el filtrado de tareas por estado, prioridad y categoría.

## Características

* ✅ **Operaciones CRUD:** Crear, Leer, Actualizar y Eliminar tareas.
* 📋 **Propiedades de la Tarea:** Cada tarea tiene una descripción, estado (Pendiente, En Progreso, Completada), prioridad (Baja, Media, Alta, Urgente), categoría, fecha de vencimiento y notas.
* 🔍 **Filtrado:** Filtrar tareas según estado, prioridad y categoría.
* 📄 **Paginación:** Navegar a través de un gran número de tareas usando paginación.
* 🔔 **Notificaciones:** Mostrar mensajes de éxito y error al usuario.
* 🛡️ **Validación:** Validación robusta tanto del lado del cliente como del servidor para garantizar la integridad de los datos.
* ⚠️ **Resaltado de Tareas Vencidas:** Las tareas vencidas se resaltan visualmente.
* 🔄 **Múltiples Vistas:** Visualiza tus tareas en tres formatos diferentes:
  * **Vista de Tarjetas:** Muestra las tareas como tarjetas individuales para una visualización detallada.
  * **Vista de Lista:** Presenta las tareas en formato de tabla para una visualización compacta.
  * **Vista Kanban:** Organiza las tareas en columnas según su estado (Pendiente, En Progreso, Completada).
* 🖱️ **Arrastrar y Soltar:** En la vista Kanban, arrastra y suelta tareas entre columnas para cambiar su estado.
* 💾 **Persistencia de Preferencias:** La aplicación recuerda tu vista preferida entre sesiones.

## Estructura del Proyecto

El proyecto sigue la estructura estándar de ASP.NET Core MVC:

* **`Controllers/`:** Contiene los controladores que manejan las solicitudes de usuario e interactúan con la capa de servicios.
  * **`TasksController.cs`:** Maneja todas las operaciones relacionadas con tareas.
  * **`HomeController.cs`:** Maneja las páginas estáticas y la navegación principal.
* **`Models/`:** Contiene los modelos de datos y ViewModels.
  * **`TodoTask.cs`:** El modelo principal para las tareas.
  * **`TaskListViewModel.cs`:** ViewModel para la lista de tareas con filtrado y paginación.
  * **`ApplicationUser.cs`:** Modelo extendido para usuarios de la aplicación.
  * **`AuditLog.cs`:** Modelo para el registro de auditoría de cambios.
* **`Views/`:** Contiene las vistas Razor para mostrar la interfaz de usuario.
  * **`Tasks/`:** Vistas específicas para la funcionalidad de gestión de tareas.
    * **`Index.cshtml`:** Vista principal con múltiples modos de visualización (tarjetas, lista, kanban).
    * **`_TaskCard.cshtml`:** Plantilla parcial para mostrar una tarea como tarjeta.
  * **`Shared/`:** Vistas y diseños compartidos.
* **`Areas/`:** Contiene áreas funcionales separadas de la aplicación.
  * **`Identity/`:** Implementación de autenticación y gestión de usuarios.
* **`Interfaces/`:** Define las interfaces para los servicios y repositorios.
  * **`ITaskService.cs`:** Interfaz para el servicio de tareas.
  * **`ITaskRepository.cs`:** Interfaz para el repositorio de tareas.
  * **`ICacheService.cs`:** Interfaz para el servicio de caché.
* **`Repositories/`:** Contiene las implementaciones de los repositorios.
  * **`EfTaskRepository.cs`:** Implementación del repositorio de tareas usando Entity Framework.
* **`Services/`:** Contiene las implementaciones de los servicios.
  * **`TaskService.cs`:** Implementación del servicio de tareas.
  * **`RedisCacheService.cs`:** Implementación del servicio de caché usando Redis.
  * **`EmailSender.cs`:** Servicio para enviar correos electrónicos.
* **`Validators/`:** Contiene validadores para los modelos.
  * **`TodoTaskValidator.cs`:** Validador para el modelo TodoTask usando FluentValidation.
* **`Middleware/`:** Contiene middleware personalizado.
  * **`RateLimitingMiddleware.cs`:** Middleware para limitar la tasa de solicitudes.
  * **`SecurityHeadersMiddleware.cs`:** Middleware para agregar encabezados de seguridad.
* **`wwwroot/`:** Contiene archivos estáticos (CSS, JavaScript, imágenes).
  * **`js/taskLoader.js`:** Script para cargar tareas de forma asíncrona.
  * **`css/site.css`:** Estilos CSS para toda la aplicación.
* **`Data/`:** Contiene código relacionado con la base de datos.
  * **`ApplicationDbContext.cs`:** Contexto de Entity Framework para la aplicación.
  * **`Migrations/`:** Migraciones de Entity Framework para la base de datos.
* **`Tests/`:** Contiene pruebas para la aplicación.
  * **`UnitTests/`:** Pruebas unitarias para servicios y repositorios.
  * **`IntegrationTests/`:** Pruebas de integración para controladores.
  * **`UITests/`:** Pruebas de interfaz de usuario.
* **`Documentacion/`:** Contiene documentación del proyecto.
  * **`ArquitecturaGeneral.md`:** Descripción de la arquitectura del sistema.
  * **`SeguridadYMejoresPracticas.md`:** Documentación sobre seguridad y mejores prácticas.
  * **`MejorasInterfazUsuario.md`:** Documentación sobre mejoras en la interfaz de usuario.
* **`Program.cs`:** El punto de entrada principal de la aplicación.
* **`appsettings.json`:** Configuración de la aplicación.

## Arquitectura

La aplicación está estructurada siguiendo una arquitectura en capas con el patrón Modelo-Vista-Controlador (MVC) como base:

- **Capa de Presentación (MVC)**:
  - **Controladores**: Gestionan el flujo de la aplicación, manejan la entrada del usuario e interactúan con la capa de servicios para procesar datos.
  - **Modelos**: Representan la estructura de datos y la lógica de negocio. Son usados por los controladores para pasar datos a las vistas.
  - **Vistas**: Renderizan la interfaz de usuario y muestran datos al usuario. Incluyen múltiples formatos de visualización (tarjetas, lista, kanban).

- **Capa de Servicios**:
  - **Servicios**: Contienen la lógica de negocio de la aplicación. Procesan datos e implementan la funcionalidad central.
  - **Validadores**: Aseguran la integridad de los datos mediante reglas de validación robustas.

- **Capa de Acceso a Datos**:
  - **Repositorios**: Abstraen la capa de acceso a datos, proporcionando una API limpia para operaciones de datos.
  - **Contexto de Base de Datos**: Gestiona la conexión con la base de datos y las operaciones de Entity Framework.

- **Infraestructura Transversal**:
  - **Middleware**: Componentes que procesan las solicitudes HTTP antes de llegar a los controladores.
  - **Servicios de Caché**: Mejoran el rendimiento almacenando datos frecuentemente accedidos.
  - **Autenticación y Autorización**: Gestionan la seguridad y el control de acceso.
  - **Registro de Auditoría**: Registra cambios en los datos para fines de seguimiento y cumplimiento.

Esta arquitectura proporciona una separación clara de responsabilidades, facilitando el mantenimiento, las pruebas y la escalabilidad de la aplicación.

## Configuración e Instalación

1. **Requisitos Previos:**
   * SDK de .NET 9.0 instalado.

2. **Clonar el repositorio:**

   ```bash
   git clone [URL del repositorio]
   cd Sprint2
   ```
   (Reemplace `[URL del repositorio]` con la URL real de su repositorio.)

3. **Restaurar Dependencias**
   ```bash
   dotnet restore
   ```

## Ejecutar la Aplicación

1. **Usando la CLI de .NET:**

   Navegue al directorio del proyecto en su terminal y ejecute:

   ```bash
   dotnet run
   ```

   Esto iniciará la aplicación. Por defecto, estará disponible en `http://localhost:5080` y `https://localhost:7095`.

2. **Usando Visual Studio Code:**

   Abra la carpeta del proyecto en Visual Studio Code. Presione F5 para iniciar la depuración. La aplicación se iniciará y podrá acceder a ella usando las URLs mencionadas anteriormente.

## Mejoras Recientes

### Sprint 3 - Marzo 2025

1. **Múltiples Vistas de Tareas**
   - Implementación de tres vistas diferentes: Tarjetas, Lista y Kanban
   - Persistencia de la preferencia de vista del usuario mediante localStorage
   - Interfaz intuitiva para cambiar entre vistas

2. **Funcionalidad Drag & Drop**
   - Arrastrar y soltar tareas entre columnas en la vista Kanban
   - Actualización automática del estado de las tareas al moverlas

3. **Corrección de Errores**
   - Solucionado el problema que impedía marcar tareas como completadas
   - Mejorada la validación para evitar conflictos entre estados y prioridades
   - Optimizado el tiempo de respuesta de las solicitudes AJAX

4. **Mejoras de Rendimiento**
   - Implementación de caché con Redis para reducir la carga de la base de datos
   - Optimización de consultas para mejorar los tiempos de respuesta
   - Carga asíncrona de tareas para una experiencia de usuario más fluida

5. **Seguridad**
   - Implementación de políticas de seguridad de contenido (CSP)
   - Middleware de limitación de tasa para prevenir ataques de fuerza bruta
   - Registro de auditoría para seguimiento de cambios en las tareas

## Ejecutar Pruebas

El proyecto incluye pruebas unitarias, de integración y de interfaz de usuario para asegurar que la aplicación funcione correctamente.

1. **Pruebas Unitarias**: Ubicadas en `Tests/UnitTests/`. Ejecútelas usando:
   ```bash
   dotnet test Tests/UnitTests/
   ```

2. **Pruebas de Integración**: Ubicadas en `Tests/IntegrationTests/`. Ejecútelas usando:
   ```bash
   dotnet test Tests/IntegrationTests/
   ```

3. **Pruebas de Interfaz de Usuario**: Ubicadas en `Tests/UITests/`. Estas pruebas pueden requerir un controlador de navegador. Ejecútelas usando:
   ```bash
   dotnet test Tests/UITests/
   ```

### Ejecutar Pruebas desde Visual Studio Code

Para ejecutar las pruebas desde el terminal de Visual Studio Code:

1. Abra el terminal integrado en Visual Studio Code (puede hacerlo presionando `` Ctrl+` ``).
2. Navegue al directorio del proyecto si no está ya allí.
3. Ejecute el comando `dotnet test` seguido de la ruta al directorio de pruebas que desea ejecutar, por ejemplo:
   ```bash
   dotnet test Tests/UnitTests/
   ```

Esto ejecutará las pruebas en el terminal integrado y mostrará los resultados directamente en Visual Studio Code.
