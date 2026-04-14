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
    private const decimal DESCUENTO_PROFESIONAL = 0.20m;      // 20%
    private const decimal MINIMO_PARA_DESCUENTO = 500m;        // €500

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

    var subtotal = dto.Productos.Sum(p => p.Precio * p.Cantidad);
    var esProfesional = usuario.TipoUsuario?.ToLower() == "profesional";
    var aplicaDescuento = esProfesional && subtotal >= MINIMO_PARA_DESCUENTO;
    var porcentajeDescuento = aplicaDescuento ? DESCUENTO_PROFESIONAL : 0m;

    var lineItems = dto.Productos.Select(p => new SessionLineItemOptions
    {
        PriceData = new SessionLineItemPriceDataOptions
        {
            UnitAmount = (long)(p.Precio * (1 - porcentajeDescuento) * 100),
            Currency = "eur",
            ProductData = new SessionLineItemPriceDataProductDataOptions
            {
                Name = aplicaDescuento
                    ? $"{p.Nombre} (Precio Pro -{(int)(porcentajeDescuento*100)}%)"
                    : p.Nombre,
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
            { "usuarioId", dto.UsuarioId.ToString() },
            { "descuento", porcentajeDescuento.ToString() }
        }
    };

    var servicio = new SessionService();
    var sesion = await servicio.CreateAsync(opciones);

    return Ok(new {
        url = sesion.Url,
        descuentoAplicado = aplicaDescuento,
        porcentaje = (int)(porcentajeDescuento * 100)
    });
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

    // Evitar duplicados
    var yaExiste = await _context.Pedidos.AnyAsync(p => p.StripeSessionId == session_id);
    if (yaExiste) return Ok(new { mensaje = "Pedido ya procesado" });

    // 1. Obtener Usuario con sus detalles de envío
    var usuario = await _context.Usuarios
        .Include(u => u.DetalleUsuario)
        .FirstOrDefaultAsync(u => u.Id == userId);

    if (usuario == null) return BadRequest("Usuario no encontrado");

    // 2. Obtener Carrito con Variantes y Productos
    var carrito = await _context.CarritoItems
        .Include(c => c.Producto)
        .Include(c => c.Variante)
        .Where(c => c.UsuarioId == userId)
        .ToListAsync();

    if (!carrito.Any()) return BadRequest("Carrito vacío");

    // 3. Procesar Stock y Pedido
    foreach (var item in carrito)
    {
        if (item.VarianteId.HasValue && item.Variante != null) {
            item.Variante.Stock -= item.Cantidad;
        } else {
            item.Producto.Stock -= item.Cantidad;
        }
    }

    var total = carrito.Sum(c => (c.Variante != null ? c.Variante.Precio : c.Producto.Precio) * c.Cantidad);

    var pedido = new Pedido {
        UsuarioId = userId,
        Fecha = DateTime.Now,
        Estado = "Pagado",
        Total = total,
        StripeSessionId = session_id,
        Detalles = carrito.Select(c => new DetallePedido {
            ProductoId = c.ProductoId,
            VarianteId = c.VarianteId,
            Cantidad = c.Cantidad,
            PrecioUnitario = c.Variante != null ? c.Variante.Precio : c.Producto.Precio
        }).ToList()
    };

    _context.Pedidos.Add(pedido);
    _context.CarritoItems.RemoveRange(carrito);
    await _context.SaveChangesAsync();

    // 4. Generar Tabla HTML para el Email
    string filasHtml = "";
    foreach (var c in carrito) {
        var nombreItem = c.Producto.Nombre;
        var subNombre = c.Variante != null ? $"<div style='font-size:12px; color:#64748b;'>Opción: {c.Variante.Nombre}</div>" : "";
        var precio = c.Variante != null ? c.Variante.Precio : c.Producto.Precio;
        
        filasHtml += $@"
            <tr>
                <td style='padding:15px; border-bottom:1px solid #edf2f7;'>
                    <span style='color:#1e293b; font-weight:600;'>{nombreItem}</span>
                    {subNombre}
                </td>
                <td style='padding:15px; border-bottom:1px solid #edf2f7; text-align:center;'>{c.Cantidad}</td>
                <td style='padding:15px; border-bottom:1px solid #edf2f7; text-align:right;'>{precio:0.00}€</td>
                <td style='padding:15px; border-bottom:1px solid #edf2f7; text-align:right; font-weight:bold;'>{(precio * c.Cantidad):0.00}€</td>
            </tr>";
    }

    // 5. Cuerpo del Email Profesional
    string nombreCliente = usuario.DetalleUsuario?.NombreCompleto ?? usuario.NombreUsuario;
    string direccionEnvio = $@"
        {usuario.DetalleUsuario?.Direccion}<br>
        {usuario.DetalleUsuario?.CodigoPostal} {usuario.DetalleUsuario?.Ciudad}<br>
        Tlf: {usuario.DetalleUsuario?.Telefono}";

    string emailHtml = $@"
    <div style='background-color:#f8fafc; padding:40px; font-family:sans-serif;'>
        <div style='max-width:600px; margin:0 auto; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 10px 15px rgba(0,0,0,0.05);'>
            <div style='background:#1e293b; padding:30px; text-align:center; color:#ffffff;'>
                <h1 style='margin:0; font-size:24px;'>¡Gracias por tu compra!</h1>
                <p style='opacity:0.8;'>Confirmación de Pedido #{pedido.Id}</p>
            </div>
            
            <div style='padding:30px;'>
                <p>Hola <strong>{nombreCliente}</strong>,</p>
                <p>Tu pedido ha sido recibido y está siendo preparado. Aquí tienes los detalles:</p>

                <table style='width:100%; border-collapse:collapse; margin-top:20px;'>
                    <thead>
                        <tr style='background:#f1f5f9; color:#475569; font-size:12px; text-transform:uppercase;'>
                            <th style='padding:10px; text-align:left;'>Producto</th>
                            <th style='padding:10px; text-align:center;'>Cant.</th>
                            <th style='padding:10px; text-align:right;'>Precio</th>
                            <th style='padding:10px; text-align:right;'>Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        {filasHtml}
                    </tbody>
                </table>

                <div style='margin-top:20px; text-align:right; padding:15px; background:#f8fafc; border-radius:12px;'>
                    <span style='color:#64748b;'>Total Pagado:</span>
                    <div style='font-size:32px; font-weight:800; color:#1e293b;'>{total:0.00}€</div>
                </div>

                <div style='margin-top:30px; border-top:1px solid #e2e8f0; padding-top:20px;'>
                    <h4 style='margin-bottom:10px; color:#1e293b;'>📍 Dirección de Envío</h4>
                    <p style='color:#64748b; font-size:14px; line-height:1.6;'>{direccionEnvio}</p>
                </div>
            </div>

            <div style='background:#f1f5f9; padding:20px; text-align:center; color:#94a3b8; font-size:12px;'>
                Este correo es automático. Por favor, no respondas directamente.<br>
                © {DateTime.Now.Year} TuTienda. Todos los derechos reservados.
            </div>
        </div>
    </div>";

    // Enviar Emails
    _emailService.Send(usuario.Correo, "Confirmación de Pedido 🛒", emailHtml);
    
    // Al Admin enviamos el mismo pero con aviso
    _emailService.Send("victorcoco2005@gmail.com", $"NUEVO PEDIDO #{pedido.Id} - {nombreCliente}", emailHtml);

    return Ok(new { mensaje = "Pedido creado correctamente" });
}
}