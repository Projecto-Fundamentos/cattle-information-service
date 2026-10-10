namespace CattleInformationService.CattleInformation.Domain.Services.Queries;

public interface ICattleInformationQueryService
{
    Task<Model.Aggregates.CattleInformation?> Handle(int id);
}