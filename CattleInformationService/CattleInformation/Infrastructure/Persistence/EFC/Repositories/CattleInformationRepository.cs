using CattleInformationService.CattleInformation.Domain.Repositories;
using CattleInformationService.Shared.Infrastructure.Persistence.EFC.Configuration;
using CattleInformationService.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace CattleInformationService.CattleInformation.Infrastructure.Persistence.EFC.Repositories;

public class CattleInformationRepository(AppDbContext appDbContext): BaseRepository<Domain.Model.Aggregates.CattleInformation>(appDbContext), ICattleInformationRepository
{
    
}