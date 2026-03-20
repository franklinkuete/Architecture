using Ardalis.Result;
using System.Net;

namespace Core.Models;

/// <summary>
/// Structure standardisée pour toutes les réponses JSON de l'API.
/// </summary>
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }           // True si l'opération a réussi
    public bool IsFailure { get; set; }           // True si l'opération a échoué (doublon utile pour le JS)
    public T? Value { get; set; }                  // Les données utiles (DTO, ID, Liste...)
    public List<ValidationError>? Errors { get; set; } // Liste des erreurs si IsFailure est true
    public HttpStatusCode Status { get; set; }     // Rappel du code HTTP (200, 201, 400...)
}

