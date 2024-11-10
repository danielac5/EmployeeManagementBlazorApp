
namespace BaseLibrary.Entities;

public class Country : BaseEntity
{
    //one to many with City
    public List<City>? Cities { get; set; }
}
