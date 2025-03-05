// Función para cambiar entre vistas de tareas
document.addEventListener('DOMContentLoaded', function() {
    console.log('viewSwitcher.js cargado');
    
    // Obtener referencias a los elementos DOM
    const cardViewBtn = document.getElementById('cardViewBtn');
    const listViewBtn = document.getElementById('listViewBtn');
    const kanbanViewBtn = document.getElementById('kanbanViewBtn');
    const cardView = document.getElementById('cardView');
    const listView = document.getElementById('listView');
    const kanbanView = document.getElementById('kanbanView');
    
    console.log('Elementos de vista:', {
        cardViewBtn: cardViewBtn ? 'encontrado' : 'no encontrado',
        listViewBtn: listViewBtn ? 'encontrado' : 'no encontrado',
        kanbanViewBtn: kanbanViewBtn ? 'encontrado' : 'no encontrado',
        cardView: cardView ? 'encontrado' : 'no encontrado',
        listView: listView ? 'encontrado' : 'no encontrado',
        kanbanView: kanbanView ? 'encontrado' : 'no encontrado'
    });
    
    if (cardViewBtn && listViewBtn && kanbanViewBtn && cardView && listView && kanbanView) {
        // Función para cambiar entre vistas
        function switchView(viewType) {
            console.log('Cambiando a vista:', viewType);
            
            try {
                // Ocultar todas las vistas
                hideAllViews();
                
                // Desactivar todos los botones
                deactivateAllButtons();
                
                // Mostrar la vista seleccionada
                if (viewType === 'card') {
                    showCardView();
                } else if (viewType === 'list') {
                    showListView();
                } else if (viewType === 'kanban') {
                    showKanbanView();
                }
                
                // Guardar preferencia
                localStorage.setItem('taskViewPreference', viewType);
                console.log('Preferencia guardada:', viewType);
                
                // Mostrar información de depuración
                logViewState();
            } catch (error) {
                console.error('Error al cambiar de vista:', error);
            }
        }
        
        // Función para ocultar todas las vistas
        function hideAllViews() {
            document.querySelectorAll('.view-container').forEach(view => {
                // Quitar la clase active
                view.classList.remove('active');
                // Ocultar explícitamente con estilo inline
                view.style.display = 'none';
                // Añadir clase para ocultar
                view.classList.add('d-none');
            });
        }
        
        // Función para desactivar todos los botones
        function deactivateAllButtons() {
            cardViewBtn.classList.remove('active');
            listViewBtn.classList.remove('active');
            kanbanViewBtn.classList.remove('active');
        }
        
        // Función para mostrar la vista de tarjetas
        function showCardView() {
            // Mostrar vista de tarjetas
            cardView.classList.add('active');
            cardView.style.display = 'block';
            cardView.classList.remove('d-none');
            cardViewBtn.classList.add('active');
            console.log('Vista de tarjetas activada');
            
            // Verificar que se ha aplicado correctamente
            setTimeout(() => {
                if (window.getComputedStyle(cardView).display === 'none') {
                    console.warn('La vista de tarjetas sigue oculta a pesar de los cambios');
                    // Forzar visualización
                    cardView.setAttribute('style', 'display: block !important');
                }
            }, 50);
        }
        
        // Función para mostrar la vista de lista
        function showListView() {
            // Mostrar vista de lista
            listView.classList.add('active');
            listView.style.display = 'block';
            listView.classList.remove('d-none');
            listViewBtn.classList.add('active');
            console.log('Vista de lista activada');
            
            // Verificar que se ha aplicado correctamente
            setTimeout(() => {
                if (window.getComputedStyle(listView).display === 'none') {
                    console.warn('La vista de lista sigue oculta a pesar de los cambios');
                    // Forzar visualización
                    listView.setAttribute('style', 'display: block !important');
                }
            }, 50);
        }
        
        // Función para mostrar la vista kanban
        function showKanbanView() {
            // Mostrar vista kanban
            kanbanView.classList.add('active');
            kanbanView.style.display = 'block';
            kanbanView.classList.remove('d-none');
            kanbanViewBtn.classList.add('active');
            console.log('Vista kanban activada');
            
            // Verificar que se ha aplicado correctamente
            setTimeout(() => {
                if (window.getComputedStyle(kanbanView).display === 'none') {
                    console.warn('La vista kanban sigue oculta a pesar de los cambios');
                    // Forzar visualización
                    kanbanView.setAttribute('style', 'display: block !important');
                }
            }, 50);
            
            // Configurar drag & drop para la vista kanban
            setTimeout(() => {
                if (typeof setupDragAndDrop === 'function') {
                    setupDragAndDrop();
                }
            }, 100);
        }
        
        // Función para mostrar información de depuración
        function logViewState() {
            console.log('Estado actual de las vistas:', {
                cardView: {
                    classList: cardView.classList.toString(),
                    style: cardView.style.display,
                    computed: window.getComputedStyle(cardView).display
                },
                listView: {
                    classList: listView.classList.toString(),
                    style: listView.style.display,
                    computed: window.getComputedStyle(listView).display
                },
                kanbanView: {
                    classList: kanbanView.classList.toString(),
                    style: kanbanView.style.display,
                    computed: window.getComputedStyle(kanbanView).display
                }
            });
        }
        
        // Configurar event listeners
        cardViewBtn.addEventListener('click', function(e) {
            e.preventDefault();
            console.log('Botón de vista de tarjetas clickeado');
            switchView('card');
        });
        
        listViewBtn.addEventListener('click', function(e) {
            e.preventDefault();
            console.log('Botón de vista de lista clickeado');
            switchView('list');
        });
        
        kanbanViewBtn.addEventListener('click', function(e) {
            e.preventDefault();
            console.log('Botón de vista kanban clickeado');
            switchView('kanban');
        });
        
        // Cargar preferencia guardada o usar kanban por defecto
        const savedView = localStorage.getItem('taskViewPreference') || 'kanban';
        console.log('Cargando preferencia guardada:', savedView);
        switchView(savedView);
    } else {
        console.error('No se encontraron todos los elementos necesarios para el cambio de vista');
    }
});
