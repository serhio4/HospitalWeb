using System.ComponentModel.DataAnnotations;

namespace HospitalWeb.Models
{
    // Aici definim starile posibile
    public enum AppointmentStatus
    {
        Pending,    // 0 - In asteptare (Galben)
        Confirmed,  // 1 - Confirmata de doctor (Albastru)
        Completed,  // 2 - Finalizata (Verde) - Doar astea primesc Review
        Cancelled   // 3 - Anulata (Rosu)
    }

    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; }

        // Proprietatea noua pentru Status
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        // Relatii FK
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        public Review? Review { get; set; }
    }
}