// Función para configurar clases de tarjetas de tareas
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
    console.log('Configurando drag & drop');
    
    // Verificar si estamos en la vista de kanban
    const kanbanView = document.getElementById('kanbanView');
    
    // Verificar si la vista kanban está activa
    const isKanbanActive = kanbanView && 
                          (kanbanView.classList.contains('active') || 
                           kanbanView.style.display === 'block' || 
                           window.getComputedStyle(kanbanView).display !== 'none');
    
    console.log('Vista kanban activa:', isKanbanActive);
    
    if (!isKanbanActive) {
        console.log('La vista kanban no está activa, no se configura drag & drop');
        return;
    }
    
    // Limpiar event listeners anteriores
    document.querySelectorAll('.task-item').forEach(item => {
        const clone = item.cloneNode(true);
        if (item.parentNode) {
            item.parentNode.replaceChild(clone, item);
        }
    });
    
    document.querySelectorAll('.task-column').forEach(zone => {
        const clone = zone.cloneNode(true);
        if (zone.parentNode) {
            zone.parentNode.replaceChild(clone, zone);
        }
    });
    
    // Obtener elementos actualizados
    const taskItems = document.querySelectorAll('.task-item');
    const dropZones = document.querySelectorAll('.task-column');
    
    console.log('Elementos encontrados:', {
        taskItems: taskItems.length,
        dropZones: dropZones.length
    });
    
    let draggedItem = null;
    
    // Configurar eventos para elementos arrastrables
    taskItems.forEach(item => {
        item.setAttribute('draggable', 'true');
        
        item.addEventListener('dragstart', function(e) {
            console.log('Iniciando arrastre de tarea:', this.getAttribute('data-task-id'));
            draggedItem = this;
            setTimeout(() => {
                this.classList.add('dragging');
            }, 0);
            
            // Almacenar el ID de la tarea en el dataTransfer
            e.dataTransfer.setData('text/plain', this.getAttribute('data-task-id'));
            e.dataTransfer.effectAllowed = 'move';
        });
        
        item.addEventListener('dragend', function() {
            console.log('Finalizando arrastre');
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
            
            // Obtener el ID de la tarea del dataTransfer
            const taskId = e.dataTransfer.getData('text/plain');
            if (!taskId) {
                console.error('No se pudo obtener el ID de la tarea');
                return;
            }
            
            const newStatus = this.getAttribute('data-status');
            if (!newStatus) {
                console.error('No se pudo obtener el estado de la columna');
                return;
            }
            
            console.log('Tarea soltada:', taskId, 'Nuevo estado:', newStatus);
            
            // Actualizar el estado de la tarea mediante AJAX
            updateTaskStatus(taskId, newStatus);
        });
    });
    
    console.log('Drag & drop configurado correctamente');
}

// Función para configurar cambios rápidos de estado
function setupTaskStatusChanges() {
    document.querySelectorAll('.quick-status-change').forEach(button => {
        button.addEventListener('click', function() {
            const taskId = this.getAttribute('data-task-id');
            const newStatus = this.getAttribute('data-status');
            
            console.log('Cambio rápido de estado:', taskId, 'Nuevo estado:', newStatus);
            
            if (taskId && newStatus) {
                updateTaskStatus(taskId, newStatus);
            }
        });
    });
}

// Función para actualizar el estado de una tarea mediante un formulario
function updateTaskStatus(taskId, newStatus) {
    console.log(`Actualizando tarea ${taskId} a estado ${newStatus}`);
    
    // Mostrar indicador de carga global
    const loadingIndicator = document.createElement('div');
    loadingIndicator.className = 'loading-indicator position-fixed';
    loadingIndicator.style.top = '50%';
    loadingIndicator.style.left = '50%';
    loadingIndicator.style.transform = 'translate(-50%, -50%)';
    loadingIndicator.style.zIndex = '9999';
    loadingIndicator.style.backgroundColor = 'rgba(255, 255, 255, 0.7)';
    loadingIndicator.style.padding = '20px';
    loadingIndicator.style.borderRadius = '10px';
    loadingIndicator.innerHTML = '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Cargando...</span></div>';
    document.body.appendChild(loadingIndicator);
    
    // Crear un formulario dinámico
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Tasks/UpdateStatus';
    form.style.display = 'none';
    
    // Añadir el token anti-falsificación
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    if (!token) {
        console.error('No se pudo encontrar el token anti-falsificación');
        showNotification('Error: No se pudo encontrar el token de seguridad', 'danger');
        if (loadingIndicator.parentNode) {
            loadingIndicator.parentNode.removeChild(loadingIndicator);
        }
        return;
    }
    
    // Añadir los campos al formulario
    const tokenInput = document.createElement('input');
    tokenInput.type = 'hidden';
    tokenInput.name = '__RequestVerificationToken';
    tokenInput.value = token;
    form.appendChild(tokenInput);
    
    const idInput = document.createElement('input');
    idInput.type = 'hidden';
    idInput.name = 'id';
    idInput.value = taskId;
    form.appendChild(idInput);
    
    const statusInput = document.createElement('input');
    statusInput.type = 'hidden';
    statusInput.name = 'newStatus';
    statusInput.value = newStatus;
    form.appendChild(statusInput);
    
    // Añadir el formulario al documento y enviarlo
    document.body.appendChild(form);
    form.submit();
}

// Función para actualizar los contadores de tareas
function updateTaskCounters() {
    const pendingCount = document.querySelectorAll('.task-column-pending .task-item').length;
    const inProgressCount = document.querySelectorAll('.task-column-in-progress .task-item').length;
    const completedCount = document.querySelectorAll('.task-column-completed .task-item').length;
    
    document.querySelector('.task-counter-pending').textContent = pendingCount;
    document.querySelector('.task-counter-in-progress').textContent = inProgressCount;
    document.querySelector('.task-counter-completed').textContent = completedCount;
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

// Función para mostrar notificaciones
function showNotification(message, type) {
    const notificationsContainer = document.getElementById('notificationsContainer');
    if (!notificationsContainer) return;
    
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show`;
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
    `;
    
    notificationsContainer.appendChild(notification);
    
    // Auto-cerrar después de 5 segundos
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            notification.remove();
        }, 150);
    }, 5000);
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

// Función para verificar si hay un parámetro de recarga forzada en la URL
function checkForceReload() {
    const urlParams = new URLSearchParams(window.location.search);
    const forceReload = urlParams.get('forceReload');
    
    console.log('Verificando parámetro forceReload:', forceReload);
    
    if (forceReload === 'True' || forceReload === 'true' || forceReload === 'TRUE' || forceReload === '1') {
        console.log('Recarga forzada detectada, actualizando la página...');
        
        // Eliminar el parámetro forceReload de la URL para evitar recargas infinitas
        urlParams.delete('forceReload');
        
        // Construir la nueva URL sin el parámetro forceReload
        const newUrl = window.location.pathname + (urlParams.toString() ? '?' + urlParams.toString() : '');
        
        // Reemplazar la URL actual sin recargar la página
        window.history.replaceState({}, document.title, newUrl);
        
        // Recargar la página inmediatamente
        location.reload();
    }
}

// Ejecutar la verificación de recarga forzada cuando se carga la página
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM cargado, verificando si es necesario recargar la página...');
    checkForceReload();
});
