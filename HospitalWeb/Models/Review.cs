using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWeb.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Range(1, 5, ErrorMessage = "Nota trebuie sa fie intre 1 si 5")]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        // Relatie (FK): Un review apartine unei singure programari
        public int AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }
    }
}