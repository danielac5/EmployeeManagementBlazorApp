
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations;

public class GeneralDepartmentRepository(AppDbContext appDbContext) : IGenericRepositoryInterface<GeneralDepartment>
{
    public async Task<GeneralResponse> DeleteById(int id)
    {
        var dep = await appDbContext.GeneralDepartments.FindAsync(id);
        if (dep is null) return NotFound();
        
        appDbContext.GeneralDepartments.Remove(dep);
        await Commit();
        return DeleteSuccess();
    }

    public async Task<List<GeneralDepartment>> GetAll() => await appDbContext.GeneralDepartments.ToListAsync();

    public async Task<GeneralDepartment> GetById(int id) => await appDbContext.GeneralDepartments.FindAsync(id);

    public async Task<GeneralResponse> Insert(GeneralDepartment item)
    {
        var checkIfNull = await CheckName(item.Name);
        if (!checkIfNull) 
            return new GeneralResponse(false, "Departament general deja adăugat");
        appDbContext.GeneralDepartments.Add(item);
        await Commit();
        return InsertSuccess();
    }
     
    public async Task<GeneralResponse> Update(GeneralDepartment item)
    {
        var dep = await appDbContext.GeneralDepartments.FindAsync(item.Id);
        if (dep is null) return NotFound();
        dep.Name = item.Name;
        await Commit();
        return UpdateSuccess();
    }

    private static GeneralResponse NotFound() => new(false, "Departamentul general nu a fost găsit");

    private static GeneralResponse DeleteSuccess() => new(true, "Departament general șters");
    private static GeneralResponse InsertSuccess() => new(true, "Departament general adăugat");
    private static GeneralResponse UpdateSuccess() => new(true, "Departament general modificat");

    private async Task Commit() => await appDbContext.SaveChangesAsync();

    private async Task<bool> CheckName(string name)
    {
        var item = await appDbContext.GeneralDepartments.FirstOrDefaultAsync(x => x.Name!.ToLower().Equals(name.ToLower()));
        return item is null;
    }
}
