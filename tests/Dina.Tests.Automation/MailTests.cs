using Microsoft.Extensions.Configuration;

namespace Dina.Tests.Automation
{
    public class MailTests
    {
        static MailTests()
        {
            Runtime.Initialize("Dina.Automation", "Tests", true);
            // Build configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: true)
                .Build();

            user = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
            password = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
            displayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
            me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:DisplayName");
        }

        [Fact]
        public async Task CanSendAndReceiveEmail()
        {
            var mailSession = new MailSession(user, password, displayName, "smtp.gmail.com", "imap.gmail.com");
            await mailSession.SendMailAsync(user, "Test Email", "test");
        }

        [Fact]
        public async Task CanSearchEmail()
        {
            var mailSession = new MailSession(user, password, displayName, "smtp.gmail.com", "imap.gmail.com");
            var r = await mailSession.SearchInboxByFromAsync("Allister Beharry");
            await mailSession.SendMailAsync(me, "Test Email", "test");
        }


        static string user;
        static string me;
        static string password;
        static string displayName;
    }
}