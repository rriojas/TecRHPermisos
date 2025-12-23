// site.js

// ----------------------------------------------------------------------
// 1. FUNCIONES AUXILIARES PARA MANEJO DE ALERTA Y VALIDACIÓN
// ----------------------------------------------------------------------

/**
 * Limpia los mensajes de error de validación previos en el formulario.
 */
function clearValidationErrors() {
    // Elimina todos los mensajes de error dinámicos
    $(".validation-error-message").remove();
    // Elimina la clase de error de Bootstrap de todos los controles
    $(".form-control").removeClass("is-invalid");
    $(".form-select").removeClass("is-invalid");
    $("[data-valmsg-for]").empty(); // Limpia mensajes de error del helper MVC
}

// Helper global para mostrar toasts rápidos usando SweetAlert2.
// Uso: showToast('Mensaje', 'success'|'info'|'warning'|'error')
function showToast(message, type = 'info') {
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer);
            toast.addEventListener('mouseleave', Swal.resumeTimer);
        }
    });
    Toast.fire({ icon: type, title: message });
}

/**
 * Procesa la respuesta del servidor (AlertResponseDTO) y maneja las alertas y la validación.
 * @param {object} response - El objeto de respuesta JSON del servidor.
 * @returns {Promise<any>} Una promesa que resuelve después de mostrar la alerta.
 */
function handleAlertResponse(response) {
    if (!response || typeof response !== 'object') {
        Swal.fire({
            title: 'Error Desconocido',
            text: 'El servidor devolvió un formato de respuesta inesperado.',
            icon: 'error'
        });
        return Promise.resolve();
    }

    clearValidationErrors(); // Siempre limpiar antes de procesar

    const isSuccess = response.success === true;
    const alertIcon = response.icon || (isSuccess ? 'success' : 'error');
    const alertTitle = response.title || (isSuccess ? '¡Éxito!' : 'Error');
    const alertText = response.message || (isSuccess ? 'Operación completada exitosamente.' : 'Ha ocurrido un error.');

    // ----------------------------------------------------------------------
    // MANEJO DE ERRORES DE VALIDACIÓN ESPECÍFICOS (La nueva propiedad)
    // ----------------------------------------------------------------------
    if (!isSuccess && response.errors && Object.keys(response.errors).length > 0) {
        // La validación del modelo falló. Inyectamos los errores en el formulario.
        for (const fieldName in response.errors) {
            if (response.errors.hasOwnProperty(fieldName)) {
                const errorMessages = response.errors[fieldName];

                // Buscar por atributo name (más robusto para diferentes tipos de input)
                const $input = $(`[name="${fieldName}"]`);

                if ($input.length) {
                    $input.addClass("is-invalid");

                    // Buscar el span de validación de MVC si existe (data-valmsg-for)
                    const $valMsg = $(`[data-valmsg-for="${fieldName}"]`);

                    if ($valMsg.length) {
                        // Usar el contenedor MVC si está presente
                        $valMsg.addClass("text-danger validation-error-message");
                        $valMsg.html(errorMessages.join('<br>'));
                    } else {
                        // Si no hay span MVC, insertamos un div invalid-feedback estándar de Bootstrap
                        const $feedback = $(`<div class="invalid-feedback validation-error-message">${errorMessages.join('<br>')}</div>`);
                        // Manejo especial para inputs que requieren la clase en el contenedor (como radios/checkboxes)
                        if ($input.is(':checkbox') || $input.is(':radio')) {
                            // Buscar el contenedor padre o agruparlo
                            $input.closest('.form-check').append($feedback);
                        } else {
                            $feedback.insertAfter($input);
                        }
                    }
                }
            }
        }

        // Si hay errores de campo, mostramos la alerta general, pero solo como un aviso.
        return Swal.fire({
            title: alertTitle,
            text: alertText,
            icon: alertIcon
        });
    }

    // ----------------------------------------------------------------------
    // MANEJO DE ALERTA GENERAL Y REDIRECCIÓN (para éxito o error general)
    // ----------------------------------------------------------------------
    return Swal.fire({
        title: alertTitle,
        text: alertText,
        icon: alertIcon,
        confirmButtonText: 'Aceptar'
    }).then(() => {
        // REDIRECCIÓN (solo si es exitoso o el servidor lo requiere)
        if (isSuccess && response.redirectUrl) {
            console.log(`Redireccionando a: ${response.redirectUrl}`);
            window.location.href = response.redirectUrl;
        } else if (isSuccess && !response.redirectUrl) {
            // Si fue exitoso y no hay redirección específica, recargar la página.
            // Esto es útil para acciones de "Eliminar" o "Estado".
            location.reload();
        }
    });
}


// ----------------------------------------------------------------------
// 2. FUNCIÓN PRINCIPAL PARA ACCIONES CON CONFIRMACIÓN (Usando Fetch API)
// ----------------------------------------------------------------------

/**
 * Función genérica para manejar acciones con confirmación (por ejemplo, Eliminar, Activar/Desactivar).
 * Utiliza Fetch API y se basa en el flujo showConfirmation del DTO.
 */
