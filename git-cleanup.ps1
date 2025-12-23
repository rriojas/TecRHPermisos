# Script para limpiar el repositorio Git de archivos de Visual Studio
# Este script debe ejecutarse desde la raíz del repositorio

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Limpieza de Repositorio Git" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que estamos en un repositorio Git
if (-not (Test-Path ".git")) {
    Write-Host "ERROR: No se encontró un repositorio Git en el directorio actual." -ForegroundColor Red
    Write-Host "Por favor, ejecuta este script desde la raíz del repositorio." -ForegroundColor Yellow
    exit 1
}

Write-Host "? Repositorio Git detectado" -ForegroundColor Green
Write-Host ""

# 2. Guardar cambios actuales (por seguridad)
Write-Host "Paso 1: Guardando el estado actual..." -ForegroundColor Yellow
git add -A
$stashResult = git stash
Write-Host "? Estado guardado" -ForegroundColor Green
Write-Host ""

# 3. Eliminar archivos del índice Git (sin eliminarlos del disco)
Write-Host "Paso 2: Eliminando archivos problemáticos del índice Git..." -ForegroundColor Yellow

# Eliminar carpeta .vs completa
if (Test-Path ".vs") {
    Write-Host "  - Eliminando .vs/ del índice..." -ForegroundColor Gray
    git rm -r --cached ".vs/" 2>$null
}

# Eliminar carpetas bin y obj
Get-ChildItem -Path . -Recurse -Directory -Filter "bin" | ForEach-Object {
    Write-Host "  - Eliminando $($_.FullName) del índice..." -ForegroundColor Gray
    git rm -r --cached $_.FullName 2>$null
}

Get-ChildItem -Path . -Recurse -Directory -Filter "obj" | ForEach-Object {
    Write-Host "  - Eliminando $($_.FullName) del índice..." -ForegroundColor Gray
    git rm -r --cached $_.FullName 2>$null
}

# Eliminar archivos de usuario
Write-Host "  - Eliminando archivos *.user del índice..." -ForegroundColor Gray
git rm --cached *.user 2>$null
git rm --cached **/*.user 2>$null

Write-Host "? Archivos eliminados del índice" -ForegroundColor Green
Write-Host ""

# 4. Aplicar .gitignore
Write-Host "Paso 3: Aplicando .gitignore..." -ForegroundColor Yellow
git add .gitignore
Write-Host "? .gitignore actualizado" -ForegroundColor Green
Write-Host ""

# 5. Hacer commit de los cambios
Write-Host "Paso 4: Creando commit de limpieza..." -ForegroundColor Yellow
git commit -m "chore: Limpiar archivos de Visual Studio y actualizar .gitignore

- Eliminar carpeta .vs/ del repositorio
- Eliminar carpetas bin/ y obj/
- Eliminar archivos *.user
- Actualizar .gitignore con reglas completas para .NET 8
- Agregar reglas para ignorar GitHub Copilot snapshots"

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Commit creado exitosamente" -ForegroundColor Green
} else {
    Write-Host "! No hay cambios para hacer commit (puede que ya estuviera limpio)" -ForegroundColor Yellow
}
Write-Host ""

# 6. Restaurar cambios guardados (si los hay)
Write-Host "Paso 5: Restaurando cambios previos..." -ForegroundColor Yellow
if ($stashResult -ne "No local changes to save") {
    git stash pop
    Write-Host "? Cambios restaurados" -ForegroundColor Green
} else {
    Write-Host "? No había cambios previos para restaurar" -ForegroundColor Green
}
Write-Host ""

# 7. Mostrar estado actual
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Estado Final del Repositorio" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
git status --short
Write-Host ""

# 8. Instrucciones finales
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Próximos Pasos" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Revisa los cambios con:" -ForegroundColor Yellow
Write-Host "   git status" -ForegroundColor White
Write-Host ""
Write-Host "2. Si todo se ve bien, publica los cambios:" -ForegroundColor Yellow
Write-Host "   git push origin master" -ForegroundColor White
Write-Host ""
Write-Host "3. Si necesitas forzar el push (solo si es necesario):" -ForegroundColor Yellow
Write-Host "   git push origin master --force" -ForegroundColor White
Write-Host ""
Write-Host "NOTA: Si aún tienes problemas con archivos bloqueados:" -ForegroundColor Red
Write-Host "  - Cierra Visual Studio completamente" -ForegroundColor Red
Write-Host "  - Ejecuta: git clean -xdf (CUIDADO: elimina archivos no rastreados)" -ForegroundColor Red
Write-Host ""
Write-Host "? Limpieza completada exitosamente!" -ForegroundColor Green
