namespace CattleInformationService.CattleInformation.Interfaces.Rest.Resources;

public record CreateCattleInformationResource(
    int RanchId,  
    string EarTag,
    string Breed,
    string Sex,
    DateOnly BirthDate,
    bool Status,
    string CurrentStage,
    string SireEarTag,
    string DamEarTag,
    string ExitReason,
    DateOnly ExitDate
    );