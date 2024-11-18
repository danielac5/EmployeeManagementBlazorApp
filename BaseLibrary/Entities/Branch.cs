using System.Text.Json.Serialization;

namespace BaseLibrary.Entities;

public class Branch : BaseEntity
{
    //relationships many to one with Department
    public Department? Department { get; set; }
    public int DepartmentId { get; set; }

    //relationship one to many with Employee
    [JsonIgnore]
    public List<Employee>? Employees { get; set; }
}
