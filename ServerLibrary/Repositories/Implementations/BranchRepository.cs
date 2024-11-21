using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations;

public class BranchRepository(AppDbContext appDbContext) : IGenericRepositoryInterface<Branch>
{
    public async Task<GeneralResponse> DeleteById(int id)
    {
        var branch = await appDbContext.Branches.FindAsync(id);
        if (branch is null) return NotFound();

        appDbContext.Branches.Remove(branch);
        await Commit();
        return DeleteSuccess();
    }

    public async Task<List<Branch>> GetAll() => await appDbContext.
        Branches.AsNoTracking().
        Include(d=>d.GeneralDepartment).
        ToListAsync();

    public async Task<Branch> GetById(int id) => await appDbContext.Branches.FindAsync(id);

    public async Task<GeneralResponse> Insert(Branch item)
    {
        if (!await CheckName(item.Name!)) return new GeneralResponse(false, "Filială adăugată deja");
        appDbContext.Branches.Add(item);
        await Commit();
        return InsertSuccess();
    }

    public async Task<GeneralResponse> Update(Branch item)
    {
        var branch = await appDbContext.Branches.FindAsync(item.Id);
        if (branch is null) return NotFound();
        branch.Name = item.Name;
        branch.GeneralDepartmentId = item.GeneralDepartmentId;
        await Commit();
        return UpdateSuccess();
    }

    private static GeneralResponse NotFound() => new(false, "Filiala nu a fost găsită");

    private static GeneralResponse InsertSuccess() => new(true, "Filială adăugată");
    private static GeneralResponse DeleteSuccess() => new(true, "Filială ștearsă");
    private static GeneralResponse UpdateSuccess() => new(true, "Filială modificată");

    private async Task Commit() => await appDbContext.SaveChangesAsync();

    private async Task<bool> CheckName(string name)
    {
        var item = await appDbContext.Branches.FirstOrDefaultAsync(x => x.Name!.ToLower().Equals(name.ToLower()));
        return item is null;
    }
}
