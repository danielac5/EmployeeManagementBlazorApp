
using System.ComponentModel.DataAnnotations;

namespace BaseLibrary.DTO;

public class EmployeeGroup2
{
    [Required] public string JobName { get; set; } = string.Empty;

    [Required, Range(1, 99999, ErrorMessage = "Selectează filiala")]
    public int BranchId { get; set; }

    [Required, Range(1, 99999, ErrorMessage = "Selectează regiunea")]
    public int TownId { get; set; }

    [Required] public string? Other { get; set; }
}
