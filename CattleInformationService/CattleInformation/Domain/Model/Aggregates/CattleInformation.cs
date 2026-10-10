using CattleInformationService.CattleInformation.Domain.Model.Commands;

namespace CattleInformationService.CattleInformation.Domain.Model.Aggregates;

public class CattleInformation
{
    public int Id { get; set; }
    public int RanchId { get; set; }  
    public string EarTag { get; set; } = string.Empty;
    public string Breed { get; set; }
    public string Sex { get; set; }
    public DateOnly BirthDate { get; set; }
    public bool Status { get; set; }
    public string CurrentStage { get; set; }
    public string? SireEarTag { get; set; }
    public string? DamEarTag { get; set; }
    public string? ExitReason { get; set; }
    public DateOnly? ExitDate { get; set; }
    
    protected CattleInformation() {}

    public CattleInformation(CreateCattleInformationCommand command)
    {
        this.RanchId = command.RanchId;
        this.EarTag = command.EarTag;
        this.Breed = command.Breed;
        this.Sex = command.Sex;
        this.BirthDate = command.BirthDate;
        this.Status = command.Status;
        this.CurrentStage = command.CurrentStage;
        this.SireEarTag = command.SireEarTag;
        this.DamEarTag = command.DamEarTag;
        this.ExitReason = command.ExitReason;
        this.ExitDate = command.ExitDate;
    }
    
    
    
    
}