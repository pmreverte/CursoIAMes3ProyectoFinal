// Inicialización de tooltips y popovers de Bootstrap
document.addEventListener('DOMContentLoaded', function () {
    // Inicializar todos los tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Inicializar todos los popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Auto-cerrar alertas después de 5 segundos
    setTimeout(function () {
        var alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
        alerts.forEach(function (alert) {
            var bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);

    // Manejar cambios de estado de tareas
    setupTaskStatusChanges();

    // Inicializar filtros de tareas
    setupTaskFilters();
    
    // Inicializar funcionalidad de drag & drop
    setupDragAndDrop();
    
    // Añadir clases de estado a las tarjetas de tareas
    setupTaskCardClasses();
    
    // Verificar si debemos cargar tareas de forma asíncrona
    const loadingIndicator = document.getElementById('globalLoadingIndicator');
    if (loadingIndicator && !loadingIndicator.classList.contains('d-none')) {
        console.log('Iniciando carga asíncrona de tareas desde site.js...');
        loadTasksAsynchronously();
    }
});

// Función para manejar cambios de estado de tareas
function setupTaskStatusChanges() {
    var statusButtons = document.querySelectorAll('.quick-status-change');
    
    statusButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            var taskId = this.getAttribute('data-task-id');
            var newStatus = this.getAttribute('data-status');
            
            if (taskId && newStatus) {
                updateTaskStatus(taskId, newStatus);
            }
        });
    });
}

// Función para añadir clases de estado a las tarjetas de tareas
function setupTaskCardClasses() {
    document.querySelectorAll('.task-card').forEach(function(card) {
        const statusBadge = card.querySelector('.badge[data-bs-toggle="tooltip"][title="Estado"]');
        if (statusBadge) {
            const statusText = statusBadge.textContent.trim();
            
            if (statusText.includes('Pendiente')) {
                card.classList.add('status-pending');
            } else if (statusText.includes('En Progreso')) {
                card.classList.add('status-in-progress');
            } else if (statusText.includes('Completada')) {
                card.classList.add('status-completed');
            }
        }
    });
}

// Función para configurar drag & drop
function setupDragAndDrop() {
    // Verificar si estamos en la vista de kanban
    const kanbanView = document.getElementById('kanbanView');
    if (!kanbanView) return;
    
    const taskCards = document.querySelectorAll('.task-card');
    const dropZones = document.querySelectorAll('.task-column');
    
    let draggedItem = null;
    
    // Configurar eventos para elementos arrastrables
    taskCards.forEach(card => {
        card.setAttribute('draggable', 'true');
        
        card.addEventListener('dragstart', function(e) {
            draggedItem = this;
            setTimeout(() => {
                this.classList.add('dragging');
            }, 0);
            
            // Almacenar el ID de la tarea en el dataTransfer
            e.dataTransfer.setData('text/plain', this.getAttribute('data-task-id'));
            e.dataTransfer.effectAllowed = 'move';
        });
        
        card.addEventListener('dragend', function() {
            this.classList.remove('dragging');
            draggedItem = null;
            
            // Eliminar la clase drag-over de todas las zonas de destino
            dropZones.forEach(zone => {
                zone.classList.remove('drag-over');
            });
        });
    });
    
    // Configurar eventos para zonas de destino
    dropZones.forEach(zone => {
        zone.addEventListener('dragover', function(e) {
            e.preventDefault();
            this.classList.add('drag-over');
        });
        
        zone.addEventListener('dragleave', function() {
            this.classList.remove('drag-over');
        });
        
        zone.addEventListener('drop', function(e) {
            e.preventDefault();
            this.classList.remove('drag-over');
            
            if (draggedItem) {
                const taskId = e.dataTransfer.getData('text/plain');
                const newStatus = this.getAttribute('data-status');
                
                if (taskId && newStatus) {
                    // Mostrar animación de carga
                    showLoadingIndicator(taskId);
                    
                    // Actualizar el estado de la tarea
                    updateTaskStatus(taskId, newStatus);
                }
            }
        });
    });
}

