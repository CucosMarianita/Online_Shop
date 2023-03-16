using System.ComponentModel.DataAnnotations;

namespace Online_Shop.Models
{
    public class Comment
    { 
        [Key]
        public int Id { get; set; }

        public int? Rating { get; set; }            /* intre 1-5 sau null */  


        [Required(ErrorMessage = "Continutul este obligatoriu")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int? ProductId { get; set; }

        public string? UserId { get; set; }

        // PASUL 6 - useri si roluri
        public virtual ApplicationUser? User { get; set; }     // un comm -> un sg utilizator

        public virtual Product? Product { get; set; }
    }
}
   

