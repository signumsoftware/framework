
using Signum.Utilities.ExpressionTrees;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Utilities;

/// <summary>
/// 1-based index
/// </summary>
public class Page<T>
{
    public int StartElementIndex
    {
        get { return (ElementsPerPage * (CurrentPage - 1)) + 1; }
    }

    public int EndElementIndex
    {
        get { return StartElementIndex + Elements.Count - 1; }
    }

    public int TotalElements { get; private set; }

    public int TotalPages
    {
        get { return (TotalElements + ElementsPerPage - 1) / ElementsPerPage; } //Round up
    }

    public int CurrentPage { get; private set; }
    public int ElementsPerPage { get; private set; }

    public List<T> Elements { get; private set; }

    public Page(int totalElements, int currentPage, int elementsPerPage, List<T> elements)
    {
        this.TotalElements = totalElements;
        this.CurrentPage = currentPage;
        this.ElementsPerPage = elementsPerPage;
        this.Elements = elements;
    }
}

public static class PageExtensions
{
    public static Page<T> Paginate<T>(this IQueryable<T> source, int elementsPerPage, int currentPage)
    {
        var list = source.Skip((currentPage - 1) * elementsPerPage).Take(elementsPerPage).ToList();
        var count = source.Count();

        return new Page<T>(count, currentPage, elementsPerPage, list);
    }

    public async static Task<Page<T>> PaginateAsync<T>(this IQueryable<T> source, int elementsPerPage, int currentPage, CancellationToken token)
    {
        var list = await source.Skip((currentPage - 1) * elementsPerPage).Take(elementsPerPage).ToListAsync();
        var count = await source.CountAsync();

        return new Page<T>(count, currentPage, elementsPerPage, list);
    }

    public static Page<T> Paginate<T>(this IEnumerable<T> source, int elementsPerPage, int currentPage)
    {
        var list = source.Skip((currentPage - 1) * elementsPerPage).Take(elementsPerPage).ToList();
        var count = source.Count();

        return new Page<T>(count, currentPage, elementsPerPage, list);
    }

    public static IEnumerable<T> TryTake<T>(this IEnumerable<T> source, int? count)
    {
        if (count == null)
            return source;

        return source.Take(count.Value);
    }

    public static IQueryable<T> TryTake<T>(this IQueryable<T> source, int? count)
    {
        if (count == null)
            return source;

        return source.Take(count.Value);
    }
}