// Función para actualizar el estado de una tarea mediante AJAX
function updateTaskStatus(taskId, newStatus) {
    // Validar que el estado sea un valor válido (0, 1 o 2)
    if (![0, 1, 2].includes(parseInt(newStatus))) {
        showNotification('Estado no válido', 'danger');
        return;
    }
    
    // Obtener el token anti-falsificación
    var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    if (!token) {
        console.error('No se pudo encontrar el token anti-falsificación');
        showNotification('Error: No se pudo encontrar el token de seguridad', 'danger');
        return;
    }
    
    // Crear form data
    var formData = new FormData();
    formData.append('id', taskId);
    formData.append('newStatus', newStatus);
    formData.append('__RequestVerificationToken', token);
    
    // Mostrar indicador de carga
    showLoadingIndicator(taskId);
    
    // Enviar solicitud AJAX
    fetch('/Tasks/UpdateStatus', {
        method: 'POST',
        body: formData,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Error en la respuesta del servidor: ' + response.status);
        }
        return response.json();
    })
    .then(data => {
        // Asegurarnos de ocultar el indicador de carga
        hideLoadingIndicator(taskId);
        
        if (data.success) {
            // Mostrar notificación de éxito con animación
            showNotification(`Tarea actualizada a "${getStatusName(newStatus)}"`, 'success');
            
            // Actualizar la interfaz sin recargar la página
            updateTaskCardUI(taskId, newStatus);
            
            // Actualizar contadores de tareas
            updateTaskCounters();
        } else {
            showNotification('Error al actualizar el estado: ' + (data.message || 'Error desconocido'), 'danger');
            // Recargar la página si hay un error para asegurar que la UI esté sincronizada
            setTimeout(() => location.reload(), 2000);
        }
    })
    .catch(error => {
        // Asegurarnos de ocultar el indicador de carga
        hideLoadingIndicator(taskId);
        showNotification('Error al actualizar el estado de la tarea: ' + error.message, 'danger');
        console.error('Error:', error);
        // Recargar la página si hay un error para asegurar que la UI esté sincronizada
        setTimeout(() => location.reload(), 2000);
    });
}

// Función para obtener el nombre del estado a partir del valor numérico
function getStatusName(statusValue) {
    switch (parseInt(statusValue)) {
        case 0: return "Pendiente";
        case 1: return "En Progreso";
        case 2: return "Completada";
        default: return "Desconocido";
    }
}

// Función para actualizar la interfaz de la tarjeta de tarea sin recargar la página
function updateTaskCardUI(taskId, newStatus) {
    console.log(`Actualizando UI para tarea ${taskId} a estado ${newStatus}`);
    
    // Convertir a entero para asegurar comparaciones correctas
    newStatus = parseInt(newStatus);
    
    const taskCard = document.querySelector(`.task-card[data-task-id="${taskId}"]`);
    if (!taskCard) {
        console.warn(`No se encontró la tarjeta para la tarea ${taskId}`);
        return;
    }
    
    // Actualizar la clase de estado
    taskCard.classList.remove('status-pending', 'status-in-progress', 'status-completed');
    
    const statusClass = newStatus === 0 ? 'status-pending' : 
                        newStatus === 1 ? 'status-in-progress' : 
                        'status-completed';
    
    taskCard.classList.add(statusClass);
    
    // Actualizar el badge de estado
    const statusBadge = taskCard.querySelector('.badge[title="Estado"]');
    if (statusBadge) {
        // Actualizar clase del badge
        statusBadge.className = `badge ${
            newStatus === 0 ? 'bg-warning text-dark' : 
            newStatus === 1 ? 'bg-primary' : 
            'bg-success'
        }`;
        
        // Actualizar icono
        const icon = statusBadge.querySelector('i');
        if (icon) {
            icon.className = `bi ${
                newStatus === 0 ? 'bi-hourglass' : 
                newStatus === 1 ? 'bi-play-circle' : 
                'bi-check-circle'
            } me-1`;
        }
        
        // Actualizar texto
        const statusName = getStatusName(newStatus);
        
        // Reemplazar el texto del estado
        if (statusBadge.lastChild && statusBadge.lastChild.nodeType === Node.TEXT_NODE) {
            statusBadge.lastChild.nodeValue = statusName;
        } else {
            // Si no podemos encontrar el nodo de texto, reemplazar todo el contenido
            const iconHTML = icon ? icon.outerHTML : '';
            statusBadge.innerHTML = `${iconHTML} ${statusName}`;
        }
    }
    
    // Si estamos en vista Kanban, mover la tarjeta a la columna correspondiente
    const kanbanView = document.getElementById('kanbanView');
    if (kanbanView && kanbanView.style.display !== 'none') {
        const targetColumn = document.querySelector(`.task-column[data-status="${newStatus}"]`);
        if (targetColumn) {
            // Obtener el contenedor de la tarjeta
            const taskItem = taskCard.closest('.task-item');
            const sourceItem = taskItem || taskCard;
            
            // En lugar de clonar, simplemente mover la tarjeta para mantener el formato exacto
            // Esto evita problemas con la clonación que pueden afectar al formato
            
            // Añadir animación
            sourceItem.classList.add('fade-in');
            
            // Mover la tarjeta a la nueva columna
            const columnBody = targetColumn.querySelector('.task-column-body');
            if (columnBody) {
                // Mover la tarjeta en lugar de clonarla
                columnBody.appendChild(sourceItem);
            }
        }
    }
}

