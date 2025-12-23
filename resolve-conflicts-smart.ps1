# Script específico para resolver conflictos en RecursosHumanosWeb
# Ejecutar desde: C:\Users\axelc\source\repos\RecursosHumanosWeb

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resolución de Conflictos - RecursosHumanosWeb" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar ubicación
$currentPath = Get-Location
if (-not (Test-Path "RecursosHumanosWeb.sln")) {
    Write-Host "ERROR: No estás en la raíz del proyecto." -ForegroundColor Red
    Write-Host "Navega a: C:\Users\axelc\source\repos\RecursosHumanosWeb" -ForegroundColor Yellow
    exit 1
}

# Verificar fusión en curso
if (-not (Test-Path ".git/MERGE_HEAD")) {
    Write-Host "? No hay una fusión en curso." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Estado actual:" -ForegroundColor Cyan
    git status
    exit 0
}

Write-Host "? Fusión en curso detectada" -ForegroundColor Green
Write-Host ""

# Obtener archivos en conflicto
$conflictFiles = git diff --name-only --diff-filter=U

if ($conflictFiles.Count -eq 0) {
    Write-Host "? No se encontraron conflictos pendientes." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Completando fusión..." -ForegroundColor Cyan
    git commit --no-edit
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Fusión completada exitosamente" -ForegroundColor Green
        Write-Host ""
        Write-Host "Siguiente paso:" -ForegroundColor Cyan
        Write-Host "  git push origin master" -ForegroundColor White
    }
    exit 0
}

# Categorizar conflictos
$criticalFiles = @()
$highPriorityFiles = @()
$mediumPriorityFiles = @()
$lowPriorityFiles = @()

foreach ($file in $conflictFiles) {
    # Controladores (CRÍTICO)
    if ($file -match "Controllers/.*Controller\.cs$") {
        $criticalFiles += $file
    }
    # DbContext (CRÍTICO)
    elseif ($file -match "RecursosHumanosContext\.cs$") {
        $criticalFiles += $file
    }
    # Vistas (ALTO)
    elseif ($file -match "Views/.*\.cshtml$") {
        $highPriorityFiles += $file
    }
    # CSS/JS (MEDIO)
    elseif ($file -match "wwwroot/.*\.(css|js)$") {
        $mediumPriorityFiles += $file
    }
    # ViewModels/DTOs (MEDIO)
    elseif ($file -match "(ViewModels|DTOs)/.*\.cs$") {
        $mediumPriorityFiles += $file
    }
    # Archivos de configuración de Copilot (BAJO)
    elseif ($file -match "(\.copilot|\.vs|copilot-)") {
        $lowPriorityFiles += $file
    }
    # Otros
    else {
        $mediumPriorityFiles += $file
    }
}

# Mostrar resumen de conflictos
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resumen de Conflictos por Prioridad" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($criticalFiles.Count -gt 0) {
    Write-Host "?? CRÍTICO ($($criticalFiles.Count) archivos):" -ForegroundColor Red
    foreach ($file in $criticalFiles) {
        Write-Host "   - $file" -ForegroundColor Red
    }
    Write-Host ""
}

if ($highPriorityFiles.Count -gt 0) {
    Write-Host "?? ALTO ($($highPriorityFiles.Count) archivos):" -ForegroundColor Yellow
    foreach ($file in $highPriorityFiles) {
        Write-Host "   - $file" -ForegroundColor Yellow
    }
    Write-Host ""
}

if ($mediumPriorityFiles.Count -gt 0) {
    Write-Host "?? MEDIO ($($mediumPriorityFiles.Count) archivos):" -ForegroundColor Green
    foreach ($file in $mediumPriorityFiles) {
        Write-Host "   - $file" -ForegroundColor Green
    }
    Write-Host ""
}

if ($lowPriorityFiles.Count -gt 0) {
    Write-Host "? BAJO ($($lowPriorityFiles.Count) archivos - archivos temporales):" -ForegroundColor Gray
    foreach ($file in $lowPriorityFiles) {
        Write-Host "   - $file" -ForegroundColor Gray
    }
    Write-Host ""
}

