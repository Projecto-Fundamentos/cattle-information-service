using CattleInformationService.Shared.Domain.Repositories;
using CattleInformationService.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace CattleInformationService.Shared.Infrastructure.Persistence.EFC.Repositories;

public class UnitOfWork(AppDbContext appDbContext): IUnitOfWork
{
    public async Task CompleteAsync()
    {
        await appDbContext.SaveChangesAsync();
    }
}