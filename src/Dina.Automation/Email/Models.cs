namespace Dina;

using System;
using System.Collections.Generic;
using System.Linq;
using MimeKit;

public class EmailAddress
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class EmailRecipient : EmailAddress
{
    // You can add recipient-specific properties here if needed
}

public class EmailSender : EmailAddress
{
    // You can add sender-specific properties here if needed
}

public class EmailMessage
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailSender Sender { get; set; } = new EmailSender();
    public List<EmailRecipient> Recipients { get; set; } = new List<EmailRecipient>();
    public DateTime Date { get; set; }

    public static EmailMessage FromMimeMessage(MimeMessage mime)
    {
        var emailMessage = new EmailMessage
        {
            Subject = mime.Subject ?? string.Empty,
            Body = mime.TextBody ?? mime.HtmlBody ?? string.Empty,
            Date = mime.Date.DateTime
        };

        if (mime.From?.Count > 0)
        {
            var sender = mime.From.Mailboxes.FirstOrDefault();
            if (sender != null)
            {
                emailMessage.Sender = new EmailSender
                {
                    Name = sender.Name ?? string.Empty,
                    Address = sender.Address ?? string.Empty
                };
            }
        }

        if (mime.To != null)
        {
            foreach (var recipient in mime.To.Mailboxes)
            {
                emailMessage.Recipients.Add(new EmailRecipient
                {
                    Name = recipient.Name ?? string.Empty,
                    Address = recipient.Address ?? string.Empty
                });
            }
        }

        return emailMessage;
    }
}
