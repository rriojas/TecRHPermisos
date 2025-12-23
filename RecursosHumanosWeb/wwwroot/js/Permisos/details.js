// JavaScript para la vista de Detalles del Permiso
document.addEventListener('DOMContentLoaded', function () {
    // Función para formatear fechas
    function formatDate(dateString) {
        if (!dateString) return '';

        const date = new Date(dateString);
        const options = {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        };

        return date.toLocaleDateString('es-MX', options);
    }

    // Función para animar los elementos al cargar
    function animateElements() {
        const detailItems = document.querySelectorAll('.detail-item');

        detailItems.forEach((item, index) => {
            item.style.opacity = '0';
            item.style.transform = 'translateY(30px)';

            setTimeout(() => {
                item.style.transition = 'all 0.6s cubic-bezier(0.4, 0, 0.2, 1)';
                item.style.opacity = '1';
                item.style.transform = 'translateY(0)';
            }, index * 100);
        });
    }

    // Función para manejar tooltips dinámicos
    function initTooltips() {
        const statusBadges = document.querySelectorAll('.status-badge');

        statusBadges.forEach(badge => {
            badge.addEventListener('mouseenter', function () {
                const tooltip = document.createElement('div');
                tooltip.className = 'custom-tooltip';

                if (this.classList.contains('status-approved')) {
                    tooltip.textContent = 'Solicitud aprobada por el supervisor';
                } else if (this.classList.contains('status-pending')) {
                    tooltip.textContent = 'Solicitud en espera de aprobación';
                } else if (this.classList.contains('status-rejected')) {
                    tooltip.textContent = 'Solicitud rechazada';
                } else if (this.classList.contains('goce-si')) {
                    tooltip.textContent = 'Permiso con goce de sueldo';
                } else if (this.classList.contains('goce-no')) {
                    tooltip.textContent = 'Permiso sin goce de sueldo';
                }

                tooltip.style.cssText = `
                    position: absolute;
                    background: rgba(0, 0, 0, 0.8);
                    color: white;
                    padding: 8px 12px;
                    border-radius: 6px;
                    font-size: 0.75rem;
                    white-space: nowrap;
                    z-index: 1000;
                    pointer-events: none;
                    opacity: 0;
                    transition: opacity 0.3s ease;
                    top: -40px;
                    left: 50%;
                    transform: translateX(-50%);
                `;

                this.style.position = 'relative';
                this.appendChild(tooltip);

                setTimeout(() => {
                    tooltip.style.opacity = '1';
                }, 100);
            });

            badge.addEventListener('mouseleave', function () {
                const tooltip = this.querySelector('.custom-tooltip');
                if (tooltip) {
                    tooltip.style.opacity = '0';
                    setTimeout(() => {
                        if (tooltip.parentNode) {
                            tooltip.parentNode.removeChild(tooltip);
                        }
                    }, 300);
                }
            });
        });
    }

    // Función para copiar información al portapapeles
    function initCopyFunctionality() {
        const detailValues = document.querySelectorAll('.detail-value');

        detailValues.forEach(value => {
            value.addEventListener('click', function (e) {
                // Evitar copiar si contiene badges o elementos especiales
                if (this.querySelector('.status-badge')) return;

                const text = this.textContent.trim();
                if (text && text.length > 0) {
                    navigator.clipboard.writeText(text).then(() => {
                        showCopyNotification(this);
                    }).catch(err => {
                        console.log('Error al copiar: ', err);
                    });
                }
            });
        });
    }

    // Función para mostrar notificación de copiado
    function showCopyNotification(element) {
        const notification = document.createElement('div');
        notification.textContent = '¡Copiado!';
        notification.style.cssText = `
            position: absolute;
            background: linear-gradient(135deg, #10b981, #059669);
            color: white;
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 0.75rem;
            font-weight: 600;
            z-index: 1000;
            pointer-events: none;
            opacity: 0;
            transition: all 0.3s ease;
            transform: translateY(-10px);
            box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
        `;

        element.style.position = 'relative';
        element.appendChild(notification);

        setTimeout(() => {
            notification.style.opacity = '1';
            notification.style.transform = 'translateY(-20px)';
        }, 50);

        setTimeout(() => {
            notification.style.opacity = '0';
            notification.style.transform = 'translateY(-30px)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 1500);
    }

    // Función para añadir efectos de partículas
    function addParticleEffect() {
        const header = document.querySelector('.card-header');
        if (!header) return;

        for (let i = 0; i < 5; i++) {
            setTimeout(() => {
                createParticle(header);
            }, i * 1000);
        }

        // Repetir cada 10 segundos
        setInterval(() => {
            for (let i = 0; i < 5; i++) {
                setTimeout(() => {
                    createParticle(header);
                }, i * 1000);
            }
        }, 10000);
    }

    // Función para crear una partícula
    function createParticle(container) {
        const particle = document.createElement('div');
        particle.style.cssText = `
            position: absolute;
            width: 4px;
            height: 4px;
            background: rgba(255, 255, 255, 0.8);
            border-radius: 50%;
            pointer-events: none;
            z-index: 0;
        `;

        const startX = Math.random() * container.offsetWidth;
        const startY = container.offsetHeight;

        particle.style.left = startX + 'px';
        particle.style.top = startY + 'px';

        container.appendChild(particle);

        const duration = 3000 + Math.random() * 2000;
        const endY = -20;
        const endX = startX + (Math.random() - 0.5) * 100;

        particle.animate([
            {
                transform: `translate(0, 0) scale(0)`,
                opacity: 0
            },
            {
                transform: `translate(0, -20px) scale(1)`,
                opacity: 1,
                offset: 0.1
            },
            {
                transform: `translate(${endX - startX}px, ${endY}px) scale(0.5)`,
                opacity: 0
            }
        ], {
            duration: duration,
            easing: 'cubic-bezier(0.4, 0, 0.2, 1)'
        }).onfinish = () => {
            if (particle.parentNode) {
                particle.parentNode.removeChild(particle);
            }
        };
    }

    // Función para detectar el estado del permiso y ajustar colores
    function adjustColorsBasedOnStatus() {
        const statusBadges = document.querySelectorAll('.status-badge');
        const header = document.querySelector('.card-header');

        statusBadges.forEach(badge => {
            if (badge.classList.contains('status-rejected')) {
                // Cambiar el gradiente del header para permisos rechazados
                if (header) {
                    header.style.background = 'linear-gradient(135deg, #dc2626 0%, #991b1b 50%, #7f1d1d 100%)';
                }
            } else if (badge.classList.contains('status-pending')) {
                // Gradiente amarillo para pendientes
                if (header) {
                    header.style.background = 'linear-gradient(135deg, #f59e0b 0%, #d97706 50%, #92400e 100%)';
                }
            }
        });
    }

    // Función para añadir efectos de hover avanzados
    function initAdvancedHoverEffects() {
        const detailItems = document.querySelectorAll('.detail-item');

        detailItems.forEach(item => {
            item.addEventListener('mouseenter', function (e) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;

                const ripple = document.createElement('div');
                ripple.style.cssText = `
                    position: absolute;
                    border-radius: 50%;
                    background: rgba(79, 70, 229, 0.1);
                    transform: scale(0);
                    animation: ripple 0.6s linear;
                    left: ${x}px;
                    top: ${y}px;
                    width: 20px;
                    height: 20px;
                    margin-left: -10px;
                    margin-top: -10px;
                    pointer-events: none;
                `;

                this.appendChild(ripple);

                setTimeout(() => {
                    if (ripple.parentNode) {
                        ripple.parentNode.removeChild(ripple);
                    }
                }, 600);
            });
        });

        // Añadir estilos para la animación ripple
        const style = document.createElement('style');
        style.textContent = `
            @keyframes ripple {
                to {
                    transform: scale(4);
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(style);
    }

    // Función para contar días hasta fechas importantes
    function addCountdownTimers() {
        const fechaItems = document.querySelectorAll('.fecha .detail-value');

        fechaItems.forEach(item => {
            const dateText = item.textContent.trim();
            const dateMatch = dateText.match(/(\d{1,2}\/\d{1,2}\/\d{4})/);

            if (dateMatch) {
                const date = new Date(dateMatch[1]);
                const now = new Date();
                const diffTime = date - now;
                const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

                if (diffDays > 0 && diffDays <= 30) {
                    const countdown = document.createElement('span');
                    countdown.style.cssText = `
                        display: block;
                        font-size: 0.8rem;
                        color: #6b7280;
                        margin-top: 5px;
                        font-weight: 500;
                    `;
                    countdown.textContent = `(En ${diffDays} ${diffDays === 1 ? 'día' : 'días'})`;
                    item.appendChild(countdown);
                }
            }
        });
    }

    // Inicializar todas las funciones
    setTimeout(animateElements, 300);
    initTooltips();
    initCopyFunctionality();
    addParticleEffect();
    adjustColorsBasedOnStatus();
    initAdvancedHoverEffects();
    addCountdownTimers();

    // Mensaje de bienvenida en consola (para desarrolladores)
    console.log('%c🎉 Sistema de Permisos Laborales', 'color: #4f46e5; font-size: 16px; font-weight: bold;');
    console.log('%cTodos los efectos visuales están activos', 'color: #10b981; font-size: 12px;');
});