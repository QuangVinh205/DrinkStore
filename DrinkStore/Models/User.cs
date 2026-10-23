
using System.ComponentModel.DataAnnotations;

namespace DrinkStore.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(20)]
        public string Phone { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }
}
