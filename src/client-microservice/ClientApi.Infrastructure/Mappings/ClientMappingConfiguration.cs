using ClientApi.Application.Client;
using ClientApi.Domain.Entities;
using Mapster;
using Clientdb = ClientApi.Infrastructure.Entities.Client;

namespace ClientApi.Infrastructure.Mappings;

public class ClientMappingConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {

        // Conversion globale DateTime -> DateOnly
        TypeAdapterConfig.GlobalSettings.NewConfig<DateTime, DateOnly>()
            .MapWith(src => DateOnly.FromDateTime(src));

        // Conversion globale DateTime? -> DateOnly?
        TypeAdapterConfig.GlobalSettings.NewConfig<DateTime?, DateOnly?>()
            .MapWith(src => src.HasValue ? DateOnly.FromDateTime(src.Value) : null);

        config.NewConfig<ClientPOCO, Clientdb>();

        config.NewConfig<ClientPOCO, ClientResponse>();
         
        config.NewConfig<Clientdb, ClientPOCO>();

        config.NewConfig<Clientdb, ClientResponse>();

        config.NewConfig<ClientRequest, ClientPOCO>();
    }
}

