using CattleInformationService.CattleInformation.Domain.Repositories;
using CattleInformationService.CattleInformation.Domain.Services.Queries;

namespace CattleInformationService.CattleInformation.Application.Internal.Queries;

public class CattleInformationQueryService(
    ICattleInformationRepository repository
    ): ICattleInformationQueryService
{
    public async Task<Domain.Model.Aggregates.CattleInformation?> Handle(int id)
    {
        return await repository.FindByIdAsync(id);
    }
}