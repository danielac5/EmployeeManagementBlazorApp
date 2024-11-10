
namespace BaseLibrary.Entities;

public class VacationType : BaseEntity
{
    //many to one with vacation
    public List<Vacation>? Vacations { get; set; }
}
