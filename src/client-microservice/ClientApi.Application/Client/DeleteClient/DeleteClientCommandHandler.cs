using Ardalis.Result;
using ClientApi.Domain.Interfaces;
using Core.Interfaces;

namespace ClientApi.Application.Client.DeleteClient
{
    public record DeleteClientCommand(int id) : ICommand<bool>,ICacheInvalidator
    {
        public List<string> CacheKeysToInvalidate => new List<string> {
            $"{ClientConst.GetAllCacheKeyPrefix}*",
            $"{ClientConst.ItemCacheKeyPrefix}-{id}" }; 
    };
    public class DeleteClientCommandHandler : ICommandHandler<DeleteClientCommand, bool>
    {
        private readonly IUnitOfWorkClient _unitOfWork;
        public DeleteClientCommandHandler(IUnitOfWorkClient unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<bool>> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ClientRepository.RemoveClientAsync(request.id);
        }
    }
}
