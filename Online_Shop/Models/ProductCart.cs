using System.ComponentModel.DataAnnotations.Schema;

namespace Online_Shop.Models
{
    public class ProductCart
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ProductId { get; set; }
        public int? CartId { get; set; }

        // Data si ora la care a fost adaugat un produs in cart
        public DateTime CartDate { get; set; }

        //-------------------------------------------------
        public virtual Product? Product { get; set; }
        public virtual Cart? Cart { get; set; }
        
    }
}
