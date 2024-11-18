
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations;

public class CountryRepository(AppDbContext appDbContext) : IGenericRepositoryInterface<Country>
{
    public async Task<GeneralResponse> DeleteById(int id)
    {
        var dep = await appDbContext.Countries.FindAsync(id);
        if (dep is null) return NotFound();

        appDbContext.Countries.Remove(dep);
        await Commit();
        return DeleteSuccess();
    }

    public async Task<List<Country>> GetAll() => await appDbContext.Countries.ToListAsync();

    public async Task<Country> GetById(int id) => await appDbContext.Countries.FindAsync(id);

    public async Task<GeneralResponse> Insert(Country item)
    {
        if (!await CheckName(item.Name!)) return new GeneralResponse(false, "Țară deja adăugată");
        appDbContext.Countries.Add(item);
        await Commit();
        return InsertSuccess();
    }

    public async Task<GeneralResponse> Update(Country item)
    {
        var dep = await appDbContext.Countries.FindAsync(item.Id);
        if (dep is null) return NotFound();
        dep.Name = item.Name;
        await Commit();
        return UpdateSuccess();
    }

    private static GeneralResponse NotFound() => new(false, "Țară nu a fost găsită");

    private static GeneralResponse InsertSuccess() => new(true, "Țară adăugată");
    private static GeneralResponse DeleteSuccess() => new(true, "Țară ștearsă");
    private static GeneralResponse UpdateSuccess() => new(true, "Țară modificată");

    private async Task Commit() => await appDbContext.SaveChangesAsync();

    private async Task<bool> CheckName(string name)
    {
        var item = await appDbContext.Countries.FirstOrDefaultAsync(x => x.Name!.ToLower().Equals(name.ToLower()));
        return item is null;
    }
}
