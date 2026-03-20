using CommandeApi.Domain.Interfaces;

namespace CommandeApi.Infrastructure.Repositories
{
    public sealed class UnitOfWork : IUnitOfWorkCommande
    {
        private ICommandeRepository? _commandeRepository;
        public UnitOfWork(ICommandeRepository commandeRepository)
        {
            _commandeRepository = commandeRepository;
        }
        public ICommandeRepository CommandeRepository => _commandeRepository!;

        public void Dispose()
        {
            _commandeRepository = null;
        }
    }
}