# Menú de opciones
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Estrategia de Resolución" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Selecciona una opción:" -ForegroundColor White
Write-Host ""
Write-Host "1. Aceptar TODOS mis cambios locales (ours)" -ForegroundColor Cyan
Write-Host "   ? Mantiene tu código actual intacto" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Aceptar TODOS los cambios remotos (theirs)" -ForegroundColor Cyan
Write-Host "   ? Descarga código del repositorio" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Estrategia INTELIGENTE (recomendado)" -ForegroundColor Green
Write-Host "   ? Local: Controladores, Modelos, Vistas" -ForegroundColor Gray
Write-Host "   ? Remoto: Archivos de configuración temporal" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Resolver manualmente en Visual Studio" -ForegroundColor Cyan
Write-Host "   ? Control total sobre cada conflicto" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Abortar fusión y volver atrás" -ForegroundColor Red
Write-Host "   ? Cancela todo y vuelve al estado anterior" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Elige una opción (1-5)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "Aceptando TODOS los cambios locales..." -ForegroundColor Yellow
        Write-Host ""
        
        foreach ($file in $conflictFiles) {
            Write-Host "  ? $file" -ForegroundColor Gray
            git checkout --ours $file
            git add $file
        }
        
        Write-Host ""
        Write-Host "? Conflictos resueltos con cambios locales" -ForegroundColor Green
    }
    
    "2" {
        Write-Host ""
        Write-Host "Aceptando TODOS los cambios remotos..." -ForegroundColor Yellow
        Write-Host ""
        
        foreach ($file in $conflictFiles) {
            Write-Host "  ? $file" -ForegroundColor Gray
            git checkout --theirs $file
            git add $file
        }
        
        Write-Host ""
        Write-Host "? Conflictos resueltos con cambios remotos" -ForegroundColor Green
    }
    
    "3" {
        Write-Host ""
        Write-Host "Aplicando estrategia INTELIGENTE..." -ForegroundColor Green
        Write-Host ""
        
        # Mantener cambios locales para archivos importantes
        Write-Host "Manteniendo cambios LOCALES para:" -ForegroundColor Cyan
        foreach ($file in ($criticalFiles + $highPriorityFiles + $mediumPriorityFiles)) {
            Write-Host "  ? $file" -ForegroundColor Gray
            git checkout --ours $file
            git add $file
        }
        
        # Aceptar cambios remotos para archivos temporales
        if ($lowPriorityFiles.Count -gt 0) {
            Write-Host ""
            Write-Host "Aceptando cambios REMOTOS para archivos temporales:" -ForegroundColor Cyan
            foreach ($file in $lowPriorityFiles) {
                Write-Host "  ? $file" -ForegroundColor Gray
                git checkout --theirs $file
                git add $file
            }
        }
        
        Write-Host ""
        Write-Host "? Estrategia inteligente aplicada" -ForegroundColor Green
    }
    
    "4" {
        Write-Host ""
        Write-Host "?? Resolución manual en Visual Studio" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Instrucciones:" -ForegroundColor Yellow
        Write-Host "1. Ve a: View > Git Changes (Ctrl+0, Ctrl+G)" -ForegroundColor White
        Write-Host "2. En 'Cambios sin combinar', haz clic en cada archivo" -ForegroundColor White
        Write-Host "3. Opciones disponibles:" -ForegroundColor White
        Write-Host "   - Aceptar cambio actual (tus cambios)" -ForegroundColor Gray
        Write-Host "   - Aceptar cambio entrante (cambios remotos)" -ForegroundColor Gray
        Write-Host "   - Aceptar ambos cambios" -ForegroundColor Gray
        Write-Host "4. Guarda cada archivo después de resolver" -ForegroundColor White
        Write-Host ""
        Write-Host "Archivos a revisar por prioridad:" -ForegroundColor Yellow
        
        if ($criticalFiles.Count -gt 0) {
            Write-Host ""
            Write-Host "?? CRÍTICO (revisa primero):" -ForegroundColor Red
            foreach ($file in $criticalFiles) {
                Write-Host "   - $file" -ForegroundColor Red
            }
        }
        
        Write-Host ""
        Read-Host "Presiona Enter cuando hayas resuelto todos los conflictos en Visual Studio"
        
        # Verificar si quedan conflictos
        $remainingConflicts = git diff --name-only --diff-filter=U
        if ($remainingConflicts) {
            Write-Host ""
            Write-Host "? Aún quedan conflictos sin resolver:" -ForegroundColor Yellow
            foreach ($file in $remainingConflicts) {
                Write-Host "  - $file" -ForegroundColor Red
            }
            Write-Host ""
            Write-Host "Resuelve los conflictos restantes y ejecuta el script nuevamente." -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host ""
        Write-Host "? Todos los conflictos resueltos" -ForegroundColor Green
    }
    
    "5" {
        Write-Host ""
        Write-Host "Abortando fusión..." -ForegroundColor Red
        git merge --abort
        Write-Host ""
        Write-Host "? Fusión abortada. Has vuelto al estado anterior." -ForegroundColor Green
        Write-Host ""
        Write-Host "Sugerencias:" -ForegroundColor Cyan
        Write-Host "- Revisa tu código local antes de intentar pull nuevamente" -ForegroundColor White
        Write-Host "- Considera hacer commit de tus cambios primero" -ForegroundColor White
        Write-Host "- Usa 'git stash' para guardar cambios temporalmente" -ForegroundColor White
        exit 0
    }
    
    default {
        Write-Host ""
        Write-Host "? Opción inválida. Por favor, ejecuta el script nuevamente." -ForegroundColor Red
        exit 1
    }
}

# Completar la fusión
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Finalizando Fusión" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar si hay conflictos pendientes
$remainingConflicts = git diff --name-only --diff-filter=U
if ($remainingConflicts) {
    Write-Host "? ERROR: Aún hay conflictos sin resolver:" -ForegroundColor Red
    foreach ($file in $remainingConflicts) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Acciones posibles:" -ForegroundColor Yellow
    Write-Host "1. Resolver manualmente en Visual Studio" -ForegroundColor White
    Write-Host "2. Ejecutar este script nuevamente y elegir otra opción" -ForegroundColor White
    exit 1
}

Write-Host "Creando commit de fusión..." -ForegroundColor Yellow

$commitMessage = @"
merge: Resolver conflictos con origin/master

Conflictos resueltos:
- Controllers: $($criticalFiles.Count + $highPriorityFiles.Count) archivos
- Views y Modelos: $($mediumPriorityFiles.Count) archivos
- Archivos temporales: $($lowPriorityFiles.Count) archivos

Estrategia: Priorizar cambios locales para código funcional
"@

git commit -m $commitMessage

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "? ¡Fusión completada exitosamente!" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Siguiente Paso: Push a GitHub" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Ejecuta uno de estos comandos:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "# Opción 1: Push normal" -ForegroundColor White
    Write-Host "git push origin master" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "# Opción 2: Push forzado con protección (recomendado)" -ForegroundColor White
    Write-Host "git push origin master --force-with-lease" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "? Error al completar la fusión" -ForegroundColor Red
    Write-Host ""
    Write-Host "Verifica el estado con:" -ForegroundColor Yellow
    Write-Host "  git status" -ForegroundColor White
}

Write-Host ""
Write-Host "Estado final del repositorio:" -ForegroundColor Cyan
git status --short
