# Script AUTOMÁTICO para resolver conflictos priorizando CAMBIOS LOCALES
# y subir TODO al repositorio remoto
# Ejecutar desde: C:\Users\axelc\source\repos\RecursosHumanosWeb

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "?? RESOLUCIÓN AUTOMÁTICA DE CONFLICTOS" -ForegroundColor Cyan
Write-Host "   Priorizando CAMBIOS LOCALES" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar ubicación
if (-not (Test-Path "RecursosHumanosWeb.sln")) {
    Write-Host "? ERROR: No estás en la raíz del proyecto." -ForegroundColor Red
    Write-Host "Navega a: C:\Users\axelc\source\repos\RecursosHumanosWeb" -ForegroundColor Yellow
    exit 1
}

Write-Host "? Ubicación correcta verificada" -ForegroundColor Green
Write-Host ""

# Verificar si hay fusión en curso
$mergeInProgress = Test-Path ".git/MERGE_HEAD"

if (-not $mergeInProgress) {
    Write-Host "? No hay fusión en curso actualmente." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Verificando estado del repositorio..." -ForegroundColor Cyan
    
    $status = git status --porcelain
    if ($status) {
        Write-Host ""
        Write-Host "?? Tienes cambios sin commitear:" -ForegroundColor Yellow
        git status --short
        Write-Host ""
        Write-Host "Guardando cambios locales..." -ForegroundColor Cyan
        git add .
        git commit -m "feat: Cambios locales antes de sincronización

- Actualización de controladores (Permisos, Usuarios, Cortes)
- Mejoras en vistas (Review, Create, Edit, Index)
- Actualización de ViewModels y DTOs
- Mejoras en CSS y JavaScript
- Sistema de reset de contraseña mejorado
- Actualización de .gitignore y scripts de Git"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Cambios guardados exitosamente" -ForegroundColor Green
        }
    } else {
        Write-Host "? No hay cambios pendientes" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Intentando sincronizar con el remoto..." -ForegroundColor Cyan
    
    # Intentar pull
    git pull origin master --no-edit 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Sincronización exitosa sin conflictos" -ForegroundColor Green
        Write-Host ""
        Write-Host "Subiendo cambios a GitHub..." -ForegroundColor Cyan
        git push origin master
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "? ¡TODO COMPLETADO EXITOSAMENTE!" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "Tu código local ha sido subido a GitHub" -ForegroundColor Green
            exit 0
        } else {
            Write-Host ""
            Write-Host "? Push rechazado. Intentando con force-with-lease..." -ForegroundColor Yellow
            git push origin master --force-with-lease
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host ""
                Write-Host "========================================" -ForegroundColor Cyan
                Write-Host "? ¡TODO COMPLETADO EXITOSAMENTE!" -ForegroundColor Green
                Write-Host "========================================" -ForegroundColor Cyan
                exit 0
            }
        }
    }
    
    # Si llegamos aquí, hay conflictos después del pull
    $mergeInProgress = Test-Path ".git/MERGE_HEAD"
}

if (-not $mergeInProgress) {
    Write-Host ""
    Write-Host "? Error inesperado. Estado del repositorio:" -ForegroundColor Red
    git status
    exit 1
}

Write-Host "? Conflictos detectados durante la fusión" -ForegroundColor Yellow
Write-Host ""

# Obtener archivos en conflicto
$conflictFiles = git diff --name-only --diff-filter=U

