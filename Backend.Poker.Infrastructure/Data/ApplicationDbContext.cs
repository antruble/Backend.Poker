using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.Poker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Poker.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
        {
        }

        // DbSet-ek a fő entitásokhoz
        public DbSet<Game> Games { get; set; }
        public DbSet<Hand> Hands { get; set; }
        public DbSet<Player> Players { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ----- Game entitás konfigurációja -----
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Status)
                      .IsRequired();

                // Ha szükséges, definiálhatjuk a Game és a Hand közötti kapcsolatot.
                // Például, ha a Game-hez szeretnénk hozzárendelni a "CurrentHand"-et:
                entity.HasOne(g => g.CurrentHand)
                      .WithOne()  // Nincs visszamutató navigációs property a Hand oldalán
                      .HasForeignKey<Game>("CurrentHandId") // Shadow property, mely tárolja az aktuális kéz Id-jét
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(g => g.Players)
                      .WithOne() // Ha nincs navigációs property a Player oldalán
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // ----- Hand entitás konfigurációja -----
            modelBuilder.Entity<Hand>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.OwnsOne(h => h.Pot, pot =>
                {
                    // Az egyes pot értékek oszlopként tárolva, itt int típusúak (vagy decimal, ha szükséges)
                    pot.Property(p => p.MainPot)
                       .HasColumnType("int")
                       .HasColumnName("MainPot");

                    pot.Property(p => p.CurrentRoundPot)
                       .HasColumnType("int")
                       .HasColumnName("CurrentRoundPot");

                    // A hozzájárulások kollekciója (PlayerContribution) owned collectionként
                    pot.OwnsMany(p => p.Contributions, pc =>
                    {
                        pc.WithOwner().HasForeignKey("HandId"); // idegen kulcs a Hand-hez
                        pc.Property<Guid>("Id");
                        pc.HasKey("Id");
                        // További oszlopok, például a befizetett összeg:
                        pc.Property(x => x.Amount).HasColumnType("int");
                    });

                    // A side potok kollekciója
                    pot.OwnsMany(p => p.SidePots, sp =>
                    {
                        sp.WithOwner().HasForeignKey("HandId");
                        sp.Property<Guid>("Id");
                        sp.HasKey("Id");
                        // Feltételezzük, hogy SidePot-ben van egy Amount property és az EligiblePlayerIds-t nem tároljuk közvetlenül (vagy pl. stringként serializáljuk)
                        sp.Property(x => x.Amount).HasColumnType("int");
                        // Ha szükséges, az eligible játékosok azonosítóit is el lehet menteni pl. JSON stringként:
                        // sp.Property<string>("EligiblePlayerIdsJson").HasColumnName("EligiblePlayerIds");
                    });
                });

                // A CommunityCards egy értékobjektumokból álló kollekció, melyet owned entity-ként tárolunk
                entity.OwnsMany(h => h.CommunityCards, cc =>
                {
                    cc.WithOwner().HasForeignKey("HandId"); // Shadow property a kulcshoz
                    cc.Property<Guid>("Id");
                    cc.HasKey("Id");
                    cc.Property(c => c.Rank)
                      .IsRequired();
                    cc.Property(c => c.Suit)
                      .IsRequired();
                    cc.ToTable("CommunityCards"); // Külön tábla a community kártyáknak
                });

            });

            // ----- Player entitás konfigurációja -----
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name)
                      .IsRequired();
                entity.Property(p => p.Chips)
                      .HasColumnType("decimal(18,2)");

                // A HoleCards értékobjektumokból álló kollekció
                entity.OwnsMany(p => p.HoleCards, hc =>
                {
                    hc.WithOwner().HasForeignKey("PlayerId");
                    hc.Property<Guid>("Id");
                    hc.HasKey("Id");

                    hc.Property(c => c.Rank)
                      .IsRequired();
                    hc.Property(c => c.Suit)
                      .IsRequired();

                    hc.ToTable("HoleCards");
                });

                // A PlayerAction, mint value object, szintén owned kollekcióként kerül tárolásra.
                entity.OwnsMany(p => p.ActionsHistory, ah =>
                {
                    ah.WithOwner().HasForeignKey("PlayerId");
                    ah.Property<Guid>("Id");
                    ah.HasKey("Id");

                    ah.Property(a => a.ActionType)
                      .IsRequired();
                    ah.Property(a => a.Amount).HasColumnType("decimal(18,2)");
                    ah.Property(a => a.Timestamp)
                      .IsRequired();

                    ah.ToTable("PlayerActions");
                });
            });

            modelBuilder.Entity<Winner>(entity => 
            { 
                entity.HasKey(w => new { w.HandId, w.PlayerId });

                //entity.HasOne<Hand>()
                //    .WithMany(h => h.Winners)
                //    .HasForeignKey(w => w.HandId)
                //    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Player)
                    .WithMany()
                    .HasForeignKey(w => w.PlayerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.Property(w => w.Pot)
                    .HasColumnType("decimal(18,2)");
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
