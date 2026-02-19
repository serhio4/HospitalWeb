using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace HospitalWeb.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele departamentului este obligatoriu.")]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; }

        // Relatie: Un departament are mai multi doctori
        public ICollection<Doctor>? Doctors { get; set; }
    }
}