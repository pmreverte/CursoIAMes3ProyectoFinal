// Inicializar funcionalidades cuando se carga la página
document.addEventListener('DOMContentLoaded', function() {
    // Añadir información inicial al panel de depuración
    if (typeof addDebugMessage === 'function') {
        addDebugMessage('Inicializando aplicación de tareas...');
        addDebugMessage(`Número de tareas en el modelo: ${document.querySelectorAll('.task-item').length}`);
    }
    
    // Configurar funcionalidades adicionales
    if (typeof setupTaskCardClasses === 'function') {
        setupTaskCardClasses();
        if (typeof addDebugMessage === 'function') addDebugMessage('Clases de tarjetas configuradas');
    }
    
    if (typeof setupDragAndDrop === 'function') {
        setupDragAndDrop();
        if (typeof addDebugMessage === 'function') addDebugMessage('Drag & drop configurado');
    }
    
    if (typeof setupTaskStatusChanges === 'function') {
        setupTaskStatusChanges();
        if (typeof addDebugMessage === 'function') addDebugMessage('Cambios de estado configurados');
    }
    
    // Iniciar la carga de tareas si es necesario
    const loadTasksAsync = document.getElementById('globalLoadingIndicator') && 
                          !document.getElementById('globalLoadingIndicator').classList.contains('d-none');
    
    if (loadTasksAsync && typeof loadTasksAsynchronously === 'function') {
        if (typeof addDebugMessage === 'function') addDebugMessage('Iniciando carga asíncrona de tareas');
        // Esperar a que el DOM esté completamente cargado
        setTimeout(() => {
            if (typeof addDebugMessage === 'function') addDebugMessage('Ejecutando carga asíncrona con retraso para asegurar carga completa');
            loadTasksAsynchronously();
        }, 500);
    }
});
