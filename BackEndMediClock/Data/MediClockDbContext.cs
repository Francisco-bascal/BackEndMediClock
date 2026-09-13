using BackEndMediClock.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEndMediClock.Data
{
    public class MediClockDbContext: DbContext
    {
        public MediClockDbContext(DbContextOptions<MediClockDbContext> options) :base(options) { }

        public DbSet<Dispositivo> Dispositivos { get; set; }
        public DbSet<Alarma> Alarmas { get; set; }
        public DbSet<Evento> Eventos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //1-N Dispositivo-Alarmas
            modelBuilder.Entity<Alarma>(e =>
                e.HasOne(a => a.Dispositivo)
                .WithMany(d => d.Alarmas)
                .HasForeignKey(a => a.DispositivoId)
                .OnDelete(DeleteBehavior.Restrict)
            );

            //Creación de una clave para mantener la unicidad de una alarma de un número dado de un dispositivo para un día dado.
            modelBuilder.Entity<Alarma>(e =>
                e.HasIndex(a => new 
                {
                    a.DispositivoId,
                    a.DiaSemana,
                    a.NumeroAlarma
                }).IsUnique()
            );

            //1-N Dispositivo-Eventos
            modelBuilder.Entity<Evento>(e =>
                e.HasOne(ev => ev.Dispositivo)
                .WithMany(d => d.Eventos)
                .HasForeignKey(ev => ev.DispositivoId)
                .OnDelete(DeleteBehavior.Restrict)
            );

            //1-N Alarma-Eventos
            modelBuilder.Entity<Evento>(e =>
                e.HasOne(ev => ev.Alarma)
                .WithMany(a => a.Eventos)
                .HasForeignKey(ev => ev.AlarmaId)
                .OnDelete(DeleteBehavior.Restrict)
            );
        }
    }
}