// Función para configurar drag & drop para una tarjeta específica
function setupDragAndDropForCard(card) {
    if (!card) return;
    
    card.setAttribute('draggable', 'true');
    
    // Usar una técnica diferente para evitar problemas con los event listeners
    // En lugar de clonar, simplemente añadir los event listeners directamente
    
    // Primero, eliminar los event listeners existentes (si es posible)
    card.removeEventListener('dragstart', handleDragStart);
    card.removeEventListener('dragend', handleDragEnd);
    
    // Luego, añadir los nuevos event listeners
    card.addEventListener('dragstart', handleDragStart);
    card.addEventListener('dragend', handleDragEnd);
    
    return card;
}

// Función para manejar el inicio del arrastre
function handleDragStart(e) {
    draggedItem = this;
    setTimeout(() => {
        this.classList.add('dragging');
    }, 0);
    
    e.dataTransfer.setData('text/plain', this.getAttribute('data-task-id'));
    e.dataTransfer.effectAllowed = 'move';
}

// Función para manejar el fin del arrastre
function handleDragEnd() {
    this.classList.remove('dragging');
    draggedItem = null;
    
    document.querySelectorAll('.task-column').forEach(zone => {
        zone.classList.remove('drag-over');
    });
}

// Función para configurar acciones rápidas para una tarjeta específica
function setupQuickActionsForCard(card) {
    card.querySelectorAll('.quick-status-change').forEach(button => {
        button.addEventListener('click', function() {
            const taskId = this.getAttribute('data-task-id');
            const newStatus = this.getAttribute('data-status');
            
            if (taskId && newStatus) {
                updateTaskStatus(taskId, newStatus);
            }
        });
    });
}

// Función para actualizar los contadores de tareas
function updateTaskCounters() {
    // Contar tareas en cada estado
    const pendingCount = document.querySelectorAll('.task-card.status-pending').length;
    const inProgressCount = document.querySelectorAll('.task-card.status-in-progress').length;
    const completedCount = document.querySelectorAll('.task-card.status-completed').length;
    
    // Actualizar badges de contadores
    document.querySelectorAll('.task-counter-pending').forEach(el => {
        el.textContent = pendingCount;
    });
    
    document.querySelectorAll('.task-counter-in-progress').forEach(el => {
        el.textContent = inProgressCount;
    });
    
    document.querySelectorAll('.task-counter-completed').forEach(el => {
        el.textContent = completedCount;
    });
}

