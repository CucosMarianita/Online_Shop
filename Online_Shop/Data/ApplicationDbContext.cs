using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Online_Shop.Models;


// PASUL 3 - useri si roluri

namespace Online_Shop.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<ProductCart> ProductCarts { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            // definire primary key compus
            modelBuilder.Entity<ProductCart>()
                .HasKey(pc => new { pc.Id, pc.ProductId, pc.CartId });


            // definire relatii cu modelele Bookmark si Article (FK)
            modelBuilder.Entity<ProductCart>()
                .HasOne(pc => pc.Product)
                .WithMany(pc => pc.ProductCarts)
                .HasForeignKey(pc => pc.ProductId);

            modelBuilder.Entity<ProductCart>()
                .HasOne(pc => pc.Cart)
                .WithMany(pc => pc.ProductCarts)
                .HasForeignKey(pc => pc.CartId);
        }
    }
}