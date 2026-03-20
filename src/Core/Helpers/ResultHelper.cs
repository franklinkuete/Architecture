using Ardalis.Result;
using System.Linq.Expressions;

namespace Core.Helpers;

public static class ResultFactory<TResponse>
{
    private static readonly Type _valueType = typeof(TResponse).GetGenericArguments()[0];

    // Factories compilées
    private static readonly Func<object, TResponse> _successFactory = CreateSuccessFactory();
    private static readonly Func<IEnumerable<ValidationError>, TResponse> _invalidFactory = CreateInvalidFactory();

    // 1. Factory pour Success(T value)
    private static Func<object, TResponse> CreateSuccessFactory()
    {
        var method = typeof(Result<>).MakeGenericType(_valueType).GetMethod("Success", [_valueType])!;
        var param = Expression.Parameter(typeof(object), "value");
        var call = Expression.Call(method, Expression.Convert(param, _valueType));
        return Expression.Lambda<Func<object, TResponse>>(call, param).Compile();
    }

    // 2. Factory pour Invalid(IEnumerable<ValidationError> errors)
    private static Func<IEnumerable<ValidationError>, TResponse> CreateInvalidFactory()
    {
        // On cherche la méthode Invalid sur Result<TValue>
        var method = typeof(Result<>)
            .MakeGenericType(_valueType)
            .GetMethod("Invalid", [typeof(IEnumerable<ValidationError>)])!;

        var param = Expression.Parameter(typeof(IEnumerable<ValidationError>), "errors");
        var call = Expression.Call(method, param);

        return Expression.Lambda<Func<IEnumerable<ValidationError>, TResponse>>(call, param).Compile();
    }

    // Méthodes d'accès publiques
    public static TResponse Success(object data) => _successFactory(data);
    public static TResponse Invalid(IEnumerable<ValidationError> errors) => _invalidFactory(errors);
}
