﻿
using BaseLibrary.Entities;
using Microsoft.EntityFrameworkCore;

namespace ServerLibrary.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees { get; set; }

    // general department / department / branch
    public DbSet<GeneralDepartment> GeneralDepartments { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Branch> Branches { get; set; }

    // country / city / town
    public DbSet<Country> Countries { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Town> Towns { get; set; }

    // authentication / role / system roles
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<SystemRole> SystemRoles { get; set; } 
    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<RefreshTokenInfo> RefreshTokenInfos { get; set; }

    // other / vacations / sanctions / health / overtime
    public DbSet<Vacation> Vacations { get; set; }
    public DbSet<VacationType> VacationTypes { get; set; }

    public DbSet<Overtime> Overtimes { get; set; }
    public DbSet<OvertimeType> OvertimeTypes { get; set; }

    public DbSet<Sanction> Sanctions { get; set; }
    public DbSet<SanctionType> SanctionTypes { get; set; }

    public DbSet<Health> Health { get; set; }
}
