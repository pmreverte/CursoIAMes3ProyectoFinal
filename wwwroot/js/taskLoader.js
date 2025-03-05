// Función para cargar tareas de forma asíncrona
function loadTasksAsynchronously() {
    console.log('Cargando tareas de forma asíncrona...');
    
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
            console.log('Estructura de datos:', JSON.stringify(data, null, 2));
            
            // Ocultar el indicador de carga
            loadingIndicator.classList.add('d-none');
            
            // Mostrar el contenedor de tareas
            tasksContainer.classList.remove('d-none');
            
            // Verificar si hay tareas (comprobando todas las posibles estructuras)
            console.log('Verificando estructura de datos...');
            console.log('data es array?', Array.isArray(data));
            console.log('data.tasks existe?', data.tasks !== undefined);
            console.log('data.Tasks existe?', data.Tasks !== undefined);
            console.log('data.tasks es array?', Array.isArray(data.tasks));
            console.log('data.Tasks es array?', Array.isArray(data.Tasks));
            
            // Intentar obtener las tareas de cualquier estructura posible
            let tasks = [];
            
            if (Array.isArray(data)) {
                console.log('Usando data directamente como array');
                tasks = data;
            } else if (data.tasks && Array.isArray(data.tasks)) {
                console.log('Usando data.tasks (array)');
                tasks = data.tasks;
            } else if (data.Tasks && Array.isArray(data.Tasks)) {
                console.log('Usando data.Tasks (array)');
                tasks = data.Tasks;
            } else if (data.tasks) {
                console.log('Usando data.tasks (no array)');
                tasks = Array.isArray(data.tasks) ? data.tasks : [data.tasks];
            } else if (data.Tasks) {
                console.log('Usando data.Tasks (no array)');
                tasks = Array.isArray(data.Tasks) ? data.Tasks : [data.Tasks];
            }
            
            console.log('Tareas encontradas:', tasks.length);
            console.log('Primera tarea:', tasks.length > 0 ? JSON.stringify(tasks[0]) : 'ninguna');
            
            if (tasks.length > 0) {
                // Actualizar la interfaz con los datos recibidos
                // Pasar directamente el array de tareas a updateTasksUI
                console.log('Actualizando UI con tareas encontradas');
                updateTasksUI(tasks);
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
    
    // Asegurarnos de que tenemos un array de tareas
    // Ahora que el controlador devuelve directamente un array, data podría ser el array directamente
    const tasks = Array.isArray(data) ? data : (data.tasks || []);
    
    console.log('Tareas a mostrar en la UI:', tasks.length);
    console.log('Primera tarea a mostrar:', tasks.length > 0 ? JSON.stringify(tasks[0]) : 'ninguna');
    
    // Verificar si el contenedor de tareas existe
    const tasksContainer = document.getElementById('tasksContainer');
    if (!tasksContainer) {
        console.error('No se encontró el contenedor de tareas');
        return;
    }
    
    // Si hay tareas, actualizar el contenido del contenedor
    if (tasks.length > 0) {
        console.log('Hay tareas para mostrar, actualizando vistas');
        
        // Asegurarse de que el contenedor de tareas esté visible
        tasksContainer.classList.remove('d-none');
        
        // Actualizar las tareas en cada vista
        updateCardView(tasks);
        updateListView(tasks);
        updateKanbanView(tasks);
        
        // Inicializar funcionalidades
        setupTaskCardClasses();
        setupDragAndDrop();
        setupTaskStatusChanges();
        
        // Inicializar tooltips
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(function(tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
    } else {
        console.warn('No hay tareas para mostrar');
        
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
}

// Función para actualizar la vista de tarjetas
function updateCardView(tasks) {
    console.log('Actualizando vista de tarjetas con', tasks.length, 'tareas');
    
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
        console.warn('No hay tareas para mostrar en la vista de tarjetas');
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
    
    console.log('Añadiendo', tasks.length, 'tarjetas a la vista');
    
    // Añadir las tarjetas
    tasks.forEach(task => {
        const taskCard = createTaskCard(task);
        const col = document.createElement('div');
        col.className = 'col-md-6 col-lg-4 mb-4 task-item';
        col.setAttribute('data-task-id', task.id);
        col.innerHTML = taskCard;
        cardViewContent.appendChild(col);
    });
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
    
    // Crear la tabla si no existe
    let table = listView.querySelector('table');
    if (!table) {
        listView.innerHTML = `
            <div class="card mb-4">
                <div class="table-responsive">
                    <table class="table table-hover mb-0">
                        <thead class="table-light">
                            <tr>
                                <th>Descripción</th>
                                <th>Estado</th>
                                <th>Prioridad</th>
                                <th>Vencimiento</th>
                                <th>Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
        table = listView.querySelector('table');
    }
    
    const tableBody = table.querySelector('tbody');
    if (!tableBody) {
        console.warn('No se encontró el cuerpo de la tabla');
        return;
    }
    
    // Limpiar el contenido actual
    tableBody.innerHTML = '';
    
    // Añadir las filas
    tasks.forEach(task => {
        const row = createTaskRow(task);
        tableBody.innerHTML += row;
    });
}

// Función para actualizar la vista Kanban
function updateKanbanView(tasks) {
    console.log('Actualizando vista Kanban');
    
    const kanbanView = document.getElementById('kanbanView');
    if (!kanbanView) {
        console.warn('No se encontró el elemento kanbanView');
        return;
    }
    
    // Verificar si la estructura Kanban existe, si no, crearla
    if (kanbanView.children.length === 0) {
        kanbanView.innerHTML = `
            <div class="row">
                <!-- Columna de tareas pendientes -->
                <div class="col-md-4 mb-4">
                    <div class="task-column task-column-pending" data-status="0">
                        <div class="task-column-header">
                            <h3><i class="bi bi-hourglass me-2"></i>Pendientes</h3>
                            <span class="badge bg-warning text-dark ms-auto task-counter-pending">0</span>
                        </div>
                        <div class="task-column-body"></div>
                    </div>
                </div>
                
                <!-- Columna de tareas en progreso -->
                <div class="col-md-4 mb-4">
                    <div class="task-column task-column-in-progress" data-status="1">
                        <div class="task-column-header">
                            <h3><i class="bi bi-play-circle me-2"></i>En Progreso</h3>
                            <span class="badge bg-primary ms-auto task-counter-in-progress">0</span>
                        </div>
                        <div class="task-column-body"></div>
                    </div>
                </div>
                
                <!-- Columna de tareas completadas -->
                <div class="col-md-4 mb-4">
                    <div class="task-column task-column-completed" data-status="2">
                        <div class="task-column-header">
                            <h3><i class="bi bi-check-circle me-2"></i>Completadas</h3>
                            <span class="badge bg-success ms-auto task-counter-completed">0</span>
                        </div>
                        <div class="task-column-body"></div>
                    </div>
                </div>
            </div>
        `;
    }
    
    // Obtener las columnas
    const pendingColumn = kanbanView.querySelector('.task-column-pending .task-column-body');
    const inProgressColumn = kanbanView.querySelector('.task-column-in-progress .task-column-body');
    const completedColumn = kanbanView.querySelector('.task-column-completed .task-column-body');
    
    if (!pendingColumn || !inProgressColumn || !completedColumn) {
        console.warn('No se encontraron las columnas Kanban');
        return;
    }
    
    // Limpiar el contenido actual
    pendingColumn.innerHTML = '';
    inProgressColumn.innerHTML = '';
    completedColumn.innerHTML = '';
    
    // Si no hay tareas, mostrar mensajes en las columnas
    if (!tasks || tasks.length === 0) {
        const emptyMessage = `
            <div class="text-center text-muted py-4">
                <i class="bi bi-inbox display-6 mb-3"></i>
                <p>No hay tareas</p>
            </div>
        `;
        pendingColumn.innerHTML = emptyMessage;
        inProgressColumn.innerHTML = emptyMessage;
        completedColumn.innerHTML = emptyMessage;
        
        // Actualizar los contadores
        kanbanView.querySelector('.task-counter-pending').textContent = '0';
        kanbanView.querySelector('.task-counter-in-progress').textContent = '0';
        kanbanView.querySelector('.task-counter-completed').textContent = '0';
        
        return;
    }
    
    // Actualizar los contadores
    const pendingCount = tasks.filter(t => t.status === 0).length;
    const inProgressCount = tasks.filter(t => t.status === 1).length;
    const completedCount = tasks.filter(t => t.status === 2).length;
    
    kanbanView.querySelector('.task-counter-pending').textContent = pendingCount;
    kanbanView.querySelector('.task-counter-in-progress').textContent = inProgressCount;
    kanbanView.querySelector('.task-counter-completed').textContent = completedCount;
    
    // Añadir las tarjetas a las columnas correspondientes
    tasks.forEach(task => {
        const taskCard = createTaskCard(task);
        const taskItem = document.createElement('div');
        taskItem.className = 'task-item';
        taskItem.setAttribute('data-task-id', task.id);
        taskItem.innerHTML = taskCard;
        
        if (task.status === 0) {
            pendingColumn.appendChild(taskItem);
        } else if (task.status === 1) {
            inProgressColumn.appendChild(taskItem);
        } else if (task.status === 2) {
            completedColumn.appendChild(taskItem);
        }
    });
}

// Función para crear una tarjeta de tarea
function createTaskCard(task) {
    // Aquí generamos el HTML para una tarjeta de tarea
    return `
        <div class="card mb-3 task-card ${task.isOverdue ? 'overdue' : ''}" data-task-id="${task.id}">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <h5 class="card-title">${task.description}</h5>
                        <div class="mb-2">
                            ${task.category ? `
                                <span class="badge bg-info" data-bs-toggle="tooltip" title="Categoría">
                                    <i class="bi bi-tag-fill me-1"></i>${task.category}
                                </span>
                            ` : ''}
                            <span class="badge ${getPriorityBadgeClass(task.priority)}" data-bs-toggle="tooltip" title="Prioridad">
                                <i class="bi ${getPriorityIconClass(task.priority)} me-1"></i>${getPriorityName(task.priority)}
                            </span>
                            <span class="badge ${getStatusBadgeClass(task.status)}" data-bs-toggle="tooltip" title="Estado">
                                <i class="bi ${getStatusIconClass(task.status)} me-1"></i>${getStatusName(task.status)}
                            </span>
                        </div>
                    </div>
                    <div class="quick-actions">
                        <div class="btn-group">
                            ${task.status !== 2 ? `
                                <button type="button" class="btn btn-success btn-sm quick-status-change" 
                                        data-task-id="${task.id}" data-status="2"
                                        data-bs-toggle="tooltip" title="Marcar como completada">
                                    <i class="bi bi-check-lg"></i>
                                </button>
                            ` : ''}
                            ${task.status === 0 ? `
                                <button type="button" class="btn btn-primary btn-sm quick-status-change" 
                                        data-task-id="${task.id}" data-status="1"
                                        data-bs-toggle="tooltip" title="Iniciar tarea">
                                    <i class="bi bi-play-fill"></i>
                                </button>
                            ` : ''}
                            <a href="/Tasks/Edit/${task.id}" class="btn btn-secondary btn-sm"
                               data-bs-toggle="tooltip" title="Editar tarea">
                                <i class="bi bi-pencil-square"></i>
                            </a>
                            <a href="/Tasks/Delete/${task.id}" class="btn btn-danger btn-sm"
                               data-bs-toggle="tooltip" title="Eliminar tarea">
                                <i class="bi bi-trash"></i>
                            </a>
                        </div>
                    </div>
                </div>
                
                ${task.notes ? `
                    <div class="task-notes">
                        <p class="card-text">${task.notes}</p>
                    </div>
                ` : ''}
                
                <div class="task-meta">
                    <i class="bi bi-calendar-event me-1"></i>Creada: ${formatDate(task.createdAt)}
                    ${task.dueDate ? `
                        <span class="ms-3">
                            <i class="bi bi-calendar-check me-1"></i>Vence: 
                            <span class="${task.isOverdue ? 'text-danger fw-bold' : ''}">
                                ${formatDate(task.dueDate)}
                                ${task.isOverdue ? `
                                    <i class="bi bi-exclamation-triangle-fill ms-1" data-bs-toggle="tooltip" title="¡Tarea vencida!"></i>
                                ` : ''}
                            </span>
                        </span>
                    ` : ''}
                </div>
                
                <div class="drag-handle mt-2 text-center text-muted">
                    <small class="d-block mb-1">Arrastra para cambiar el estado</small>
                    <i class="bi bi-grip-horizontal"></i>
                </div>
            </div>
        </div>
    `;
}

// Función para crear una fila de tarea para la vista de lista
function createTaskRow(task) {
    // Aquí generamos el HTML para una fila de tarea
    return `
        <tr class="${task.isOverdue ? 'table-danger' : ''}" data-task-id="${task.id}">
            <td>
                <div class="fw-bold">${task.description}</div>
                ${task.category ? `<span class="badge bg-info">${task.category}</span>` : ''}
            </td>
            <td>
                <span class="badge ${getStatusBadgeClass(task.status)}">${getStatusName(task.status)}</span>
            </td>
            <td>
                <span class="badge ${getPriorityBadgeClass(task.priority)}">${getPriorityName(task.priority)}</span>
            </td>
            <td>
                ${task.dueDate ? `
                    <span class="${task.isOverdue ? 'text-danger' : ''}">
                        ${formatDate(task.dueDate)}
                        ${task.isOverdue ? `
                            <i class="bi bi-exclamation-triangle-fill ms-1" data-bs-toggle="tooltip" title="¡Tarea vencida!"></i>
                        ` : ''}
                    </span>
                ` : `<span class="text-muted">-</span>`}
            </td>
            <td>
                <div class="btn-group btn-group-sm">
                    ${task.status !== 2 ? `
                        <button type="button" class="btn btn-success quick-status-change" 
                                data-task-id="${task.id}" data-status="2"
                                data-bs-toggle="tooltip" title="Marcar como completada">
                            <i class="bi bi-check-lg"></i>
                        </button>
                    ` : ''}
                    ${task.status === 0 ? `
                        <button type="button" class="btn btn-primary quick-status-change" 
                                data-task-id="${task.id}" data-status="1"
                                data-bs-toggle="tooltip" title="Iniciar tarea">
                            <i class="bi bi-play-fill"></i>
                        </button>
                    ` : ''}
                    <a href="/Tasks/Edit/${task.id}" class="btn btn-secondary"
                       data-bs-toggle="tooltip" title="Editar tarea">
                        <i class="bi bi-pencil-square"></i>
                    </a>
                    <a href="/Tasks/Delete/${task.id}" class="btn btn-danger"
                       data-bs-toggle="tooltip" title="Eliminar tarea">
                        <i class="bi bi-trash"></i>
                    </a>
                </div>
            </td>
        </tr>
    `;
}

// Función para actualizar la paginación
function updatePagination(pagination) {
    console.log('Actualizando paginación');
    
    const paginationContainer = document.querySelector('.pagination-container');
    if (!paginationContainer) {
        console.warn('No se encontró el contenedor de paginación');
        return;
    }
    
    // Crear la paginación
    let html = `
        <nav aria-label="Paginación de tareas">
            <ul class="pagination justify-content-center">
    `;
    
    // Botón anterior
    if (pagination.currentPage > 1) {
        html += `
            <li class="page-item">
                <a class="page-link" href="?page=${pagination.currentPage - 1}" aria-label="Anterior">
                    <span aria-hidden="true">&laquo;</span>
                </a>
            </li>
        `;
    } else {
        html += `
            <li class="page-item disabled">
                <a class="page-link" href="#" aria-label="Anterior">
                    <span aria-hidden="true">&laquo;</span>
                </a>
            </li>
        `;
    }
    
    // Calcular el número total de páginas
    const totalPages = Math.ceil(pagination.totalItems / pagination.pageSize);
    
    // Mostrar páginas
    for (let i = 1; i <= totalPages; i++) {
        if (i === pagination.currentPage) {
            html += `<li class="page-item active"><a class="page-link" href="#">${i}</a></li>`;
        } else {
            html += `<li class="page-item"><a class="page-link" href="?page=${i}">${i}</a></li>`;
        }
    }
    
    // Botón siguiente
    if (pagination.currentPage < totalPages) {
        html += `
            <li class="page-item">
                <a class="page-link" href="?page=${pagination.currentPage + 1}" aria-label="Siguiente">
                    <span aria-hidden="true">&raquo;</span>
                </a>
            </li>
        `;
    } else {
        html += `
            <li class="page-item disabled">
                <a class="page-link" href="#" aria-label="Siguiente">
                    <span aria-hidden="true">&raquo;</span>
                </a>
            </li>
        `;
    }
    
    html += `
            </ul>
        </nav>
    `;
    
    paginationContainer.innerHTML = html;
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
        case 0: return "Baja";
        case 1: return "Media";
        case 2: return "Alta";
        case 3: return "Urgente";
        default: return "Desconocida";
    }
}

function getStatusName(status) {
    switch (parseInt(status)) {
        case 0: return "Pendiente";
        case 1: return "En Progreso";
        case 2: return "Completada";
        default: return "Desconocido";
    }
}

// Función para formatear fechas
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString();
}

// Iniciar la carga de tareas cuando se carga la página
document.addEventListener('DOMContentLoaded', function() {
    console.log('Documento cargado, verificando si se deben cargar tareas...');
    
    // Verificar si debemos cargar tareas de forma asíncrona
    const loadingIndicator = document.getElementById('globalLoadingIndicator');
    if (loadingIndicator && !loadingIndicator.classList.contains('d-none')) {
        console.log('Iniciando carga asíncrona de tareas...');
        loadTasksAsynchronously();
    }
});