// Función para mostrar indicador de carga
function showLoadingIndicator(taskId) {
    console.log(`Mostrando indicador de carga para tarea ${taskId}`);
    
    // Buscar la tarjeta de tarea por su ID
    var taskCard = document.querySelector(`.task-card[data-task-id="${taskId}"]`);
    var taskItem = document.querySelector(`.task-item[data-task-id="${taskId}"]`);
    
    // Elemento al que añadiremos el indicador
    var targetElement = taskCard || taskItem;
    
    if (targetElement) {
        // Añadir clase de carga
        targetElement.classList.add('loading');
        
        // Crear indicador de carga si no existe
        if (!targetElement.querySelector('.loading-indicator')) {
            var loadingIndicator = document.createElement('div');
            loadingIndicator.className = 'loading-indicator';
            loadingIndicator.innerHTML = '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Cargando...</span></div>';
            
            targetElement.appendChild(loadingIndicator);
        }
    } else {
        console.warn(`No se encontró elemento para la tarea ${taskId}`);
    }
}

// Función para ocultar indicador de carga
function hideLoadingIndicator(taskId) {
    console.log(`Ocultando indicador de carga para tarea ${taskId}`);
    
    // Buscar todos los posibles elementos que podrían tener el indicador
    document.querySelectorAll(`.task-card[data-task-id="${taskId}"], .task-item[data-task-id="${taskId}"]`).forEach(element => {
        // Quitar clase de carga
        element.classList.remove('loading');
        
        // Eliminar indicador de carga
        var loadingIndicator = element.querySelector('.loading-indicator');
        if (loadingIndicator) {
            loadingIndicator.remove();
        }
    });
    
    // Buscar también por el ID directamente (por si acaso)
    document.querySelectorAll(`[data-task-id="${taskId}"]`).forEach(element => {
        element.classList.remove('loading');
        
        var loadingIndicator = element.querySelector('.loading-indicator');
        if (loadingIndicator) {
            loadingIndicator.remove();
        }
    });
}

// Función para mostrar notificación
function showNotification(message, type = 'info') {
    var alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.setAttribute('role', 'alert');
    
    // Seleccionar el icono adecuado según el tipo de notificación
    let icon = 'bi-info-circle-fill';
    if (type === 'danger') icon = 'bi-exclamation-triangle-fill';
    if (type === 'success') icon = 'bi-check-circle-fill';
    if (type === 'warning') icon = 'bi-exclamation-circle-fill';
    
    alertDiv.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi ${icon} me-2 fs-4"></i>
            <div>${message}</div>
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    var container = document.querySelector('.notifications-container');
    if (!container) {
        // Crear el contenedor si no existe
        container = document.createElement('div');
        container.className = 'notifications-container';
        document.body.appendChild(container);
    }
    
    container.appendChild(alertDiv);
    
    // Añadir animación
    setTimeout(() => {
        alertDiv.style.transform = 'translateX(0)';
        alertDiv.style.opacity = '1';
    }, 10);
    
    // Auto-cerrar después de 5 segundos
    setTimeout(() => {
        alertDiv.style.transform = 'translateX(100%)';
        alertDiv.style.opacity = '0';
        
        // Eliminar el elemento después de la animación
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.parentNode.removeChild(alertDiv);
            }
        }, 300);
    }, 5000);
}

// Función para configurar filtros de tareas
function setupTaskFilters() {
    var filterForm = document.getElementById('taskFilterForm');
    if (filterForm) {
        // Limpiar filtros
        var clearFiltersBtn = document.getElementById('clearFilters');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', function (e) {
                e.preventDefault();
                
                // Restablecer todos los campos del formulario
                filterForm.reset();
                
                // Enviar el formulario
                filterForm.submit();
            });
        }
        
        // Aplicar filtros automáticamente al cambiar los valores
        var filterInputs = filterForm.querySelectorAll('select, input[type="checkbox"]');
        filterInputs.forEach(function (input) {
            input.addEventListener('change', function () {
                filterForm.submit();
            });
        });
    }
}

