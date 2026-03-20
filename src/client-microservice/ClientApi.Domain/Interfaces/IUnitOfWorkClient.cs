namespace ClientApi.Domain.Interfaces;

public interface IUnitOfWorkClient : IDisposable
{
    IClientRepository ClientRepository { get; }
}

//🔹 Deux philosophies possibles
//1. Interfaces dans le domaine(DDD puriste)
//•	IClientRepository vit dans le domaine.
//•	Il ne retourne que des entités métier(Client).
//•	Les DTOs(ClientResponse) sont gérés plus haut(application/handler).
//•	➡️ Avantage : domaine totalement indépendant.
//•	➡️ Inconvénient : tu as souvent un double mapping(DBSet → Domain → DTO).
//________________________________________
//2. Interfaces dans l’application(pragmatique)
//•	IClientRepository vit dans la couche application.
//•	Il peut retourner directement des DTOs (ClientResponse).
//•	Tu peux utiliser ProjectToType<ClientResponse>() directement dans le repo.
//•	➡️ Avantage : un seul mapping, simplicité, moins de boilerplate.
//•	➡️ Inconvénient : ton domaine n’est plus totalement indépendant, mais ce n’est pas grave si tu assumes que ton application est le vrai point d’orchestration.
//________________________________________
//🔹 Pourquoi beaucoup d’architectes choisissent la 2ᵉ approche ?
//Parce que dans la pratique :
//•	Les repositories sont des abstractions d’accès aux données, pas des règles métier.
//•	Les règles métier vivent dans les services de domaine ou les aggregates.
//•	Les repositories sont donc plus proches de l’application que du domaine pur.
//En plaçant IClientRepository dans l’application :
//•	Tu peux retourner directement des DTOs.
//•	Tu simplifies tes handlers.
//•	Tu évites les successions de mapping.
//________________________________________
