namespace TaskManager.Helpers;

public static class PaginationHelper
{
    public static (int page, int pageSize) ValidateAndNormalize(int? page, int? pageSize, int defaultPage = 1, int defaultPageSize = 10, int maxPageSize = 50)
    {
        int currentPage = page.GetValueOrDefault(defaultPage);
        int currentPageSize = pageSize.GetValueOrDefault(defaultPageSize);

        if (currentPage < 1) throw new ArgumentException("page must be >= 1");
        if (currentPageSize < 1 || currentPageSize > maxPageSize)
            throw new ArgumentException($"pageSize must be between 1 and {maxPageSize}");

        return (currentPage, currentPageSize);
    }
}