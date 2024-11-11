
using System.Text.Json.Serialization;

namespace BaseLibrary.Entities;

public class VacationType : BaseEntity
{
    //many to one with vacation
    [JsonIgnore]
    public List<Vacation>? Vacations { get; set; }
}
