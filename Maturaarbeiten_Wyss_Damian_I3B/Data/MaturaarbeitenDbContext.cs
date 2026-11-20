using Microsoft.EntityFrameworkCore;
using Maturaarbeiten_Wyss_Damian_I3B.Models;

namespace Maturaarbeiten_Wyss_Damian_I3B.Data
{
    public class MaturaarbeitenDbContext : DbContext
    {
        public MaturaarbeitenDbContext(DbContextOptions<MaturaarbeitenDbContext> options)
            : base(options)
        {
        }

        public DbSet<Raum> Raeume { get; set; }
        public DbSet<Person> Personen { get; set; }
        public DbSet<G4Praesentation> Praesentationen { get; set; }
        public DbSet<Buchung> Buchungen { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Raum>().ToTable("mat_raum");
            modelBuilder.Entity<Person>().ToTable("mat_person");
            modelBuilder.Entity<G4Praesentation>().ToTable("mat_g4_praesentation");

            modelBuilder.Entity<Buchung>(entity =>
            {
                entity.ToTable("mat_buchung", tb =>
                {
                    tb.HasTrigger("TR_mat_Buchung_Validierung");
                    tb.UseSqlOutputClause(false);
                });
            });

            modelBuilder.Entity<Person>()
                .HasIndex(p => p.Email)
                .IsUnique();

            modelBuilder.Entity<G4Praesentation>()
                .HasIndex(p => p.Thema)
                .IsUnique();
        }
    }
}