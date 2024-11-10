
using System.ComponentModel.DataAnnotations;

namespace BaseLibrary.Entities;

public class Health : OtheBaseEntity
{
    [Required] public DateTime Date { get; set; }
    [Required] public string MedicalDiagnose { get; set; } = string.Empty;
    [Required] public string MedicalRecommandation {  get; set; } = string.Empty;    
}
