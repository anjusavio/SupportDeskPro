/// <summary>
/// Query model for retrieving paginated category list for Admin management view.
/// </summary>
using MediatR;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Contracts.Categories;

namespace SupportDeskPro.Application.Features.Categories.GetCategories;

public record GetCategoriesQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null
) : IRequest<PagedResult<CategoryResponse>>;