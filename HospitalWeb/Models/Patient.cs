using System.ComponentModel.DataAnnotations;

namespace HospitalWeb.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string CNP { get; set; } // Identificator unic

        // Relatie: Un pacient are mai multe programari
        public ICollection<Appointment>? Appointments { get; set; }
    }
}