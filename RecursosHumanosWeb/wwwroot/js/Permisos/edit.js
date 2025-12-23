// edit.js - Script para el formulario de Editar Permiso

(function () {
    'use strict';

    console.log('🚀 Edit Permiso script loaded');

    // --- ELEMENTOS DEL DOM ---
    const form = document.getElementById('editPermissionForm');
    if (!form) {
        console.error('❌ Formulario no encontrado');
        return;
    }

    // Get initial tipo from data attribute (pre-filled value)
    let tipoPermisoActual = parseInt(form.dataset.tipoPermiso);
    console.log('📋 Tipo de Permiso Inicial:', tipoPermisoActual);

    const elements = {
        form: form,
        tipoPermisoRadios: document.querySelectorAll('input[name="IdTipoPermiso"]'),
        fecha1Date: document.getElementById('Fecha1Date'),
        hora1Time: document.getElementById('Hora1Time'),
        fecha2Date: document.getElementById('Fecha2Date'),
        hora2Time: document.getElementById('Hora2Time'),
        fecha1Hidden: document.querySelector('input[name="Fecha1"]'),
        fecha2Hidden: document.querySelector('input[name="Fecha2"]'),
        motivo: document.getElementById('Motivo'),
        charCount: document.getElementById('char-count'),
        submitBtn: document.getElementById('submitBtn'),
        // File upload elements
        evidenceFile: document.getElementById('evidenceFile'),
        fileUploadArea: document.getElementById('fileUploadArea'),
        fileUploadContent: document.getElementById('fileUploadContent'),
        filePreview: document.getElementById('filePreview'),
        fileName: document.getElementById('fileName'),
        removeFile: document.getElementById('removeFile'),
        evidenciaHidden: document.querySelector('input[name="Evidencia"]')
    };

    // --- CONFIGURACIÓN DE CAMPOS SEGÚN TIPO DE PERMISO ---
    function configurarCamposSegunTipo(tipoPermiso) {
        const config = {
            2: { // Falta
                fecha1Label: 'Fecha de inicio',
                fecha2Label: 'Fecha de fin',
                hora1Visible: false,
                hora2Visible: false,
                fecha2Required: true
            },
            3: { // Retardo
                fecha1Label: 'Fecha del retardo',
                fecha2Label: 'Fecha de fin',
                hora1Visible: true,
                hora2Visible: false,
                fecha2Required: false
            },
            4: { // Cambio de horario
                fecha1Label: 'Fecha de inicio',
                fecha2Label: 'Fecha de fin',
                hora1Visible: true,
                hora2Visible: true,
                fecha2Required: true
            },
            5: { // Turno por turno
                fecha1Label: 'Fecha de inicio',
                fecha2Label: 'Fecha de fin',
                hora1Visible: false,
                hora2Visible: false,
                fecha2Required: true
            }
        };

        const currentConfig = config[tipoPermiso];
        if (!currentConfig) return;

        // Actualizar labels
        document.getElementById('fecha1Date-label').textContent = currentConfig.fecha1Label;
        document.getElementById('fecha2Date-label').textContent = currentConfig.fecha2Label;

        // Mostrar/ocultar campos de hora
        const hora1Group = document.getElementById('hora1Time-group');
        const hora2Group = document.getElementById('hora2Time-group');
        
        hora1Group.style.display = currentConfig.hora1Visible ? 'block' : 'none';
        hora2Group.style.display = currentConfig.hora2Visible ? 'block' : 'none';

        // Limpiar valores de horas si se ocultan
        if (!currentConfig.hora1Visible && elements.hora1Time) {
            elements.hora1Time.value = '';
        }
        if (!currentConfig.hora2Visible && elements.hora2Time) {
            elements.hora2Time.value = '';
        }

        // Configurar required en Fecha2
        const fecha2Group = document.getElementById('fecha2Date-group');
        fecha2Group.style.display = tipoPermiso === 3 ? 'none' : 'block';
        
        if (elements.fecha2Date) {
            elements.fecha2Date.required = currentConfig.fecha2Required;
            if (!currentConfig.fecha2Required) {
                elements.fecha2Date.value = '';
            }
        }

        console.log(`✅ Campos configurados para tipo ${tipoPermiso}`);
    }

    // --- MANEJAR CAMBIO DE TIPO DE PERMISO ---
    function handleTipoPermisoChange(e) {
        const nuevoTipo = parseInt(e.target.value);
        console.log(`🔄 Tipo de permiso cambiado: ${tipoPermisoActual} → ${nuevoTipo}`);
        
        tipoPermisoActual = nuevoTipo;
        form.dataset.tipoPermiso = nuevoTipo;
        
        // Reconfigurar campos
        configurarCamposSegunTipo(nuevoTipo);
        
        // Agregar clase activa a la tarjeta seleccionada
        document.querySelectorAll('.permission-type-card').forEach(card => {
            card.classList.remove('active');
        });
        e.target.closest('.permission-type-card').classList.add('active');
    }

    // --- COMBINAR FECHA Y HORA EN CAMPOS HIDDEN ---
    function combinarFechaHora() {
        if (elements.fecha1Date.value) {
            const fecha1 = new Date(elements.fecha1Date.value + 'T00:00:00');
            if (elements.hora1Time && elements.hora1Time.value && elements.hora1Time.offsetParent !== null) {
                const [hours, minutes] = elements.hora1Time.value.split(':');
                fecha1.setHours(parseInt(hours), parseInt(minutes), 0);
            }
            elements.fecha1Hidden.value = fecha1.toISOString();
        }

        if (tipoPermisoActual !== 3 && elements.fecha2Date && elements.fecha2Date.value) {
            const fecha2 = new Date(elements.fecha2Date.value + 'T00:00:00');
            if (elements.hora2Time && elements.hora2Time.value && elements.hora2Time.offsetParent !== null) {
                const [hours, minutes] = elements.hora2Time.value.split(':');
                fecha2.setHours(parseInt(hours), parseInt(minutes), 0);
            } else {
                fecha2.setHours(23, 59, 59);
            }
            elements.fecha2Hidden.value = fecha2.toISOString();
        }
    }

    // --- CONTADOR DE CARACTERES ---
    function updateCharCount() {
        const count = elements.motivo.value.length;
        elements.charCount.textContent = count;
        
        if (count > 255) {
            elements.charCount.style.color = '#ef4444';
        } else if (count > 200) {
            elements.charCount.style.color = '#f59e0b';
        } else {
            elements.charCount.style.color = '#6b7280';
        }
    }

    // --- MANEJO DE ARCHIVO ---
    function handleFileSelect(e) {
        const file = e.target.files[0];
        if (!file) return;

        // Validar tamaño (5MB)
        if (file.size > 5 * 1024 * 1024) {
            Swal.fire({
                icon: 'error',
                title: 'Archivo muy grande',
                text: 'El archivo no debe superar los 5MB'
            });
            e.target.value = '';
            return;
        }

        // Validar tipo
        const allowedTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];
        if (!allowedTypes.includes(file.type)) {
            Swal.fire({
                icon: 'error',
                title: 'Tipo de archivo no válido',
                text: 'Solo se permiten archivos PDF, JPG y PNG'
            });
            e.target.value = '';
            return;
        }

        // Mostrar preview
        elements.fileName.textContent = file.name;
        elements.fileUploadContent.style.display = 'none';
        elements.filePreview.style.display = 'block';
    }

    function removeFileHandler() {
        elements.evidenceFile.value = '';
        elements.evidenciaHidden.value = '';
        elements.fileName.textContent = '';
        elements.filePreview.style.display = 'none';
        elements.fileUploadContent.style.display = 'block';
    }

    // --- ENVÍO DEL FORMULARIO ---
    function handleFormSubmit(e) {
        e.preventDefault();
        
        // Validar que haya un tipo seleccionado
        if (!tipoPermisoActual) {
            Swal.fire({
                icon: 'warning',
                title: 'Tipo requerido',
                text: 'Debe seleccionar un tipo de permiso'
            });
            return;
        }

        // Validar campos requeridos
        if (!elements.fecha1Date.value) {
            Swal.fire({
                icon: 'warning',
                title: 'Campo requerido',
                text: 'La fecha de inicio es obligatoria'
            });
            return;
        }

        if (tipoPermisoActual !== 3 && !elements.fecha2Date.value) {
            Swal.fire({
                icon: 'warning',
                title: 'Campo requerido',
                text: 'La fecha de fin es obligatoria para este tipo de permiso'
            });
            return;
        }

        if (!elements.motivo.value.trim()) {
            Swal.fire({
                icon: 'warning',
                title: 'Campo requerido',
                text: 'El motivo es obligatorio'
            });
            return;
        }

        // Combinar fechas y horas
        combinarFechaHora();

        // Deshabilitar botón
        elements.submitBtn.disabled = true;
        elements.submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Guardando...';

        // Crear FormData
        const formData = new FormData(elements.form);

        // Obtener token
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Enviar petición
        fetch(elements.form.action, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        })
        .then(response => {
            if (!response.ok) throw new Error('Error HTTP: ' + response.status);
            return response.json();
        })
        .then(data => {
            if (data.success) {
                Swal.fire({
                    icon: 'success',
                    title: data.title || '¡Éxito!',
                    text: data.message || 'Permiso actualizado exitosamente',
                    showConfirmButton: false,
                    timer: 2000
                }).then(() => {
                    if (data.redirectUrl) {
                        window.location.href = data.redirectUrl;
                    } else {
                        window.location.href = '/Permisos/Index';
                    }
                });
            } else {
                // Manejar errores de validación
                if (data.errors) {
                    let errorHtml = '<ul style="text-align: left;">';
                    for (const field in data.errors) {
                        data.errors[field].forEach(error => {
                            errorHtml += `<li>${error}</li>`;
                        });
                    }
                    errorHtml += '</ul>';
                    
                    Swal.fire({
                        icon: 'error',
                        title: data.title || 'Error de Validación',
                        html: errorHtml
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: data.title || 'Error',
                        text: data.message || 'No se pudo actualizar el permiso'
                    });
                }
            }
        })
        .catch(error => {
            console.error('Error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error del servidor',
                text: error.message || 'No se pudo conectar con el servidor'
            });
        })
        .finally(() => {
            elements.submitBtn.disabled = false;
            elements.submitBtn.innerHTML = '<i class="fas fa-save"></i> Guardar Cambios';
        });
    }

    // --- INICIALIZACIÓN ---
    function init() {
        console.log('🔧 Inicializando formulario de edición');

        // Configurar campos según tipo inicial
        configurarCamposSegunTipo(tipoPermisoActual);

        // Marcar la tarjeta activa inicial
        const radioChecked = document.querySelector(`input[name="IdTipoPermiso"]:checked`);
        if (radioChecked) {
            radioChecked.closest('.permission-type-card').classList.add('active');
        }

        // Event listeners para cambio de tipo
        elements.tipoPermisoRadios.forEach(radio => {
            radio.addEventListener('change', handleTipoPermisoChange);
        });

        // Event listeners
        elements.motivo.addEventListener('input', updateCharCount);
        if (elements.evidenceFile) elements.evidenceFile.addEventListener('change', handleFileSelect);
        if (elements.removeFile) elements.removeFile.addEventListener('click', removeFileHandler);
        elements.form.addEventListener('submit', handleFormSubmit);

        // Event listeners para fechas/horas
        [elements.fecha1Date, elements.hora1Time, elements.fecha2Date, elements.hora2Time].forEach(el => {
            if (el) {
                el.addEventListener('change', combinarFechaHora);
            }
        });

        // Inicializar contador de caracteres
        updateCharCount();

        // Drag & Drop para archivo
        if (elements.fileUploadArea) {
            elements.fileUploadArea.addEventListener('dragover', (e) => {
                e.preventDefault();
                elements.fileUploadArea.style.borderColor = '#3b82f6';
            });

            elements.fileUploadArea.addEventListener('dragleave', () => {
                elements.fileUploadArea.style.borderColor = '#d1d5db';
            });

            elements.fileUploadArea.addEventListener('drop', (e) => {
                e.preventDefault();
                elements.fileUploadArea.style.borderColor = '#d1d5db';
                const files = e.dataTransfer.files;
                if (files.length > 0) {
                    elements.evidenceFile.files = files;
                    handleFileSelect({ target: elements.evidenceFile });
                }
            });
        }

        console.log('✅ Formulario inicializado correctamente');
    }

    // Ejecutar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();