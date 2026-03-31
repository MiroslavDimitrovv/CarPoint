using System.ComponentModel.DataAnnotations;

namespace CarPoint.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int CarId { get; set; }

        public ApplicationUser? User { get; set; }
        public Car? Car { get; set; }
    }
}
