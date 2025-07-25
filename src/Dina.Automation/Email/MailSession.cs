namespace Dina;

using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System.Threading.Tasks;

public class MailSession : Runtime
{
    public string _smtpHost = "smtp.gmail.com";
    public int _smtpPort = 587;
    public string _imapHost = "imap.gmail.com";
    public int _imapPort = 993;
    private readonly string _user;
    private readonly string _password;
    private readonly string _displayName;

    public MailSession(string user, string password, string displayName, string smtpHost, string imapHost)
    {
        this._user = user;
        this._password = password;
        this._displayName = displayName;
        this._smtpHost = smtpHost;
        this._imapHost = imapHost;
    }

    public async Task<string> SendMailAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_displayName, _user));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_user, _password);
        var ret = await client.SendAsync(message);
        await client.DisconnectAsync(true);
        return ret;
    }

    public async Task<IList<MimeMessage>> ReceiveInboxAsync(int maxCount = 10)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_imapHost, _imapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(_user, _password);
        var inbox = client.Inbox;
        await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

        var count = Math.Min(inbox.Count, maxCount);
        var messages = new List<MimeMessage>();
        for (int i = inbox.Count - count; i < inbox.Count; i++)
        {
            var message = await inbox.GetMessageAsync(i);
            messages.Add(message);
        }

        await client.DisconnectAsync(true);
        return messages;
    }

    // Search inbox by subject
    public async Task<IList<MimeMessage>> SearchInboxBySubjectAsync(string subject)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_imapHost, _imapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(_user, _password);
        var inbox = client.Inbox;
        await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

        var query = SearchQuery.SubjectContains(subject);
        var uids = await inbox.SearchAsync(query);

        var messages = new List<MimeMessage>();
        foreach (var uid in uids)
        {
            var message = await inbox.GetMessageAsync(uid);
            messages.Add(message);
        }

        await client.DisconnectAsync(true);
        return messages;
    }

    // Search inbox by sender email
    public async Task<IList<MimeMessage>> SearchInboxByFromAsync(string fromEmail)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_imapHost, _imapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(_user, _password);
        var inbox = client.Inbox;
        await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

        var query = SearchQuery.FromContains(fromEmail);
        var uids = await inbox.SearchAsync(query);

        var messages = new List<MimeMessage>();
        foreach (var uid in uids)
        {
            var message = await inbox.GetMessageAsync(uid);
            messages.Add(message);
        }

        await client.DisconnectAsync(true);
        return messages;
    }

    // Search inbox by arbitrary MailKit SearchQuery
    public async Task<IList<MimeMessage>> SearchInboxAsync(SearchQuery query)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_imapHost, _imapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(_user, _password);
        var inbox = client.Inbox;
        await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

        var uids = await inbox.SearchAsync(query);

        var messages = new List<MimeMessage>();
        foreach (var uid in uids)
        {
            var message = await inbox.GetMessageAsync(uid);
            messages.Add(message);
        }

        await client.DisconnectAsync(true);
        return messages;
    }
}

