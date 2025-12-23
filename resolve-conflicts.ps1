# Script para resolver conflictos de Git
# Ejecutar desde la raíz del repositorio

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resolución de Conflictos Git" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que estamos en medio de un merge
if (-not (Test-Path ".git/MERGE_HEAD")) {
    Write-Host "ERROR: No hay una fusión en curso." -ForegroundColor Red
    exit 1
}

Write-Host "? Fusión en curso detectada" -ForegroundColor Green
Write-Host ""

# Listar archivos en conflicto
Write-Host "Archivos con conflictos:" -ForegroundColor Yellow
git diff --name-only --diff-filter=U
Write-Host ""

# Preguntar estrategia de resolución
Write-Host "Selecciona una estrategia de resolución:" -ForegroundColor Cyan
Write-Host "1. Aceptar TODOS mis cambios locales (ours)" -ForegroundColor White
Write-Host "2. Aceptar TODOS los cambios remotos (theirs)" -ForegroundColor White
Write-Host "3. Resolver manualmente en Visual Studio" -ForegroundColor White
Write-Host "4. Abortar la fusión y volver al estado anterior" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Elige una opción (1-4)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "Aceptando cambios locales..." -ForegroundColor Yellow
        
        # Obtener lista de archivos en conflicto
        $conflictFiles = git diff --name-only --diff-filter=U
        
        foreach ($file in $conflictFiles) {
            Write-Host "  - Resolviendo: $file" -ForegroundColor Gray
            git checkout --ours $file
            git add $file
        }
        
        Write-Host "? Conflictos resueltos con cambios locales" -ForegroundColor Green
    }
    
    "2" {
        Write-Host ""
        Write-Host "Aceptando cambios remotos..." -ForegroundColor Yellow
        
        $conflictFiles = git diff --name-only --diff-filter=U
        
        foreach ($file in $conflictFiles) {
            Write-Host "  - Resolviendo: $file" -ForegroundColor Gray
            git checkout --theirs $file
            git add $file
        }
        
        Write-Host "? Conflictos resueltos con cambios remotos" -ForegroundColor Green
    }
    
    "3" {
        Write-Host ""
        Write-Host "Abriendo Visual Studio para resolución manual..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Instrucciones:" -ForegroundColor Cyan
        Write-Host "1. En Visual Studio, ve a la ventana Git (View > Git Changes)" -ForegroundColor White
        Write-Host "2. Haz clic en cada archivo con conflicto" -ForegroundColor White
        Write-Host "3. Elige 'Aceptar cambio actual' o 'Aceptar cambio entrante'" -ForegroundColor White
        Write-Host "4. Una vez resueltos todos, vuelve aquí y presiona Enter" -ForegroundColor White
        Write-Host ""
        
        # Listar archivos en conflicto
        $conflictFiles = git diff --name-only --diff-filter=U
        Write-Host "Archivos pendientes:" -ForegroundColor Yellow
        foreach ($file in $conflictFiles) {
            Write-Host "  - $file" -ForegroundColor Gray
        }
        
        Read-Host "`nPresiona Enter cuando hayas resuelto todos los conflictos en Visual Studio"
        
        # Verificar si quedan conflictos
        $remainingConflicts = git diff --name-only --diff-filter=U
        if ($remainingConflicts) {
            Write-Host "? Aún quedan conflictos sin resolver:" -ForegroundColor Yellow
            foreach ($file in $remainingConflicts) {
                Write-Host "  - $file" -ForegroundColor Red
            }
            Write-Host ""
            Write-Host "Resuelve los conflictos restantes y ejecuta este script nuevamente." -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host "? Todos los conflictos resueltos" -ForegroundColor Green
    }
    
    "4" {
        Write-Host ""
        Write-Host "Abortando fusión..." -ForegroundColor Yellow
        git merge --abort
        Write-Host "? Fusión abortada. Volviste al estado anterior." -ForegroundColor Green
        Write-Host ""
        Write-Host "Puedes intentar el pull nuevamente cuando estés listo." -ForegroundColor Cyan
        exit 0
    }
    
    default {
        Write-Host ""
        Write-Host "ERROR: Opción inválida." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Finalizando Fusión" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar estado
$status = git status --short
if ($status -match "^U") {
    Write-Host "? Aún hay conflictos sin resolver:" -ForegroundColor Yellow
    git status --short | Where-Object { $_ -match "^U" }
    Write-Host ""
    Write-Host "Por favor, resuelve los conflictos restantes antes de continuar." -ForegroundColor Yellow
    exit 1
}

# Completar el merge
Write-Host "Completando la fusión..." -ForegroundColor Yellow
git commit -m "merge: Resolver conflictos de fusión con origin/master

- Conflictos resueltos en archivos de Models
- Conflictos resueltos en Views
- Conflictos resueltos en archivos de configuración"

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Fusión completada exitosamente" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ahora puedes hacer push:" -ForegroundColor Cyan
    Write-Host "  git push origin master" -ForegroundColor White
} else {
    Write-Host "? Error al completar la fusión" -ForegroundColor Red
    Write-Host "Verifica el estado con: git status" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Estado Final" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
git status --short
