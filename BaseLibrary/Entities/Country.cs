
using System.Text.Json.Serialization;

namespace BaseLibrary.Entities;

public class Country : BaseEntity
{
    //one to many with City
    [JsonIgnore]
    public List<City>? Cities { get; set; }
}
