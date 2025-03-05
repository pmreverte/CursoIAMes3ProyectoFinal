using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace Sprint2.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For development purposes, we're just logging the email instead of actually sending it
            // In a production environment, you would implement actual email sending logic here
            Console.WriteLine($"Email to: {email}, Subject: {subject}");
            Console.WriteLine($"Message: {htmlMessage}");
            
            // Return a completed task since we're not actually sending an email
            return Task.CompletedTask;
        }
    }
}
