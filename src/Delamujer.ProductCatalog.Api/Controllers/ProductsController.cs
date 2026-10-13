using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Delamujer.ProductCatalog.Api.Controllers;

/// <summary>
/// Gestión y administración del catálogo de productos e inventario transaccional concurrente.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Consulta el listado paginado de productos con filtros de búsqueda y estado.
    /// </summary>
    /// <param name="query">Parámetros de paginación y filtros.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Colección paginada de productos.</returns>
    /// <response code="200">Retorna el listado paginado de productos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> GetPaged(
        [FromQuery] ProductQueryParams query,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle de un producto por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del producto (GUID).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalles del producto.</returns>
    /// <response code="200">Producto encontrado.</response>
    /// <response code="404">No existe un producto con el identificador proporcionado.</response>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    /// <summary>
    /// Crea un nuevo producto en el catálogo con su stock inicial.
    /// </summary>
    /// <param name="dto">Datos del producto a registrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Producto creado con su identificador asignado y URL en cabecera Location.</returns>
    /// <response code="201">Producto creado exitosamente.</response>
    /// <response code="400">Datos inválidos o regla de negocio vulnerada.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponseDto>> Create(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _productService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Actualiza la información general de un producto existente.
    /// </summary>
    /// <param name="id">Identificador del producto a actualizar.</param>
    /// <param name="dto">Nuevos valores para nombre, descripción y precio.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Producto actualizado.</returns>
    /// <response code="200">Producto actualizado exitosamente.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="404">Producto no encontrado.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> Update(
        Guid id,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _productService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Actualiza el inventario de un producto sumando o restando unidades de forma atómica y segura ante concurrencia.
    /// </summary>
    /// <remarks>
    /// Permite ingresar valores positivos para aumentar el stock o negativos para descontarlo.
    /// Si el descuento resultaría en un stock inferior a cero, la operación se rechaza atómicamente.
    /// </remarks>
    /// <param name="id">Identificador del producto.</param>
    /// <param name="dto">Delta de unidades a ajustar (ej. +10 o -5).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Producto con el balance de stock actualizado.</returns>
    /// <response code="200">Inventario ajustado exitosamente.</response>
    /// <response code="400">El ajuste dejaría el stock en negativo o el delta es cero.</response>
    /// <response code="404">Producto no encontrado.</response>
    [HttpPatch("{id:guid}/stock")]
    [HttpPut("{id:guid}/stock")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> AdjustStock(
        Guid id,
        [FromBody] AdjustStockDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _productService.AdjustStockAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Desactiva (eliminación lógica) un producto del catálogo activo.
    /// </summary>
    /// <param name="id">Identificador del producto a desactivar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Producto desactivado exitosamente.</response>
    /// <response code="404">Producto no encontrado.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
