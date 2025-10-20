using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RecursosHumanosWeb.Models.DTOs;
using System;
using System.Collections.Generic;

namespace RecursosHumanosWeb.Models;

public partial class RecursosHumanosContext : DbContext
{
    public RecursosHumanosContext()
    {
    }

    public RecursosHumanosContext(DbContextOptions<RecursosHumanosContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiFunction> ApiFunctions { get; set; }

    public virtual DbSet<ApiKey> ApiKeys { get; set; }

    public virtual DbSet<ApiPermiso> ApiPermisos { get; set; }

    public virtual DbSet<ApiPermisosApiKeysTabla> ApiPermisosApiKeysTablas { get; set; }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<Corte> Cortes { get; set; }

    public virtual DbSet<Departamento> Departamentos { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<Puesto> Puestos { get; set; }

    public virtual DbSet<Tabla> Tablas { get; set; }

    public virtual DbSet<TipoPermiso> TipoPermisos { get; set; }

    public virtual DbSet<TipoUsuario> TipoUsuarios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VwPermisosUsuario> VwPermisosUsuarios { get; set; }
    public DbSet<LoginDTO> LoginResultDTO { get; set; }
    public DbSet<PermisosDTO> SearchPermissionsDTO { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("RecursosHumanosContext"));
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginDTO>().HasNoKey();
        modelBuilder.Entity<PermisosDTO>().HasNoKey();

        modelBuilder.Entity<ApiFunction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ApiFunct__3214EC078A7C26CC");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.ApiFunctionIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiFunctions_Usuario_Crea");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.ApiFunctionIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiFunctions_Usuario_Modifica");
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ApiKeys__3214EC070BD7848C");

            entity.ToTable("ApiKey");

            entity.Property(e => e.Clave)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.ApiKeyIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiKeys_Usuario_Crea");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.ApiKeyIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiKeys_Usuario_Modifica");
        });

        modelBuilder.Entity<ApiPermiso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ApiPermi__3214EC071D27C3DF");

            entity.ToTable("ApiPermiso");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.ApiPermisoIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiPermisos_Usuario_Crea");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.ApiPermisoIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiPermisos_Usuario_Modifica");
        });

        modelBuilder.Entity<ApiPermisosApiKeysTabla>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ApiPermi__3214EC07606E35B0");

            entity.ToTable("ApiPermisosApiKeysTabla");

            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");

            entity.HasOne(d => d.IdApiKeyNavigation).WithMany(p => p.ApiPermisosApiKeysTablas)
                .HasForeignKey(d => d.IdApiKey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiPermisosApiKeysTablas_ApiKeys");

            entity.HasOne(d => d.IdApiPermisoNavigation).WithMany(p => p.ApiPermisosApiKeysTablas)
                .HasForeignKey(d => d.IdApiPermiso)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiPermisosApiKeysTablas_ApiPermisos");

            entity.HasOne(d => d.IdTablaNavigation).WithMany(p => p.ApiPermisosApiKeysTablas)
                .HasForeignKey(d => d.IdTabla)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiPermisosApiKeysTablas_Tablas");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.ApiPermisosApiKeysTablaIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiPermisosApiKeysTablas_UsuarioCrea");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.ApiPermisosApiKeysTablaIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApiPermisosApiKeysTablas_UsuarioModifica");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Area__3214EC07B807D830");

            entity.ToTable("Area");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.AreaIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Area__IdUsuarioC__6B24EA82");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.AreaIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Area__IdUsuarioM__6C190EBB");
        });

        modelBuilder.Entity<Corte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Corte__3214EC07702F01F4");
            entity.ToTable("Corte");
            entity.Property(e => e.Inicia).HasColumnType("DATETIME");
            entity.Property(e => e.Termina).HasColumnType("DATETIME");
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("DATETIME").ValueGeneratedNever();
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getdate())").HasColumnType("DATETIME").ValueGeneratedNever();

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.CorteIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Corte__IdUsuario__74AE54BC");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.CorteIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Corte__IdUsuario__75A278F5");
        });


        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departam__3214EC07B2345851");

            entity.ToTable("Departamento");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.DepartamentoIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Departame__IdUsu__6D0D32F4");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.DepartamentoIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Departame__IdUsu__6E01572D");
        });

        // Configuración para Permiso
        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permiso__3214EC07503FFDCA");
            entity.ToTable("Permiso");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.Evidencia).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.FechaAutorizacion).HasColumnType("DATETIME");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("DATETIME").ValueGeneratedNever();
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getdate())").HasColumnType("DATETIME").ValueGeneratedNever();
            entity.Property(e => e.Motivo).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.Fecha1).HasColumnType("DATETIME");
            entity.Property(e => e.Fecha2).HasColumnType("DATETIME");
            entity.Property(e => e.Dias).ValueGeneratedNever();
            entity.Property(e => e.Goce).HasDefaultValue(0);

            entity.HasOne(d => d.IdCorteNavigation).WithMany(p => p.Permisos)
                .HasForeignKey(d => d.IdCorte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Permiso__IdCorte__2A6B46EF");

            entity.HasOne(d => d.IdTipoPermisoNavigation).WithMany(p => p.Permisos)
                .HasForeignKey(d => d.IdTipoPermiso)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Permiso__IdTipoP__2B5F6B28");

            entity.HasOne(d => d.IdUsuarioAutorizaNavigation).WithMany(p => p.PermisoIdUsuarioAutorizaNavigations)
                .HasForeignKey(d => d.IdUsuarioAutoriza)
                .HasConstraintName("FK__Permiso__IdUsuar__2F2FFC0C");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.PermisoIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Permiso__IdUsuar__2D47B39A");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.PermisoIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Permiso__IdUsuar__2E3BD7D3");

            entity.HasOne(d => d.IdUsuarioSolicitaNavigation).WithMany(p => p.PermisoIdUsuarioSolicitaNavigations)
                .HasForeignKey(d => d.IdUsuarioSolicita)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Permiso__IdUsuar__2C538F61");
        });

        modelBuilder.Entity<Puesto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Puesto__3214EC0719CC2913");

            entity.ToTable("Puesto");

            entity.Property(e => e.ClaveTabular)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.PuestoIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Puesto__IdUsuari__6EF57B66");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.PuestoIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Puesto__IdUsuari__6FE99F9F");
        });

        modelBuilder.Entity<Tabla>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tablas_T__3214EC0790040BB8");

            entity.ToTable("Tabla");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.TablaIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .HasConstraintName("FK_Tablas_Usuario_Crea");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.TablaIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .HasConstraintName("FK_Tablas_Usuario_Modifica");
        });

        modelBuilder.Entity<TipoPermiso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tipo_Per__3214EC07B72387FD");

            entity.ToTable("TipoPermiso");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.TipoPermisoIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tipo_Perm__IdUsu__70DDC3D8");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.TipoPermisoIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tipo_Perm__IdUsu__71D1E811");
        });

        modelBuilder.Entity<TipoUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tipo_Usu__3214EC07FD1734E4");

            entity.ToTable("TipoUsuario");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.TipoUsuarioIdUsuarioCreaNavigations)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tipo_Usua__IdUsu__72C60C4A");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.TipoUsuarioIdUsuarioModificaNavigations)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tipo_Usua__IdUsu__73BA3083");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuario__3214EC076D30FB0B");

            entity.ToTable("Usuario");

            entity.Property(e => e.Clave)
                .HasMaxLength(256)
                .IsUnicode(false);
            entity.Property(e => e.Correo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Estatus).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdAreaNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdArea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__IdArea__66603565");

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdDepartamento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__IdDepar__6754599E");

            entity.HasOne(d => d.IdPuestoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdPuesto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__IdPuest__68487DD7");

            entity.HasOne(d => d.IdTipoUsuarioNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdTipoUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__IdTipoU__656C112C");

            entity.HasOne(d => d.IdUsuarioCreaNavigation).WithMany(p => p.InverseIdUsuarioCreaNavigation)
                .HasForeignKey(d => d.IdUsuarioCrea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__IdUsuar__693CA210");

            entity.HasOne(d => d.IdUsuarioModificaNavigation).WithMany(p => p.InverseIdUsuarioModificaNavigation)
                .HasForeignKey(d => d.IdUsuarioModifica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__IdUsuar__6A30C649");
        });

        modelBuilder.Entity<VwPermisosUsuario>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_Permisos_Usuario");

            entity.Property(e => e.Correo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Evidencia)
                .HasMaxLength(22)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.FechaAutorizacion).HasPrecision(0);
            entity.Property(e => e.FechaCreacion).HasPrecision(0);
            entity.Property(e => e.FechaModificacion).HasPrecision(0);
            entity.Property(e => e.FechaSolicitud).HasPrecision(0);
            entity.Property(e => e.HoraEntrada).HasPrecision(0);
            entity.Property(e => e.HorarioCubrir).HasPrecision(0);
            entity.Property(e => e.IdTipoPermisos).HasColumnName("IdTipo_Permisos");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
