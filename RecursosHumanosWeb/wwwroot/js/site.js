/**
 * site.js - Versión simplificada para depurar el cuelgue
 */
$(document).ready(function () {
    console.log('🚀 site.js - Iniciando...');

    // Simular autenticación estática para pruebas
    const isAuthenticated = true; // Cambia a false para probar el caso de invitado

    if (isAuthenticated) {
        console.log('Setting up authenticated header...');
        $('header').removeClass('header-not-logged').addClass('header-logged');
        $('.navbar-nav').css('display', '');
    } else {
        console.log('Setting up guest header...');
        $('header').removeClass('header-logged').addClass('header-not-logged');
        $('.navbar-nav').css('display', 'none');
    }

    console.log('✅ site.js - Listo para usar');
});