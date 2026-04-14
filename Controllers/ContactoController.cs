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
    [HttpPost]
public IActionResult EnviarContacto(ContactoDto contacto)
{
    try
    {
        // Estructura HTML con estilos in-line (compatibilidad máxima)
        string cuerpoHtml = $@"
        <html>
        <body style='margin: 0; padding: 0; background-color: #f4f7fa; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
            <table border='0' cellpadding='0' cellspacing='0' width='100%'>
                <tr>
                    <td style='padding: 40px 0;'>
                        <table align='center' border='0' cellpadding='0' cellspacing='0' width='600' style='background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
                            <tr>
                                <td style='background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); padding: 30px; text-align: center;'>
                                    <h1 style='color: #ffffff; margin: 0; font-size: 22px; letter-spacing: 1px;'>Nuevo Mensaje de Cliente</h1>
                                </td>
                            </tr>
                            
                            <tr>
                                <td style='padding: 40px;'>
                                    <p style='margin: 0 0 20px; color: #64748b; font-size: 16px;'>Has recibido una nueva consulta desde el formulario de contacto:</p>
                                    
                                    <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 20px; margin-bottom: 30px;'>
                                        <table width='100%'>
                                            <tr>
                                                <td style='padding: 5px 0; font-weight: bold; color: #1e293b; width: 80px;'>Nombre:</td>
                                                <td style='padding: 5px 0; color: #475569;'>{contacto.Nombre}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 5px 0; font-weight: bold; color: #1e293b;'>Email:</td>
                                                <td style='padding: 5px 0;'>
                                                    <a href='mailto:{contacto.Email}' style='color: #4f46e5; text-decoration: none;'>{contacto.Email}</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </div>

                                    <div style='margin-top: 10px;'>
                                        <p style='margin: 0 0 10px; font-weight: bold; color: #1e293b;'>Contenido del mensaje:</p>
                                        <div style='color: #334155; line-height: 1.6; font-size: 15px; border-left: 4px solid #4f46e5; padding-left: 15px; font-style: italic;'>
                                            ""{contacto.Mensaje.Replace("\n", "<br>")}""
                                        </div>
                                    </div>
                                </td>
                            </tr>

                            <tr>
                                <td style='background-color: #f1f5f9; padding: 20px; text-align: center; border-top: 1px solid #e2e8f0;'>
                                    <p style='margin: 0; color: #94a3b8; font-size: 12px;'>
                                        Este correo se envió desde el sistema automático de <b>TuTienda</b>.<br>
                                        Puedes responder directamente a este email para contactar con el cliente.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

        // Usamos tu servicio con los 4 parámetros
        _emailService.Send(
            "soportex20k@gmail.com",                  // Destinatario (tú)
            $"📩 Contacto: {contacto.Nombre}",            // Asunto
            cuerpoHtml,                                   // El HTML profesional
            contacto.Email                                // Para que al dar a "Responder" le llegue a él
        );

        return Ok(new { message = "Mensaje enviado correctamente" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "No se pudo enviar el correo", details = ex.Message });
    }
}
}
}
