using Microsoft.EntityFrameworkCore;
using ProjectManagement.Api.DTOs;

namespace ProjectManagement.Api.Services;

internal static class Pagination
{
    public static async Task<PagedResponse<T>> ReadAsync<T>(IQueryable<T> orderedItems,
        int totalCount, PageQuery page, CancellationToken token)
    {
        var items = await orderedItems.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(token);
        return new(items, page.Page, page.PageSize, totalCount, (int)Math.Ceiling((double)totalCount / page.PageSize));
    }
}
