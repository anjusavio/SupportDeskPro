/// <summary>
/// Query model for retrieving single category detail by Id (Admin only).
/// </summary>
using MediatR;
using SupportDeskPro.Contracts.Categories;

namespace SupportDeskPro.Application.Features.Categories.GetCategoryById;

public record GetCategoryByIdQuery(Guid CategoryId)
    : IRequest<CategoryResponse>;