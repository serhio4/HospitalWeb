using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWeb.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Doctor Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Relatie (FK): Un doctor apartine unui departament
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Relatie: Un doctor are mai multe programari
        public ICollection<Appointment>? Appointments { get; set; }
    }
}