
using System.Text.Json.Serialization;

namespace BaseLibrary.Entities;

public class OvertimeType : BaseEntity
{
    //many to one relationship with overtime
    [JsonIgnore]
    public List<Overtime>? Overtimes { get; set; }
}
