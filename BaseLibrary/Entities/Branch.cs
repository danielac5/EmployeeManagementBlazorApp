using System.Text.Json.Serialization;

namespace BaseLibrary.Entities;

public class Branch : BaseEntity
{
    //relationships many to one with general department
    public GeneralDepartment? GeneralDepartment { get; set; }
    public int GeneralDepartmentId { get; set; }

    //relationship one to many with Employee
    [JsonIgnore]
    public List<Employee>? Employees { get; set; }
}
