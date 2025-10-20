$(document).ready(function () {
    let table = new DataTable('#permisosTable', {
        language: {
            url: '//cdn.datatables.net/plug-ins/2.3.4/i18n/es-ES.json'
        },
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, 'Todos']],
        order: [[2, 'desc']],
        orderMulti: true,
        columnDefs: [
            { orderable: false, targets: 4 },
            {
                targets: [0, 1, 2, 3],
                dtcc: { searchable: true, orderable: true }
            }
        ],
        layout: {
            topStart: {
                pageLength: { menu: [10, 25, 50, 100, -1] }
            },
            topEnd: 'search',
            bottomStart: 'info',
            bottomEnd: 'paging'
        },
        searchBuilder: {
            columns: [0, 1, 2, 3],
            conditions: {
                string: {
                    startsWith: { conditionName: 'Empieza con' },
                    endsWith: { conditionName: 'Termina con' },
                    contains: { conditionName: 'Contiene' },
                    notContains: { conditionName: 'No contiene' }
                }
            }
        }
        // 🔹 initComplete eliminado (inputs en footer)
    });

    table.on('draw', function () {
        toggleEmptyState(table.rows({ search: 'applied', filter: 'applied' }).count() === 0);
    });
});

function clearAllFilters() {
    document.getElementById('nombreUsuario') && (document.getElementById('nombreUsuario').value = '');
    document.getElementById('idArea') && (document.getElementById('idArea').value = '');
    document.getElementById('searchForm').submit();
    showNotification('✅ Todos los filtros han sido limpiados', 'success');
}

function showNotification(message, type = 'info') {
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer);
            toast.addEventListener('mouseleave', Swal.resumeTimer);
        },
        customClass: {
            popup: 'swal-popup-custom',
            title: 'swal-title-custom'
        }
    });
    Toast.fire({ icon: type, title: message });
}

function toggleEmptyState(show) {
    let emptyState = document.querySelector('.empty-state');
    const tableContainer = document.querySelector('.table-container');
    if (show && !emptyState) {
        emptyState = document.createElement('div');
        emptyState.className = 'empty-state fade-in';
        emptyState.innerHTML = `
                    <div class="empty-icon"><i class="fas fa-file-alt"></i></div>
                    <h3>No hay permisos registrados</h3>
                    <p class="text-muted">Intenta ajustar los filtros de búsqueda</p>
                `;
        tableContainer.parentNode.insertBefore(emptyState, tableContainer);
    }
    if (emptyState) emptyState.style.display = show ? 'block' : 'none';
    tableContainer.style.display = show ? 'none' : 'block';
}

document.addEventListener('DOMContentLoaded', function () {
    const elements = document.querySelectorAll('.fade-in, .slide-in-left');
    elements.forEach(element => {
        element.style.opacity = '0';
        element.style.transform = element.classList.contains('slide-in-left') ? 'translateX(-30px)' : 'translateY(20px)';
    });
    setTimeout(() => {
        elements.forEach((element, index) => {
            setTimeout(() => {
                element.style.transition = 'all 0.6s ease-out';
                element.style.opacity = '1';
                element.style.transform = 'translate(0, 0)';
            }, index * 150);
        });
    }, 100);
});