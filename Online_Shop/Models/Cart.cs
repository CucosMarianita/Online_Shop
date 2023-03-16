using System.ComponentModel.DataAnnotations;

namespace Online_Shop.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

       /* [Required(ErrorMessage = "Numele colectiei este obligatoriu")]
        public string Name { get; set; }*/

        public string? UserId { get; set; }

        // -------------------
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<ProductCart>? ProductCarts { get; set; }

    }
}
 