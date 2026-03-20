using ClientApi.Domain.Entities;
using ClientApi.Domain.Interfaces;
using ClientApi.Infrastructure.Entities;
using Core.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;

internal class ClientRepository : IClientRepository
{
    private readonly IRepositoryBase<Client> _persistence;

    public ClientRepository(IRepositoryBase<Client> persistence)
    {
        _persistence = persistence;
    }

    public async Task<ClientPOCO?> AddClientAsync(ClientPOCO client)
    {
        var entity = client.Adapt<Client>();

        var createdClient = await _persistence.AddAsync(entity);
        return createdClient?.Adapt<ClientPOCO>();
    }

    public async Task<bool> ClientExistsAsync(string firstname, string lastname, DateOnly? dateNaissance)
    {
        // Utilisation d’AnyAsync pour exécuter directement en SQL
        return await _persistence.GetAll()
            .OrderBy(p => p.Id)
            .AnyAsync(c => c.Firstname == firstname
                        && c.Lastname == lastname
                        && c.Datenaissance == dateNaissance);
    }

    public async Task<IEnumerable<ClientPOCO>> GetAllClientAsync(int pageIndex, int pageSize)
    {
        // Pagination exécutée côté SQL
        var resultat = await _persistence.GetAll()
            .OrderBy(c => c.Id)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Adaptation après matérialisation
        return resultat.Select(c => c.Adapt<ClientPOCO>());
    }

    public async Task<ClientPOCO?> GetClientByIdAsync(int id)
    {
        var result = await _persistence.GetByIdAsync(id);
        return result?.Adapt<ClientPOCO>();
    }

    public async Task<bool> RemoveClientAsync(int clientId)
    {
        var deleted = await _persistence.DeleteAsync(clientId);
        return deleted;
    }
}