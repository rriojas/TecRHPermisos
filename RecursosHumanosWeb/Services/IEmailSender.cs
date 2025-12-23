using System.Threading.Tasks;
using RecursosHumanosWeb.Models.ViewModels.ResetPassword;

namespace RecursosHumanosWeb.Models
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string to, string subject, string body);
        Task<bool> SendTokenEmailAsync(RequestTokenEmailModel model);
    }
}
