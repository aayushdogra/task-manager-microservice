namespace TaskManager.Helpers;

public static class PaginationHelper
{
    public static (int page, int pageSize) Normalize(int? page, int? pageSize, int defaultPage = 1, int defaultPageSize = 10, int maxPageSize = 50)
    {
        int currentPage = page.GetValueOrDefault(defaultPage);
        int currentPageSize = pageSize.GetValueOrDefault(defaultPageSize);

        if (currentPage <= 0) currentPage = defaultPage;
        if (currentPageSize <= 0) currentPageSize = defaultPageSize;
        if (currentPageSize > maxPageSize) currentPageSize = maxPageSize;

        return (currentPage, currentPageSize);
    }
}