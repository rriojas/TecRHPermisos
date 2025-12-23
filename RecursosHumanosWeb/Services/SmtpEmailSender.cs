using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RecursosHumanosWeb.Models.ViewModels.ResetPassword;
using System.Web;

namespace RecursosHumanosWeb.Models
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;

        public SmtpEmailSender(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.EmailSender, _settings.PasswordApp),
                    EnableSsl = _settings.EnableSsl
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_settings.EmailSender),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(to);
                client.Send(message);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> SendTokenEmailAsync(RequestTokenEmailModel model)
        {
            var body = FormatTokenEmail(model);
            return SendEmailAsync(model.Email, "Cambio de contraseña - Enlace de verificación", body);
        }

        // Helper that formats the email body using the RequestTokenEmailModel
        private string FormatTokenEmail(RequestTokenEmailModel model)
        {
            var html = $@"<div style='font-family: Inter, system-ui, -apple-system, Roboto, Segoe UI, Helvetica Neue, Arial; color: #111827;'>
                    <h2>Cambio de contraseña</h2>
                    <p>Hola,</p>
                    <p>Se solicitó un restablecimiento de contraseña para la cuenta asociada a <strong>{HttpUtility.HtmlEncode(model.Email)}</strong>.</p>
                    <p>Haz clic en el siguiente enlace para restablecer tu contraseña. El enlace expirará en {model.ExpirationMinutes} minutos.</p>
                    <p><a href='{HttpUtility.HtmlEncode(model.Link)}' target='_blank'>Restablecer contraseña</a></p>
                    <p>Si no solicitaste este cambio, ignora este correo.</p>
                    <hr />
                    <p style='font-size:0.85rem; color:#6b7280;'>Si el enlace no funciona, copia y pega la siguiente URL en tu navegador:<br />{HttpUtility.HtmlEncode(model.Link)}</p>
                </div>";
            return html;
        }
    }
}