// Función para cargar tareas de forma asíncrona
function loadTasksAsynchronously() {
    console.log('Cargando tareas de forma asíncrona desde site.js...');
    
    const tasksContainer = document.getElementById('tasksContainer');
    const loadingIndicator = document.getElementById('globalLoadingIndicator');
    
    if (!tasksContainer || !loadingIndicator) {
        console.error('No se encontraron los elementos necesarios para cargar tareas');
        return;
    }
    
    // Obtener los parámetros de la URL actual
    const urlParams = new URLSearchParams(window.location.search);
    
    // Construir la URL para la solicitud AJAX
    let url = '/Tasks/GetTasksJson';
    if (urlParams.toString()) {
        url += '?' + urlParams.toString();
    }
    
    console.log('Realizando solicitud AJAX a:', url);
    
    // Realizar la solicitud AJAX
    fetch(url)
        .then(response => {
            console.log('Respuesta recibida:', response.status);
            if (!response.ok) {
                throw new Error('Error en la respuesta del servidor: ' + response.status);
            }
            return response.json();
        })
        .then(data => {
            console.log('Datos recibidos:', data);
            
            // Ocultar el indicador de carga
            loadingIndicator.classList.add('d-none');
            
            // Mostrar el contenedor de tareas
            tasksContainer.classList.remove('d-none');
            
            // Verificar si hay tareas
            if (data && data.tasks && data.tasks.length > 0) {
                // Actualizar la interfaz con los datos recibidos
                updateTasksUI(data);
            } else {
                console.log('No se encontraron tareas');
                // Mostrar mensaje de no hay tareas
                tasksContainer.innerHTML = `
                    <div class="card mb-4">
                        <div class="card-body text-center py-5">
                            <i class="bi bi-search display-1 text-muted mb-3"></i>
                            <h3>No se encontraron tareas</h3>
                            <p class="text-muted mb-4">No hay tareas que coincidan con los criterios de búsqueda.</p>
                            <a href="/Tasks/Create" class="btn btn-primary">
                                <i class="bi bi-plus-circle me-2"></i>Crear Nueva Tarea
                            </a>
                        </div>
                    </div>
                `;
            }
        })
        .catch(error => {
            console.error('Error al cargar tareas:', error);
            
            // Ocultar el indicador de carga
            loadingIndicator.classList.add('d-none');
            
            // Mostrar el contenedor de tareas (aunque esté vacío)
            tasksContainer.classList.remove('d-none');
            
            // Mostrar un mensaje de error
            showNotification('Error al cargar las tareas: ' + error.message + '. Por favor, recarga la página.', 'danger');
            
            // Mostrar mensaje de error en el contenedor
            tasksContainer.innerHTML = `
                <div class="alert alert-danger">
                    <h4 class="alert-heading"><i class="bi bi-exclamation-triangle-fill me-2"></i>Error al cargar las tareas</h4>
                    <p>${error.message}</p>
                    <hr>
                    <p class="mb-0">Intenta recargar la página o contacta al administrador del sistema.</p>
                    <button class="btn btn-outline-danger mt-3" onclick="location.reload()">
                        <i class="bi bi-arrow-clockwise me-2"></i>Recargar página
                    </button>
                </div>
            `;
        });
}

// Función para actualizar la interfaz con los datos recibidos
function updateTasksUI(data) {
    console.log('Actualizando interfaz con datos recibidos');
    
    // Actualizar las tareas en cada vista
    if (document.getElementById('cardView')) {
        updateCardView(data.tasks);
    }
    
    if (document.getElementById('listView')) {
        updateListView(data.tasks);
    }
    
    if (document.getElementById('kanbanView')) {
        updateKanbanView(data.tasks);
    }
    
    // Inicializar funcionalidades
    setupTaskCardClasses();
    setupDragAndDrop();
    setupTaskStatusChanges();
    
    // Inicializar tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function(tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Funciones auxiliares para obtener clases y nombres
function getStatusBadgeClass(status) {
    switch (parseInt(status)) {
        case 0: return 'bg-warning text-dark';
        case 1: return 'bg-primary';
        case 2: return 'bg-success';
        default: return 'bg-secondary';
    }
}

function getStatusIconClass(status) {
    switch (parseInt(status)) {
        case 0: return 'bi-hourglass';
        case 1: return 'bi-play-circle';
        case 2: return 'bi-check-circle';
        default: return 'bi-question-circle';
    }
}

function getPriorityBadgeClass(priority) {
    switch (parseInt(priority)) {
        case 0: return 'bg-success';
        case 1: return 'bg-warning text-dark';
        case 2: return 'bg-danger';
        case 3: return 'bg-danger text-white';
        default: return 'bg-secondary';
    }
}

function getPriorityIconClass(priority) {
    switch (parseInt(priority)) {
        case 0: return 'bi-arrow-down-circle';
        case 1: return 'bi-dash-circle';
        case 2: return 'bi-arrow-up-circle';
        case 3: return 'bi-exclamation-circle';
        default: return 'bi-circle';
    }
}

function getPriorityName(priority) {
    switch (parseInt(priority)) {
        case 0: return 'Baja';
        case 1: return 'Media';
        case 2: return 'Alta';
        case 3: return 'Urgente';
        default: return 'Desconocida';
    }
}

// Función para formatear fechas
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString();
}

