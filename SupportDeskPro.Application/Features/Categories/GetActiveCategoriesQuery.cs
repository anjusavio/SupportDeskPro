/// <summary>
/// Query model for fetching active categories for ticket creation dropdown.
/// Available to all authenticated roles.
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Categories;

namespace SupportDeskPro.Application.Features.Categories.GetActiveCategories;

public record GetActiveCategoriesQuery : IRequest<List<CategorySummaryResponse>>;