using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using TallerBackend.Models;
using TiendaApi.Services;

namespace TallerBackend.Controllers
{
    [ApiController]
[Route("api/[controller]")]
public class ContactoController : ControllerBase
{
    private readonly EmailService _emailService;

    public ContactoController(EmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    public IActionResult EnviarContacto(ContactoDto contacto)
    {
        try
        {
            _emailService.Send(
                "victorcoco2005@gmail.com",
                $"Nuevo mensaje de {contacto.Nombre}",
                $"Nombre: {contacto.Nombre}\nEmail: {contacto.Email}\n\n{contacto.Mensaje}",
                contacto.Email
            );

            return Ok(new { message = "Mensaje enviado correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
}
