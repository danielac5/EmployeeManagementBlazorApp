﻿
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;
using ServerLibrary.Repositories.Contracts;

namespace ServerLibrary.Repositories.Implementations;

public class DepartmentRepository(AppDbContext appDbContext) : IGenericRepositoryInterface<Department>
{
    public async Task<GeneralResponse> DeleteById(int id)
    {
        var dep = await appDbContext.Departments.FindAsync(id);
        if (dep is null) return NotFound();

        appDbContext.Departments.Remove(dep);
        await Commit();
        return DeleteSuccess();
    }

    public async Task<List<Department>> GetAll() => await appDbContext
        .Departments.AsNoTracking() //to improve performance, cause we are just reading the data
        .Include(gd => gd.GeneralDepartment)
        .ToListAsync();

    public async Task<Department> GetById(int id) => await appDbContext.Departments.FindAsync(id);

    public async Task<GeneralResponse> Insert(Department item)
    {
        if (!await CheckName(item.Name!)) return new GeneralResponse(false, "Departament deja adăugat");
        appDbContext.Departments.Add(item);
        await Commit();
        return InsertSuccess();
    }

    public async Task<GeneralResponse> Update(Department item)
    {
        var dep = await appDbContext.Departments.FindAsync(item.Id);
        if (dep is null) return NotFound();
        dep.Name = item.Name;
        dep.GeneralDepartmentId = item.GeneralDepartmentId;
        await Commit();
        return UpdateSuccess();
    }

    private static GeneralResponse NotFound() => new(false, "Departamentul nu a fost găsit");

    private static GeneralResponse InsertSuccess() => new(true, "Departament adăugat");
    private static GeneralResponse DeleteSuccess() => new(true, "Departament șters");
    private static GeneralResponse UpdateSuccess() => new(true, "Departament modificat");

    private async Task Commit() => await appDbContext.SaveChangesAsync();

    private async Task<bool> CheckName(string name)
    {
        var item = await appDbContext.Departments.FirstOrDefaultAsync(x => x.Name!.ToLower().Equals(name.ToLower()));
        return item is null;
    }
}
