using CattleInformationService.CattleInformation.Domain.Model.Commands;
using CattleInformationService.CattleInformation.Interfaces.Rest.Resources;

namespace CattleInformationService.CattleInformation.Interfaces.Rest.Transform;

public static class CreateCattleInformationCommandFromResourceAssembler
{
    public static CreateCattleInformationCommand ToCommandFromResource(CreateCattleInformationResource resource)
    {
        return new CreateCattleInformationCommand(
            resource.RanchId,
            resource.EarTag,
            resource.Breed,
            resource.Sex,
            resource.BirthDate,
            resource.Status,
            resource.CurrentStage,
            resource.SireEarTag,
            resource.DamEarTag,
            resource.ExitReason,
            resource.ExitDate
        );
    } 
}