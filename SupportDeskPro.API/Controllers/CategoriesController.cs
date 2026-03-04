/// <summary>
/// REST controller for ticket category management.
/// Admin manages all categories. All authenticated roles
/// can fetch active categories for ticket creation dropdown.
/// </summary>
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDeskPro.Application.Features.Categories.CreateCategory;
using SupportDeskPro.Application.Features.Categories.GetActiveCategories;
using SupportDeskPro.Application.Features.Categories.GetCategories;
using SupportDeskPro.Application.Features.Categories.GetCategoryById;
using SupportDeskPro.Application.Features.Categories.UpdateCategory;
using SupportDeskPro.Application.Features.Categories.UpdateCategoryStatus;
using SupportDeskPro.Application.Features.Tenants.GetTenants;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Categories;
using SupportDeskPro.Contracts.Common;

namespace SupportDeskPro.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves paginated list of all categories in the current tenant.
    /// Supports filtering by active status.
    /// Includes ticket count per category and parent category name.
    /// Results ordered by SortOrder then alphabetically by name.
    /// </summary>
    // GET /api/categories (Admin only)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(page, pageSize, isActive));
        return Ok(ApiResponse<PagedResult<CategoryResponse>>.Ok(result));
    }

    /// <summary>
    /// Retrieves single category detail by Id including parent category name
    /// and total ticket count assigned to this category.
    /// Returns 404 if category does not exist in the current tenant.
    /// </summary>
    // GET /api/categories/{id} (Admin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        return Ok(ApiResponse<CategoryResponse>.Ok(result));
    }

    /// <summary>
    /// Creates a new ticket category in the current tenant.
    /// Supports parent/sub-category hierarchy via ParentCategoryId.
    /// SortOrder controls display order in dropdowns and lists.
    /// Returns 409 if category name already exists in the tenant.
    /// Returns 404 if specified ParentCategoryId does not exist.
    /// </summary>
    // POST /api/categories (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        [FromServices] ICurrentTenantService tenantService)
    {
        var result = await _mediator.Send(
            new CreateCategoryCommand(
                tenantService.TenantId ?? Guid.Empty,
                request.Name,
                request.Description,
                request.ParentCategoryId,
                request.SortOrder));

        return CreatedAtAction(nameof(GetById), new { id = result.CategoryId },
            ApiResponse<string>.Ok( result.CategoryId.ToString()!, result.Message));
    }

    /// <summary>
    /// Updates category name, description, parent category and sort order.
    /// Validates name uniqueness excluding the current category.
    /// Prevents circular reference — category cannot be its own parent.
    /// Returns 404 if category does not exist in the current tenant.
    /// Returns 409 if updated name already exists in another category.
    /// </summary>
    // PUT /api/categories/{id} (Admin only)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request)
    {
        var result = await _mediator.Send(
            new UpdateCategoryCommand(
                id,
                request.Name,
                request.Description,
                request.ParentCategoryId,
                request.SortOrder));

        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    /// <summary>
    /// Activates or deactivates a category.
    /// Deactivated categories are hidden from ticket creation dropdown.
    /// Existing tickets retain their category assignment — not affected.
    /// Returns 404 if category does not exist in the current tenant.
    /// </summary>
    // PATCH /api/categories/{id}/status (Admin only)
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateCategoryStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateCategoryStatusCommand(id, request.IsActive));
        return Ok(ApiResponse<string>.Ok(result.Message));
    }

    /// <summary>
    /// Retrieves all active categories for ticket creation dropdown.
    /// Available to all authenticated roles — Customer, Agent and Admin.
    /// Returns Id, name and parent category for each active category.
    /// Results ordered by SortOrder then alphabetically by name.
    /// </summary>
    // GET /api/categories/active (All authenticated roles)
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _mediator.Send(new GetActiveCategoriesQuery());
        return Ok(ApiResponse<List<CategorySummaryResponse>>.Ok(result));
    }
}
