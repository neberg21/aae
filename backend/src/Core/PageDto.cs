namespace Core;

public abstract record PageDto<T>
{
    public required T[] Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageSize { get; init; }
    public required int PageNumber { get; init; }
    public required int TotalPages { get; init; }
}