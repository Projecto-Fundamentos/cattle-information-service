using CattleInformationService.CattleInformation.Domain.Model.Commands;
using CattleInformationService.CattleInformation.Domain.Repositories;
using CattleInformationService.CattleInformation.Domain.Services.Commands;
using CattleInformationService.Shared.Domain.Repositories;

namespace CattleInformationService.CattleInformation.Application.Internal.Command;

public class CattleInformationCommandService(
    ICattleInformationRepository  cattleInformationRepository,
    IUnitOfWork unitOfWork
    ): ICattleInformationCommandService
{
    public async Task<Domain.Model.Aggregates.CattleInformation?> Handle(CreateCattleInformationCommand command)
    {
        var cattleInformation = new Domain.Model.Aggregates.CattleInformation(command);
        await cattleInformationRepository.AddAsync(cattleInformation);
        await unitOfWork.CompleteAsync();
        return cattleInformation;
        
    }
}