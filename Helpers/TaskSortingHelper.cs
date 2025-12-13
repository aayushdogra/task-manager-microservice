using TaskManager.Dto;
using TaskManager.Models;
using System.Linq;

namespace TaskManager.Helpers;

public static class TaskSortingHelper
{
    // Apply primary sorting + stable secondary sorting by (Id) with direction-aware ThenBy
    public static IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, TaskSortBy sortBy, SortDirection direction)
    {
        return (sortBy, direction) switch
        {
            (TaskSortBy.Title, SortDirection.Asc) =>
                query.OrderBy(t => t.Title).ThenBy(t => t.Id),

            (TaskSortBy.Title, SortDirection.Desc) =>
                query.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id),

            (TaskSortBy.UpdatedAt, SortDirection.Asc) =>
                query.OrderBy(t => t.UpdatedAt).ThenBy(t => t.Id),

            (TaskSortBy.UpdatedAt, SortDirection.Desc) =>
                query.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id),

            (TaskSortBy.CreatedAt, SortDirection.Asc) =>
                query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),

            _ =>
                query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
        };
    }
}