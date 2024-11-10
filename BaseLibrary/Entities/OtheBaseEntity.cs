
using System.ComponentModel.DataAnnotations;

namespace BaseLibrary.Entities;

public  class OtheBaseEntity
{
    public int Id { get; set; }
    [Required]
    public string CivilId { get; set; } = string.Empty;
    [Required]
    public string FileNumber { get; set; } = string.Empty;
    public string? Other {  get; set; }
}
