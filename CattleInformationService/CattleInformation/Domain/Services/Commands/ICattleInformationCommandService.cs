using CattleInformationService.CattleInformation.Domain.Model.Commands;

namespace CattleInformationService.CattleInformation.Domain.Services.Commands;

public interface ICattleInformationCommandService
{
    Task<Model.Aggregates.CattleInformation?> Handle(CreateCattleInformationCommand command);
}