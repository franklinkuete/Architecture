using ClientApi.Domain.Interfaces;
using ClientApi.Infrastructure.Entities;

namespace ClientApi.Infrastructure.Repository;

public sealed class UnitOfWork : IUnitOfWorkClient
{
    private ClientDbContext _dbContext;
    private IClientRepository _clientRepository;
    public UnitOfWork(IClientRepository clientRepository, ClientDbContext dbContext)
    {
        _clientRepository = clientRepository; 
        _dbContext = dbContext;
    }
    public IClientRepository ClientRepository => _clientRepository;

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}