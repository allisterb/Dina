using Microsoft.Extensions.Configuration;
using OllamaSharp;
using OpenAI.Assistants;
using Serilog;
using Serilog.Extensions.Logging;

namespace Dina.Tests.Understanding
{
    public class ModelTests
    {
        static ModelTests()
        {
            var logger = new LoggerConfiguration()
                 .Enrich.FromLogContext()
                 .MinimumLevel.Verbose()
                 .WriteTo.Console()
                 .WriteTo.File(Path.Combine(Runtime.AssemblyLocation, "Dina.log"))
                 .CreateLogger();
            var lf = new SerilogLoggerFactory(logger);
            var lp = new SerilogLoggerProvider(logger, false);
            Runtime.Initialize("Dina.Understanding", "Tests", false, lf, lp);
            Documents.MuPdfPath = "C:\\Projects\\Dina\\bin";
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        [Fact]
        public async Task CanStartOllamaGemini3nChat()
        {
            var mc = new ModelConversation();
            var resp = mc.Prompt("What kind of image is this?", File.ReadAllBytes("C:\\Users\\Allister\\Pictures\\applogo.png"), "image/png");
            string s = "";
            Assert.NotNull(resp);
            await foreach (var response in resp)
            {
                Assert.NotNull(response);
                s += response;
            }
            Assert.NotNull(s);
            Console.WriteLine(s);
        }

        [Fact]
        public async Task CanAskGemini3nAboutPdf()
        {
            var mc = new ModelConversation();
            var pdfbin = File.ReadAllBytes("..\\..\\..\\..\\data\\test.pdf");
            var pdftext = Documents.ConvertPdfToText("..\\..\\..\\..\\data\\test.pdf");
            Assert.True(pdftext.IsSuccess);
            var resp = mc.Prompt("What is the first question on the document: Document:{0}", pdftext.Value[0]);
            //var resp = mc.Prompt("What is the first question on the text: \n Text:{0}", pdftext.Value[0]);
            string s = "";
            Assert.NotNull(resp);
            await foreach (var response in resp)
            {
                Assert.NotNull(response);
                s += response;
            }
            Assert.NotNull(s);

        }

        
        [Fact]
        public async Task CanCallKernelFunction()
        {
            var ac = new AgentConversation(
                "Answer questions about different locations.\r\n" +
                "For France, use the time format: HH:MM.\r\n HH goes from 00 to 23 hours, MM goes from 00 to 59 minutes\r\n",
                "Location Agent"
                );
            ac.AddPlugin<TestFunctions>("Time");
            await foreach (var response in ac.Prompt("What is the time in Paris, France?"))
            {
                Assert.NotNull(response);
                Console.WriteLine(response.Message.ToString());
            }   
        }
        
        [Fact]
        public async Task CanCallAgent()
        {
            var ac = new AgentConversation(
               "You are an assistant that summarizes key points and sentiments from customer reviews.", "Email Agent", immutableKernel: true);
            await ac.TestAgent();
        }
        
        [Fact]
        public async Task AgentCanSendEmail()
        {
            var user = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
            var password = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
            var displayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
            var me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:ManagerEmail"); 
            var ms = new MailPlugin(user, password, displayName, "smtp.gmail.com", "imap.gmail.com");
            var ac = new AgentConversation("You are an assistant that helps people.", "Email Agent");
            ac.AddPlugin(ms, "Email");
            var p = ac.Prompt($"Send an email to {me} with the subject of 'Test Message' and body hello to the 5b. \"");
            await foreach (var response in p)
            {
                Assert.NotNull(response);
                Console.WriteLine(response.Message.ToString());
            }
        }

        [Fact]
        public async Task AgentCanAskQuestions()
        {
            var user = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
            var password = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
            var displayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
            var me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:ManagerEmail");
            var ms = new MailPlugin(user, password, displayName, "smtp.gmail.com", "imap.gmail.com");
            var ac = new AgentConversation("You are an assistant that helps people.", plugins: [(ms, "Email")]);
            await foreach(var message in ac.Prompt(
                $"You will complete the following steps:\n * First ask the user for a number.\n* Then ask the user for an email address.\n *Then send an email to that address with the subject of 'Test Message' and body containing the number entered in the first step. "))
            { 
                Assert.NotNull(message);
                Console.WriteLine(message); 
            }
            await foreach (var message in ac.Prompt("My number is 42.", me))
            {
                Assert.NotNull(message);
                Console.WriteLine(message);
            }
            await foreach (var message in ac.Prompt("My email is {0}", me))
            {
                Assert.NotNull(message);
                Console.WriteLine(message);
            }
        }
        static IConfigurationRoot config;
    }
}