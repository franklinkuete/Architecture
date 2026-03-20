

public interface ICachedQuery<TResponse> : IQuery<TResponse>,ICachedQuery
{
    
}

public interface ICachedQuery
{
    string CacheKey { get; }
    CachePolicy Policy { get; }
}

