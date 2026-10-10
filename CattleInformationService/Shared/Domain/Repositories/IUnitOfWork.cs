namespace CattleInformationService.Shared.Domain.Repositories;

public interface IUnitOfWork
{
    Task CompleteAsync();
}