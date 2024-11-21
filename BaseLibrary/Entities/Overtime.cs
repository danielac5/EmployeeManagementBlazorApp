
using System.ComponentModel.DataAnnotations;

namespace BaseLibrary.Entities;

public class Overtime : OtherBaseEntity
{
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; private set; }
    public int NumberOfDays => (EndDate - StartDate).Days;

    //many to one with vacation type
    public OvertimeType? OvertimeType { get; set; }
    [Required]
    public int OvertimeTypeId { get; set; }
}
