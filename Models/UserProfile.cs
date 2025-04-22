using System.ComponentModel.DataAnnotations;

namespace Closy.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome utente è obbligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Il nome utente deve essere tra 3 e 50 caratteri")]
        public string Username { get; set; }

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La password è obbligatoria")]
        public string Password { get; set; }
    }
}