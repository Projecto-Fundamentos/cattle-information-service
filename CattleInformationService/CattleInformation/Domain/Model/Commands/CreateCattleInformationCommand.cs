namespace CattleInformationService.CattleInformation.Domain.Model.Commands;

public record CreateCattleInformationCommand(
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