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

    // GET /api/categories/{id} (Admin only)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        return Ok(ApiResponse<CategoryResponse>.Ok(result));
    }

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

    // GET /api/categories/active (All authenticated roles)
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _mediator.Send(new GetActiveCategoriesQuery());
        return Ok(ApiResponse<List<CategorySummaryResponse>>.Ok(result));
    }
}
