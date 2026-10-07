using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Web.Services;

/// <summary>
/// DbContext dedicado ao repositório de chaves do Data Protection
/// (tabela "DataProtectionKeys"), separado do CooperativaDbContext para não
/// alterar o modelo nem as migrações da aplicação. Usado em ambientes com
/// banco externo (contêineres): as chaves sobrevivem a reinícios e são
/// compartilhadas entre múltiplas instâncias.
/// </summary>
public sealed class ChavesDataProtectionContext : DbContext, IDataProtectionKeyContext
{
    public ChavesDataProtectionContext(DbContextOptions<ChavesDataProtectionContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataProtectionKey>(entidade =>
        {
            entidade.ToTable("DataProtectionKeys");
            // "Id" é int com geração de valor (identity/sequence no PostgreSQL) —
            // convenção do EF para chave inteira; não usar ValueGeneratedNever.
            entidade.HasKey(e => e.Id);
        });
    }
}