function handleActionWithConfirmation(url, data = {}, options = {}) {
    const token = $('input[name="__RequestVerificationToken"]').val();

    if (!token) {
        console.error('⚠️ Token antiforgery no encontrado');
        handleAlertResponse({ success: false, title: 'Error', message: 'Token de seguridad no encontrado. Recarga la página.', icon: 'error' });
        return Promise.reject('No token found');
    }

    const defaults = {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(data)
    };

    return fetch(url, { ...defaults, ...options })
        .then(response => {
            if (!response.ok) throw new Error(`Error HTTP: ${response.status}`);
            return response.json();
        })
        .then(result => {
            // Si requiere confirmación, mostrar SweetAlert
            if (result.showConfirmation) {
                return Swal.fire({
                    title: result.title || '¿Estás seguro?',
                    text: result.message,
                    icon: result.icon || 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#d33',
                    confirmButtonText: result.confirmButtonText || 'Sí, continuar',
                    cancelButtonText: result.cancelButtonText || 'Cancelar'
                }).then(swalResult => {
                    if (swalResult.isConfirmed) {
                        // Reenviar con confirmación
                        data.confirmed = true;
                        return fetch(url, {
                            ...defaults,
                            body: JSON.stringify(data)
                        })
                            .then(response => response.json())
                            .then(finalResult => {
                                // Mostrar el resultado final (éxito/error)
                                return handleAlertResponse(finalResult);
                            });
                    }
                    console.log('❌ Usuario canceló la acción');
                    return { success: false, cancelled: true };
                });
            } else {
                // Si no requiere confirmación, solo mostrar el resultado.
                return handleAlertResponse(result);
            }
        })
        .catch(error => {
            console.error('❌ Error en handleActionWithConfirmation:', error);
            handleAlertResponse({
                success: false,
                title: 'Error de Conexión',
                message: error.message || 'Ocurrió un error al procesar la solicitud.',
                icon: 'error'
            });
            return { success: false, error: error.message };
        });
}


// ----------------------------------------------------------------------
// 3. INICIALIZACIÓN Y EVENTOS
// ----------------------------------------------------------------------

$(document).ready(function () {
    console.log('🚀 Sistema de alertas y manejo de acciones cargado');

    // ----------------------------------------------------------------------
    // MANEJO DE ALERTAS DESDE TEMPDATA (desde el servidor en redirecciones)
    // ----------------------------------------------------------------------
    const errorAlertJson = document.querySelector('[data-error-alert]')?.getAttribute('data-error-alert');
    const successAlertJson = document.querySelector('[data-success-alert]')?.getAttribute('data-success-alert');

    if (errorAlertJson) {
        try {
            const alertData = JSON.parse(errorAlertJson);
            Swal.fire({
                icon: alertData.icon || 'error',
                title: alertData.title || 'Error',
                text: alertData.text || 'Ha ocurrido un error'
            });
        } catch (e) {
            console.error('Error parsing error alert JSON:', e);
        }
    }

    if (successAlertJson) {
        try {
            const alertData = JSON.parse(successAlertJson);
            Swal.fire({
                icon: alertData.icon || 'success',
                title: alertData.title || '¡Éxito!',
                text: alertData.text || 'Operación completada exitosamente'
            });
        } catch (e) {
            console.error('Error parsing success alert JSON:', e);
        }
    }

    // Event listener para botones con data-action
    $(document).on('click', '[data-action]', function (e) {
        e.preventDefault();
        console.log('🖱️ Clic detectado en botón con data-action');

        const $btn = $(this);
        const action = $btn.data('action');
        const controller = $btn.data('controller') || 'Permisos';
        const id = $btn.data('id');

        const url = `/${controller}/${action}`;
        const data = { id: id };

        // Deshabilitar botón durante la petición
        $btn.prop('disabled', true);
        $btn.css('opacity', '0.5');

        handleActionWithConfirmation(url, data)
            .finally(() => {
                // Habilitar botón al finalizar (ya sea éxito, error o cancelación)
                $btn.prop('disabled', false);
                $btn.css('opacity', '1');
            });
    });

    // Add AJAX form handler: intercept forms with class 'ajax-form' and submit as form data
    (function(){
        document.addEventListener('submit', function(e){
            var form = e.target;
            if (!form.classList || !form.classList.contains('ajax-form')) return;
            e.preventDefault();

            // Build FormData from the form
            var formData = new FormData(form);

            // Read antiforgery token if present
            var token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

            // Prepare headers; do not set Content-Type so browser sets multipart/form-data boundary
            var headers = {};
            if (token) headers['RequestVerificationToken'] = token;
            headers['Accept'] = 'application/json';

            fetch(form.action, {
                method: (form.method || 'POST').toUpperCase(),
                headers: headers,
                body: formData
            })
            .then(function(response){
                if (!response.ok) throw new Error('Error HTTP: ' + response.status);
                return response.json();
            })
            .then(function(json){
                // Delegate to central handler
                return handleAlertResponse(json);
            })
            .catch(function(err){
                console.error('Error submitting ajax form:', err);
                handleAlertResponse({ success: false, title: 'Error', message: err.message || 'Error en la petición' });
            });
        });
    })();
});
