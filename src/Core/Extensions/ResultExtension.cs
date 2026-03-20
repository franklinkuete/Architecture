using Ardalis.Result;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Core.Extensions;

public static class ResultExtensions
{
    // Extension destinée à convertir un Result<T> (Ardalis.Result)
    // en une ActionResult<ApiResponse<T>> utilisable par ASP.NET Core.
    // ⚠️ Important : on ne gère pas ici les erreurs 500 (exceptions imprévues),
    // elles doivent être capturées par ton GlobalExceptionMiddleware.
    public static ActionResult<ApiResponse<T>> ToApiResponse<T>(this Result<T> result)
    {
        // Cas 1 : succès classique
        if (result.IsSuccess)
        {
            return new OkObjectResult(new ApiResponse<T>
            {
                IsSuccess = true,
                IsFailure = false,
                Value = result.Value,              // valeur renvoyée par le handler
                Status = HttpStatusCode.OK,        // code HTTP 200
                Errors = null
            });
        }

        // Cas 2 : statut "Created"
        // ⚠️ Attention : tu utilises NotFoundObjectResult ici,
        // mais le Status est "Created" (201). Cela peut être incohérent.
        // Normalement, on devrait utiliser CreatedAtAction ou CreatedResult.
        if(result.Status == ResultStatus.Created)
        {
            // Ici, on utilise CreatedResult (ou CreatedAtAction si tu veux préciser une route)
            return new CreatedResult(string.Empty, new ApiResponse<T>
            {
                IsSuccess = true,
                IsFailure = false,
                Value = result.Value,                // tu peux renvoyer la valeur créée
                Status = HttpStatusCode.Created,
                Errors = null
            });
        }


        // Cas 3 : échec de validation ou autre erreur attendue
        // On renvoie un 400 BadRequest avec les erreurs de validation.
        return new BadRequestObjectResult(new ApiResponse<T>
        {
            IsSuccess = false,
            IsFailure = true,
            Value = default,                       // pas de valeur
            Status = HttpStatusCode.BadRequest,    // code HTTP 400
            Errors = result?.ValidationErrors?.ToList()
        });
    }
}

