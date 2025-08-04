namespace Dina;

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;


public class MailPlugin : IPlugin
{
    private readonly MailSession _mailSession;


    public MailPlugin(MailSession mailSession)
    {
        _mailSession = mailSession;
    }

    public MailPlugin(string user, string password, string displayName, string smtpHost = "smtp.gmail.com", string imapHost = "imap.gmail.com")
    {
        _mailSession = new MailSession(user, password, displayName, smtpHost, imapHost);
    }

    [KernelFunction, Description("Send an email to a recipient")]
    public async Task SendEmailAsync(
        [Description("Recipient email address")] string to,
        [Description("Email subject")] string subject,
        [Description("Email body")] string body,
        ILogger? logger = null)
    {
        //logger?.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);   
        await _mailSession.SendMailAsync(to, subject, body);

    }

    [KernelFunction, Description("Get the most recent emails from the inbox")]
    public async Task<List<EmailMessage>> GetRecentEmailsAsync(
        [Description("Maximum number of emails to retrieve")] int maxCount = 10)
    {
        var messages = await _mailSession.ReceiveInboxAsync(maxCount);
        var result = new List<EmailMessage>();
        foreach (var mime in messages)
        {
            result.Add(EmailMessage.FromMimeMessage(mime));
        }
        return result;
    }

    [KernelFunction, Description("Search inbox emails by subject")]
    public async Task<List<EmailMessage>> SearchInboxBySubjectAsync(
        [Description("Subject to search for")] string subject)
    {
        var messages = await _mailSession.SearchInboxBySubjectAsync(subject);
        var result = new List<EmailMessage>();
        foreach (var mime in messages)
        {
            result.Add(EmailMessage.FromMimeMessage(mime));
        }
        return result;
    }

    [KernelFunction, Description("Search inbox emails by sender email address")]
    public async Task<List<EmailMessage>> SearchInboxByFromAsync(
        [Description("Sender email address to search for")] string fromEmail)
    {
        var messages = await _mailSession.SearchInboxByFromAsync(fromEmail);
        var result = new List<EmailMessage>();
        foreach (var mime in messages)
        {
            result.Add(EmailMessage.FromMimeMessage(mime));
        }
        return result;
    }

    public Dictionary<string, Dictionary<string, object>> SharedState { get; set; } = new Dictionary<string, Dictionary<string, object>>();

}