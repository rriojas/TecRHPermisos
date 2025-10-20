$(document).ready(function () {
    console.log('Iniciando script de edición de permisos...');

    const formElement = $('#editPermissionForm');
    const isAdministrador = formElement.data('is-administrador') === true || formElement.data('is-administrador') === 'True';
    const isAutorizador = formElement.data('is-autorizador') === true || formElement.data('is-autorizador') === 'True';

    console.log('Roles:', { isAdministrador, isAutorizador });

    // Configuración de tipos de permiso
    const typeConfigurations = {
        2: { // Falta
            fecha1: { show: true, label: "Del día", required: true },
            fecha2: { show: true, label: "Al día", required: true },
            hora1: { show: false, label: "Hora entrada", required: false },
            hora2: { show: false, label: "Hora salida", required: false }
        },
        3: { // Retardo
            fecha1: { show: true, label: "En el día", required: true },
            fecha2: { show: false, label: "Al día", required: false },
            hora1: { show: true, label: "Hora de llegada", required: true },
            hora2: { show: false, label: "Hora salida", required: false }
        },
        4: { // Cambio de Horario
            fecha1: { show: true, label: "Del día", required: true },
            fecha2: { show: true, label: "Al día", required: true },
            hora1: { show: true, label: "Hora entrada", required: true },
            hora2: { show: true, label: "Hora salida", required: true }
        },
        5: { // Turno por Turno
            fecha1: { show: true, label: "Del día", required: true },
            fecha2: { show: true, label: "Por el día", required: true },
            hora1: { show: true, label: "Hora entrada", required: true },
            hora2: { show: true, label: "Hora salida", required: true }
        }
    };

    // Obtener el tipo de permiso prellenado
    const currentType = parseInt($('input[name="IdTipoPermiso"]').val());
    const config = typeConfigurations[currentType];

    // Mostrar secciones desde el inicio
    $('#datetime-section').show();
    $('#reason-section').show();
    $('#evidence-section').show();

    // Configurar campos según el tipo prellenado
    if (config) {
        updateFieldVisibility('fecha1Date', config.fecha1);
        updateFieldVisibility('fecha2Date', config.fecha2);
        updateFieldVisibility('hora1Time', config.hora1);
        updateFieldVisibility('hora2Time', config.hora2);
    }

    // Función para mostrar/ocultar campos
    function updateFieldVisibility(fieldName, config) {
        const group = $(`#${fieldName}-group`);
        const label = $(`#${fieldName}-label`);
        const input = group.find('input');

        if (config && config.show) {
            group.show();
            label.text(config.label);
            input.prop('required', config.required);

            // Mostrar/ocultar asterisco rojo
            const requiredSpan = group.find('.required');
            if (config.required) {
                requiredSpan.show();
            } else {
                requiredSpan.hide();
            }
        } else {
            group.hide();
            input.prop('required', false).val('');
        }
    }

    // Contador de caracteres para el motivo
    const motivoTextarea = $('textarea[name="Motivo"]');
    const charCount = $('#char-count');

    motivoTextarea.on('input keyup paste', function () {
        const currentLength = $(this).val().length;
        const maxLength = 255;

        charCount.text(currentLength);

        // Cambiar color según la cantidad
        const counter = $('.char-counter');
        counter.removeClass('error warning success');

        if (currentLength > maxLength) {
            counter.addClass('error');
        } else if (currentLength > maxLength * 0.9) {
            counter.addClass('warning');
        } else {
            counter.addClass('success');
        }

        validateForm();
    });

    // Manejo de archivos PDF
    $('#evidenceFile').on('change', function () {
        const file = this.files[0];
        const filePreview = $('#filePreview');
        const fileUploadContent = $('.file-upload-content');

        if (file) {
            // Validar tipo de archivo
            if (file.type !== 'application/pdf') {
                alert('Solo se permiten archivos PDF.');
                this.value = '';
                return;
            }

            // Validar tamaño (5MB)
            if (file.size > 5 * 1024 * 1024) {
                alert('El archivo no debe superar los 5MB.');
                this.value = '';
                return;
            }

            // Mostrar preview
            $('#fileName').text(file.name);
            filePreview.show();
            fileUploadContent.hide();
        } else {
            filePreview.hide();
            fileUploadContent.show();
        }
    });

    // Remover archivo
    $('#removeFile').on('click', function () {
        $('#evidenceFile').val('');
        $('#filePreview').hide();
        $('.file-upload-content').show();
    });

    // Validación del formulario
    function validateForm() {
        let isValid = true;

        // Verificar motivo
        const motivo = motivoTextarea.val().trim();
        if (!motivo || motivo.length === 0) {
            isValid = false;
        }

        // Verificar campos requeridos según el tipo
        if (config) {
            if (config.fecha1.required && !$('input[name="Fecha1Date"]').val()) {
                isValid = false;
            }
            if (config.fecha2.required && !$('input[name="Fecha2Date"]').val()) {
                isValid = false;
            }
            if (config.hora1.required && !$('input[name="Hora1Time"]').val()) {
                isValid = false;
            }
            if (config.hora2.required && !$('input[name="Hora2Time"]').val()) {
                isValid = false;
            }
        }

        // Habilitar/deshabilitar botón de envío
        $('#submitBtn').prop('disabled', !isValid);
        return isValid;
    }

    // Combinar fecha y hora en campos ocultos
    function combineDateTime() {
        if (config) {
            // Fecha1
            if (config.fecha1.show) {
                const date1 = $('input[name="Fecha1Date"]').val();
                const time1 = $('input[name="Hora1Time"]').val();
                let combined1 = date1;
                if (time1 && config.hora1.show) {
                    combined1 = `${date1}T${time1}:00`;
                }
                $('input[name="Fecha1"]').val(combined1);
            }

            // Fecha2
            if (config.fecha2.show) {
                const date2 = $('input[name="Fecha2Date"]').val();
                const time2 = $('input[name="Hora2Time"]').val();
                let combined2 = date2;
                if (time2 && config.hora2.show) {
                    combined2 = `${date2}T${time2}:00`;
                }
                $('input[name="Fecha2"]').val(combined2);
            }
        }
    }

    // Event listeners para validación en tiempo real
    $('input, textarea').on('input change', function () {
        setTimeout(function () {
            combineDateTime();
            validateForm();
        }, 100);
    });

    // Submit del formulario
    $('#editPermissionForm').on('submit', function (e) {
        e.preventDefault();

        // Combinar fechas y horas antes de enviar
        combineDateTime();

        if (!validateForm()) {
            alert('Por favor complete todos los campos obligatorios.');
            return;
        }

        const submitBtn = $('#submitBtn');
        const originalText = submitBtn.html();

        // Deshabilitar botón y mostrar loading
        submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Guardando...');

        // Crear FormData para envío
        const formData = new FormData(this);

        // Enviar por AJAX
        $.ajax({
            url: '/Permisos/Edit/' + $('#editPermissionForm input[name="Id"]').val(),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    alert('Permiso modificado exitosamente');
                    window.location.href = '/Permisos/Index';
                } else {
                    alert('Error: ' + (response.message || 'Error desconocido'));
                    submitBtn.prop('disabled', false).html(originalText);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error:', error);
                alert('Error al enviar la solicitud. Intente nuevamente.');
                submitBtn.prop('disabled', false).html(originalText);
            }
        });
    });

    // Validación inicial
    validateForm();
    combineDateTime(); // Asegurar que los campos ocultos estén inicializados

    console.log('Script de edición de permisos inicializado correctamente');
});