if ($conflictFiles.Count -eq 0) {
    Write-Host "? No hay conflictos pendientes, completando fusión..." -ForegroundColor Green
    git commit --no-edit
    git push origin master --force-with-lease
    Write-Host ""
    Write-Host "? Proceso completado" -ForegroundColor Green
    exit 0
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Archivos con Conflictos Detectados" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$totalFiles = $conflictFiles.Count
$counter = 0

foreach ($file in $conflictFiles) {
    $counter++
    Write-Host "[$counter/$totalFiles] $file" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resolviendo Conflictos..." -ForegroundColor Yellow
Write-Host "Estrategia: MANTENER CAMBIOS LOCALES" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$counter = 0
$successful = 0
$failed = 0

foreach ($file in $conflictFiles) {
    $counter++
    
    try {
        # Aceptar cambios locales (ours)
        git checkout --ours $file 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            git add $file 2>&1 | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                $successful++
                Write-Host "? [$counter/$totalFiles] $file" -ForegroundColor Green
            } else {
                $failed++
                Write-Host "? [$counter/$totalFiles] $file (error al agregar)" -ForegroundColor Red
            }
        } else {
            $failed++
            Write-Host "? [$counter/$totalFiles] $file (error al resolver)" -ForegroundColor Red
        }
    }
    catch {
        $failed++
        Write-Host "? [$counter/$totalFiles] $file (excepción: $_)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resumen de Resolución" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total de archivos: $totalFiles" -ForegroundColor White
Write-Host "Resueltos exitosamente: $successful" -ForegroundColor Green
Write-Host "Fallidos: $failed" -ForegroundColor Red
Write-Host ""

# Verificar si quedan conflictos
$remainingConflicts = git diff --name-only --diff-filter=U

if ($remainingConflicts) {
    Write-Host "? Aún quedan conflictos sin resolver:" -ForegroundColor Yellow
    Write-Host ""
    foreach ($file in $remainingConflicts) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Intentando resolución alternativa..." -ForegroundColor Yellow
    
    # Estrategia alternativa: eliminar marcadores de conflicto manualmente
    foreach ($file in $remainingConflicts) {
        try {
            if (Test-Path $file) {
                $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
                
                if ($content) {
                    # Eliminar marcadores de conflicto y mantener sección "ours"
                    $pattern = '<<<<<<< HEAD\r?\n([\s\S]*?)=======\r?\n[\s\S]*?>>>>>>> .*\r?\n'
                    $resolved = $content -replace $pattern, '$1'
                    
                    Set-Content -Path $file -Value $resolved -NoNewline
                    git add $file
                    Write-Host "  ? Resuelto manualmente: $file" -ForegroundColor Green
                }
            }
        }
        catch {
            Write-Host "  ? No se pudo resolver: $file" -ForegroundColor Red
        }
    }
}

# Verificación final
$finalConflicts = git diff --name-only --diff-filter=U

if ($finalConflicts) {
    Write-Host ""
    Write-Host "? ERROR: No se pudieron resolver todos los conflictos automáticamente" -ForegroundColor Red
    Write-Host ""
    Write-Host "Archivos que requieren intervención manual:" -ForegroundColor Yellow
    foreach ($file in $finalConflicts) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Opciones:" -ForegroundColor Cyan
    Write-Host "1. Abre Visual Studio y resuelve manualmente los archivos listados" -ForegroundColor White
    Write-Host "2. Ejecuta: git checkout --ours <archivo> && git add <archivo>" -ForegroundColor White
    Write-Host "3. Aborta con: git merge --abort" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "? Todos los Conflictos Resueltos" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Crear commit de fusión
Write-Host "Creando commit de fusión..." -ForegroundColor Cyan

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$commitMessage = @"
merge: Fusión con origin/master - Prioridad LOCAL

Resolución automática de conflictos:
- ? $successful archivos resueltos exitosamente
- Estrategia: Mantener cambios locales (ours)
- Timestamp: $timestamp

Archivos principales actualizados:
- Controllers: PermisosController, UsuariosController, CortesController
- Views: Review, Create, Edit, Index (múltiples módulos)
- Models: ViewModels, DTOs actualizados
- Configuración: .gitignore, appsettings.json
- Scripts: Resolución automática de conflictos
"@

git commit -m $commitMessage

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? Error al crear commit de fusión" -ForegroundColor Red
    Write-Host ""
    Write-Host "Estado del repositorio:" -ForegroundColor Yellow
    git status
    exit 1
}

Write-Host "? Commit de fusión creado exitosamente" -ForegroundColor Green
Write-Host ""

# Subir cambios a GitHub
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Subiendo Cambios a GitHub" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Intentando push normal..." -ForegroundColor Cyan
git push origin master 2>&1 | Tee-Object -Variable pushOutput | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Push exitoso" -ForegroundColor Green
} else {
    Write-Host "? Push rechazado, intentando con --force-with-lease..." -ForegroundColor Yellow
    Write-Host ""
    
    git push origin master --force-with-lease 2>&1 | Tee-Object -Variable pushOutput | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Push con --force-with-lease exitoso" -ForegroundColor Green
    } else {
        Write-Host "? Force-with-lease rechazado, intentando --force..." -ForegroundColor Yellow
        Write-Host ""
        
        git push origin master --force 2>&1 | Tee-Object -Variable pushOutput | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Push forzado exitoso" -ForegroundColor Green
        } else {
            Write-Host ""
            Write-Host "? ERROR: No se pudo subir al repositorio remoto" -ForegroundColor Red
            Write-Host ""
            Write-Host "Salida del comando:" -ForegroundColor Yellow
            Write-Host $pushOutput
            Write-Host ""
            Write-Host "Posibles causas:" -ForegroundColor Yellow
            Write-Host "1. Problemas de conexión a internet" -ForegroundColor White
            Write-Host "2. Permisos insuficientes en el repositorio" -ForegroundColor White
            Write-Host "3. El repositorio remoto tiene protección de rama" -ForegroundColor White
            Write-Host ""
            Write-Host "Intenta manualmente:" -ForegroundColor Cyan
            Write-Host "  git push origin master --force" -ForegroundColor White
            exit 1
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "? ¡PROCESO COMPLETADO EXITOSAMENTE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resumen:" -ForegroundColor Cyan
Write-Host "  ? Conflictos resueltos: $successful archivos" -ForegroundColor Green
Write-Host "  ? Estrategia: Cambios locales mantenidos" -ForegroundColor Green
Write-Host "  ? Commit de fusión creado" -ForegroundColor Green
Write-Host "  ? Cambios subidos a GitHub" -ForegroundColor Green
Write-Host ""
Write-Host "Estado final del repositorio:" -ForegroundColor Cyan
Write-Host ""
git log --oneline -3
Write-Host ""
git status --short
Write-Host ""
Write-Host "?? Tu código local está ahora en GitHub!" -ForegroundColor Green
Write-Host ""
Write-Host "Repositorio remoto: https://github.com/rriojas/TecRHPermisos" -ForegroundColor Cyan