// Función para actualizar la vista de tarjetas
function updateCardView(tasks) {
    console.log('Actualizando vista de tarjetas');
    
    const cardView = document.getElementById('cardView');
    if (!cardView) {
        console.warn('No se encontró el elemento cardView');
        return;
    }
    
    // Crear el contenedor de filas si no existe
    let cardViewContent = cardView.querySelector('.row');
    if (!cardViewContent) {
        cardViewContent = document.createElement('div');
        cardViewContent.className = 'row';
        cardView.appendChild(cardViewContent);
    } else {
        // Limpiar el contenido actual
        cardViewContent.innerHTML = '';
    }
    
    // Si no hay tareas, mostrar un mensaje
    if (!tasks || tasks.length === 0) {
        cardView.innerHTML = `
            <div class="card mb-4">
                <div class="card-body text-center py-5">
                    <i class="bi bi-search display-1 text-muted mb-3"></i>
                    <h3>No se encontraron tareas</h3>
                    <p class="text-muted mb-4">No hay tareas que coincidan con los criterios de búsqueda.</p>
                    <a href="/Tasks/Create" class="btn btn-primary">
                        <i class="bi bi-plus-circle me-2"></i>Crear Nueva Tarea
                    </a>
                </div>
            </div>
        `;
        return;
    }
}

// Función para actualizar la vista de lista
function updateListView(tasks) {
    console.log('Actualizando vista de lista');
    
    const listView = document.getElementById('listView');
    if (!listView) {
        console.warn('No se encontró el elemento listView');
        return;
    }
    
    // Si no hay tareas, mostrar un mensaje
    if (!tasks || tasks.length === 0) {
        listView.innerHTML = `
            <div class="card mb-4">
                <div class="card-body text-center py-5">
                    <i class="bi bi-search display-1 text-muted mb-3"></i>
                    <h3>No se encontraron tareas</h3>
                    <p class="text-muted mb-4">No hay tareas que coincidan con los criterios de búsqueda.</p>
                    <a href="/Tasks/Create" class="btn btn-primary">
                        <i class="bi bi-plus-circle me-2"></i>Crear Nueva Tarea
                    </a>
                </div>
            </div>
        `;
        return;
    }
}

// Función para actualizar la vista Kanban
function updateKanbanView(tasks) {
    console.log('Actualizando vista Kanban');
    
    const kanbanView = document.getElementById('kanbanView');
    if (!kanbanView) {
        console.warn('No se encontró el elemento kanbanView');
        return;
    }
    
    // Si no hay tareas, mostrar un mensaje
    if (!tasks || tasks.length === 0) {
        kanbanView.innerHTML = `
            <div class="card mb-4">
                <div class="card-body text-center py-5">
                    <i class="bi bi-search display-1 text-muted mb-3"></i>
                    <h3>No se encontraron tareas</h3>
                    <p class="text-muted mb-4">No hay tareas que coincidan con los criterios de búsqueda.</p>
                    <a href="/Tasks/Create" class="btn btn-primary">
                        <i class="bi bi-plus-circle me-2"></i>Crear Nueva Tarea
                    </a>
                </div>
            </div>
        `;
        return;
    }
}
