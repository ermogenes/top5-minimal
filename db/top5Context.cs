using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace top5.db
{
    public partial class top5Context : DbContext
    {
        public top5Context()
        {
        }

        public top5Context(DbContextOptions<top5Context> options)
            : base(options)
        {
        }

        public virtual DbSet<Item> Item { get; set; } = null!;
        public virtual DbSet<Top> Top { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8_general_ci")
                .HasCharSet("utf8");

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(e => new { e.TopId, e.Posicao })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity.ToTable("item");

                entity.HasIndex(e => e.TopId, "fk_item_top_idx");

                entity.Property(e => e.TopId)
                    .HasMaxLength(36)
                    .HasColumnName("top_id");

                entity.Property(e => e.Posicao).HasColumnName("posicao");

                entity.Property(e => e.Curtidas).HasColumnName("curtidas");

                entity.Property(e => e.Nome)
                    .HasMaxLength(50)
                    .HasColumnName("nome");

                entity.HasOne(d => d.Top)
                    .WithMany(p => p.Item)
                    .HasForeignKey(d => d.TopId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_item_top");
            });

            modelBuilder.Entity<Top>(entity =>
            {
                entity.ToTable("top");

                entity.Property(e => e.Id)
                    .HasMaxLength(36)
                    .HasColumnName("id");

                entity.Property(e => e.Curtidas).HasColumnName("curtidas");

                entity.Property(e => e.Titulo)
                    .HasMaxLength(100)
                    .HasColumnName("titulo");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
