
namespace BaseLibrary.Entities;

public class Town : BaseEntity
{
    //one to many with employee
    public List<Employee>? Employeses { get; set; }

    //many to one with City
    public City? City { get; set; }
    public int CityId { get; set; }
}
