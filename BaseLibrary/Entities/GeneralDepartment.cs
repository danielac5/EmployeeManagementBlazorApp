
using System.Text.Json.Serialization;

namespace BaseLibrary.Entities;

public class GeneralDepartment : BaseEntity
{
    //relationships one to many with Department
    [JsonIgnore] public List<Department>? Departments { get; set; }

    // relationship one to many with Branch
    [JsonIgnore] public List<Branch>? Branches { get; set; }
}
