using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using TiendaApi.Data;
using TiendaApi.Dtos;
using TiendaApi.Models;
using TiendaApi.Services;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly TiendaDbContext _context;
    private readonly IConfiguration _configuration;

    private readonly EmailService _emailService;

   public StripeController(
    TiendaDbContext context,
    IConfiguration configuration,
    EmailService emailService)
{
    _context = context;
    _configuration = configuration;
    _emailService = emailService;
    StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
}
    [HttpPost("crear-sesion")]
    public async Task<IActionResult> CrearSesion([FromBody] CrearSesionDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
        if (usuario == null) return Unauthorized("Usuario no encontrado");

        var lineItems = dto.Productos.Select(p => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = (long)(p.Precio * 100),
                Currency = "eur",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = p.Nombre,
                }
            },
            Quantity = p.Cantidad
        }).ToList();

        var opciones = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = _configuration["Stripe:SuccessUrl"],
            CancelUrl = _configuration["Stripe:CancelUrl"],
            Metadata = new Dictionary<string, string>
            {
                { "usuarioId", dto.UsuarioId.ToString() } // ← guardar usuarioId en metadata
            }
        };

        var servicio = new SessionService();
        var sesion = await servicio.CreateAsync(opciones);

        return Ok(new { url = sesion.Url });
    }

 [HttpGet("exito")]
[AllowAnonymous]
public async Task<IActionResult> Exito(string session_id)
{
    var servicio = new SessionService();
    var sesion = await servicio.GetAsync(session_id);

    if (sesion.PaymentStatus != "paid")
        return BadRequest("Pago no realizado");

    if (!sesion.Metadata.TryGetValue("usuarioId", out var usuarioIdStr))
        return BadRequest("No se encontró el usuario en la sesión");

    int userId = int.Parse(usuarioIdStr);

    var yaExiste = await _context.Pedidos
        .AnyAsync(p => p.StripeSessionId == session_id);
    if (yaExiste)
        return Ok(new { mensaje = "Pedido ya procesado" });

    var carrito = await _context.CarritoItems
        .Include(c => c.Producto)
        .Include(c => c.Variante) // ← incluir variante
        .Where(c => c.UsuarioId == userId)
        .ToListAsync();

    if (!carrito.Any()) return BadRequest("Carrito vacío");

    foreach (var item in carrito)
    {
        if (item.VarianteId.HasValue && item.Variante != null)
        {
            if (item.Variante.Stock < item.Cantidad)
                return BadRequest($"Stock insuficiente para {item.Producto.Nombre}");
            item.Variante.Stock -= item.Cantidad;
        }
        else
        {
            if (item.Producto.Stock < item.Cantidad)
                return BadRequest($"Stock insuficiente para {item.Producto.Nombre}");
            item.Producto.Stock -= item.Cantidad;
        }
    }

    // ← precio real según variante
    var total = carrito.Sum(c =>
        (c.Variante != null ? c.Variante.Precio : c.Producto.Precio) * c.Cantidad);

    var pedido = new Pedido
    {
        UsuarioId = userId,
        Fecha = DateTime.Now,
        Estado = "Pagado",
        Total = total,
        StripeSessionId = session_id,
        Detalles = carrito.Select(c => new DetallePedido
        {
            ProductoId = c.ProductoId,
            VarianteId = c.VarianteId,  // ← añadir
            Cantidad = c.Cantidad,
            PrecioUnitario = c.Variante != null ? c.Variante.Precio : c.Producto.Precio
        }).ToList()
    };

    _context.Pedidos.Add(pedido);
    _context.CarritoItems.RemoveRange(carrito);
    await _context.SaveChangesAsync();

    var usuario = await _context.Usuarios.FindAsync(userId);
    if (usuario == null) return BadRequest("Usuario no encontrado");

    var cuerpoCliente = $@"
<h2>🧾 Gracias por tu compra</h2>
<p>Tu pedido ha sido procesado correctamente.</p>
<p><strong>Total:</strong> {total} €</p>
<p><strong>Estado:</strong> Pagado</p>
<p>En breve recibirás tu pedido 🚀</p>
";

    _emailService.Send(usuario.Correo, "Confirmación de tu pedido", cuerpoCliente);

    var cuerpoAdmin = $@"
<h2>🚨 Nuevo pedido recibido</h2>
<p><strong>Usuario ID:</strong> {userId}</p>
<p><strong>Total:</strong> {total} €</p>
<p><strong>Fecha:</strong> {DateTime.Now}</p>
<p>Revisa el panel de administración.</p>
";

    _emailService.Send("victorcoco2005@gmail.com", "Nuevo pedido realizado", cuerpoAdmin);

    return Ok(new { mensaje = "Pedido creado correctamente" });
}
}