
using System.Text.Json.Serialization;

namespace BaseLibrary.Entities;

public class Department : BaseEntity
{
    //relationships many to one with General department
    public GeneralDepartment? GeneralDepartment { get; set; }
    public int GeneralDepartmentId { get; set; }

    // relationship one to many with Branch
    [JsonIgnore]
    public List<Branch>? Branches { get; set; }
}
