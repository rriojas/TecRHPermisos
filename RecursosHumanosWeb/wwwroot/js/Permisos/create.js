$(document).ready(function () {
    console.log('Iniciando script de permisos...');

    // --- 1. CONFIGURACIÓN INICIAL DE ROLES Y ELEMENTOS ---
    const formElement = $('#permissionForm');
    const isRH = formElement.data('is-rh') === true || formElement.data('is-rh') === 'True';
    const isAdministrador = formElement.data('is-administrador') === true || formElement.data('is-administrador') === 'True';
    // const isEmpleado = formElement.data('is-empleado') === true || formElement.data('is-empleado') === 'True'; // No necesario para esta lógica

    console.log('Roles:', { isRH, isAdministrador });

    // Elemento select (Select2) para usuario solicitante — el select2 se inicializa en la vista Create.cshtml
    const $idUsuarioSolicita = $("#IdUsuarioSolicita");

    // --- 3. CONFIGURACIÓN DE TIPOS DE PERMISO (LÓGICA EXISTENTE) ---
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

    // Event handler para cambio de tipo de permiso
    $('input[name="IdTipoPermiso"]').on('change', function () {
        console.log('Tipo seleccionado:', $(this).val());

        const selectedType = $(this).val();
        const config = typeConfigurations[selectedType];

        if (config) {
            // Mostrar las secciones siguientes
            $('#datetime-section').slideDown(300);
            $('#reason-section').slideDown(300);
            $('#evidence-section').slideDown(300);

            // Configurar campos según el tipo
            updateFieldVisibility('fecha1Date', config.fecha1);
            updateFieldVisibility('fecha2Date', config.fecha2);
            updateFieldVisibility('hora1Time', config.hora1);
            updateFieldVisibility('hora2Time', config.hora2);
        }

        // Actualizar estilos visuales
        $('.permission-type-card').removeClass('selected');
        $(this).closest('.permission-type-card').addClass('selected');

        validateForm();
    });

    // Función para mostrar/ocultar campos
    function updateFieldVisibility(fieldName, config) {
        const group = $(`#${fieldName}-group`);
        const label = $(`#${fieldName}-label`);
        const input = group.find('input');

        if (config && config.show) {
            group.slideDown(200);
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
            group.slideUp(200);
            input.prop('required', false).val('');
        }
    }

    // --- 4. CONTADOR DE CARACTERES Y ARCHIVOS (LÓGICA EXISTENTE) ---
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
                // Usar el manejador centralizado para mostrar alertas
                handleAlertResponse({ success: false, title: 'Error de Archivo', message: 'Solo se permiten archivos PDF.', icon: 'error' });
                this.value = '';
                filePreview.hide();
                fileUploadContent.show();
                return;
            }

            // Validar tamaño (5MB)
            if (file.size > 5 * 1024 * 1024) {
                handleAlertResponse({ success: false, title: 'Error de Archivo', message: 'El archivo no debe superar los 5MB.', icon: 'error' });
                this.value = '';
                filePreview.hide();
                fileUploadContent.show();
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
        validateForm();
    });

    // Remover archivo
    $('#removeFile').on('click', function () {
        $('#evidenceFile').val('');
        $('#filePreview').hide();
        $('.file-upload-content').show();
        validateForm();
    });

    // --- 5. VALIDACIÓN Y MANEJO DEL FORMULARIO (LÓGICA EXISTENTE) ---

    // Validación del formulario
    function validateForm() {
        let isValid = true;

        // 5.1. Verificar Usuario Solicitante (solo para RH/Admin)
        if (isRH || isAdministrador) {
            if (!$idUsuarioSolicita.val() || $idUsuarioSolicita.val().length === 0) {
                isValid = false;
            }
        }

        // 5.2. Verificar que se haya seleccionado un tipo
        const selectedType = $('input[name="IdTipoPermiso"]:checked').val();
        if (!selectedType) {
            isValid = false;
        }

        // 5.3. Verificar motivo
        const motivo = motivoTextarea.val() ? motivoTextarea.val().trim() : '';
        if (!motivo || motivo.length > 255) {
            isValid = false;
        }

        // 5.4. Verificar campos requeridos según el tipo
        const config = selectedType && typeConfigurations[selectedType] ? typeConfigurations[selectedType] : null;
        if (config) {

            // Validar fechas y horas
            if (config.fecha1.required && !$('input[name="Fecha1Date"]').val()) { isValid = false; }
            if (config.fecha2.required && !$('input[name="Fecha2Date"]').val()) { isValid = false; }
            if (config.hora1.required && !$('input[name="Hora1Time"]').val()) { isValid = false; }
            if (config.hora2.required && !$('input[name="Hora2Time"]').val()) { isValid = false; }

            // Validación lógica de Fechas (Fecha1 no debe ser posterior a Fecha2)
            const date1Val = $('input[name="Fecha1Date"]').val();
            const date2Val = $('input[name="Fecha2Date"]').val();
            if (config.fecha1.show && config.fecha2.show && date1Val && date2Val) {
                if (new Date(date1Val) > new Date(date2Val)) {
                    // Mostrar error visual aquí (p. ej., span de validación)
                    isValid = false;
                }
            }
        }

        // Habilitar/deshabilitar botón de envío
        $('#submitBtn').prop('disabled', !isValid);
        return isValid;
    }

    // Combinar fecha y hora en campos ocultos (para el Model Binder de ASP.NET)
    function combineDateTime() {
        const selectedType = $('input[name="IdTipoPermiso"]:checked').val();
        if (!selectedType) return;

        const config = typeConfigurations[selectedType];

        // Fecha1
        if (config.fecha1.show) {
            const date1 = $('input[name="Fecha1Date"]').val();
            const time1 = $('input[name="Hora1Time"]').val() || '00:00'; // Valor por defecto si no se pide hora

            let combined1 = date1;
            // Si la hora es requerida o visible, combinamos fecha y hora
            if (config.hora1.show && $('input[name="Hora1Time"]').val()) {
                combined1 = `${date1}T${time1}:00`;
            } else if (date1) {
                combined1 = `${date1}T00:00:00`; // Solo fecha
            }
            $('input[name="Fecha1"]').val(combined1);
        } else {
            $('input[name="Fecha1"]').val('');
        }

        // Fecha2
        if (config.fecha2.show) {
            const date2 = $('input[name="Fecha2Date"]').val();
            const time2 = $('input[name="Hora2Time"]').val() || '00:00';

            let combined2 = date2;
            if (config.hora2.show && $('input[name="Hora2Time"]').val()) {
                combined2 = `${date2}T${time2}:00`;
            } else if (date2) {
                combined2 = `${date2}T00:00:00`;
            }
            $('input[name="Fecha2"]').val(combined2);
        } else {
            $('input[name="Fecha2"]').val('');
        }
    }

    // Event listeners para validación en tiempo real
    $('input, textarea, select').on('input change', function () {
        // Usamos un pequeño timeout para asegurar que el DOM se actualice antes de validar
        setTimeout(function () {
            combineDateTime();
            validateForm();
        }, 100);
    });

    // Submit del formulario
    $('#permissionForm').on('submit', function (e) {
        e.preventDefault();

        // 1. Validar final
        if (!validateForm()) {
            alert('Por favor complete todos los campos obligatorios y revise las fechas/motivo.');
            return;
        }

        // 2. Preparar el envío
        const submitBtn = $('#submitBtn');
        const originalText = submitBtn.html();

        submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Enviando...');

        const formData = new FormData(this);

        // 3. Enviar por AJAX
        $.ajax({
            url: $(this).attr('action'), // Usar la URL de la acción del formulario
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                // Delegar el manejo de UI y redirección al manejador central
                Promise.resolve(handleAlertResponse(response)).then(() => {
                    // Si no hubo redirección y la respuesta indica fallo, reactivar el botón
                    if (!response.success) {
                        submitBtn.prop('disabled', false).html(originalText);
                    }
                });
            },
            error: function (xhr, status, error) {
                console.error('Error:', error);
                handleAlertResponse({ success: false, title: 'Error de Conexión', message: 'Error de conexión o servidor. Intente nuevamente.', icon: 'error' });
                submitBtn.prop('disabled', false).html(originalText);
            }
        });
    });

    // Validación inicial al cargar la página (para el modo Edit)
    validateForm();

    // Disparar el cambio inicial si hay un tipo seleccionado (para modo Edit)
    $('input[name="IdTipoPermiso"]:checked').trigger('change');

    console.log('Script de permisos inicializado correctamente');
});