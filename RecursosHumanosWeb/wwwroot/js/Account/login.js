// Mostrar/ocultar contraseña
document.addEventListener("DOMContentLoaded", () => {
    const toggleBtn = document.querySelector(".password-toggle");
    const passwordInput = document.querySelector("#Clave");
    const passwordIcon = document.querySelector("#passwordIcon");

    if (toggleBtn && passwordInput) {
        toggleBtn.addEventListener("click", () => {
            if (passwordInput.type === "password") {
                passwordInput.type = "text";
                passwordIcon.classList.remove("fa-eye");
                passwordIcon.classList.add("fa-eye-slash");
            } else {
                passwordInput.type = "password";
                passwordIcon.classList.remove("fa-eye-slash");
                passwordIcon.classList.add("fa-eye");
            }
        });
    }
});
