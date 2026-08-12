using Anvil.Interfaces;
using ForgeKit.Api.Data;

namespace ForgeKit.Api.Foundations;

public class PocDataSeeder : IDataSeeder
{
    private readonly AppDbContext _dbContext;

    public PocDataSeeder(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Seed()
    {
        // Legacy seed data removed; referenced entity namespaces (Visits, Educational, Compliance)
        // no longer exist in this version of the schema.
    }
}
