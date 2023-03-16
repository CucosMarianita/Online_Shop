using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Online_Shop.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [StringLength(100, ErrorMessage = "Titlul nu poate avea mai mult de 100 de caractere")]
        [MinLength(2, ErrorMessage = "Titlul trebuie sa aiba mai mult de 2 caractere")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Descrierea produsului este obligatorie")]
        public string Content { get; set; }


        // Adaugam un string unde vom salva calea imaginii pentru produs
        public string? Image { get; set; }


        [Required(ErrorMessage = "Pretul este obligatoriu")]
        [Range(1, 10000000, ErrorMessage = "Introduceti o valoare mai mare decat 0")]
        public int? Price { get; set; }

        public float? Stars { get; set; }

        public bool? Check { get; set; }    

        // ----------------------------------------------------------------

        [Required(ErrorMessage = "Categoria este obligatorie")]
        public int? CategoryId { get; set; }

        public string? UserId { get; set; }

        // PASUL 6 - useri si roluri
        // un produs apartine unui sg utilizator
        public virtual ApplicationUser? User { get; set; }

        public virtual Category? Category { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? Categ { get; set; }

        public virtual ICollection<ProductCart>? ProductCarts { get; set; }

        

    }
}
