using Microsoft.Extensions.Configuration;

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
            Documents.BinPath = "C:\\Projects\\Dina\\bin";
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("testappsettings.json", optional: false, reloadOnChange: true)
                .Build();
            user = config["Email:User"] ?? throw new ArgumentNullException("Email:User");
            password = config["Email:Password"] ?? throw new ArgumentNullException("Email:Password"); ;
            displayName = config["Email:DisplayName"] ?? throw new ArgumentNullException("Email:DisplayName");
            me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:ManagerEmail");
            ms = new MailPlugin(user, password, displayName, "smtp.gmail.com", "imap.gmail.com");
            fp = new FilesPlugin("..\\..\\..\\..\\data\\files");
        }

        [Fact]
        public async Task CanStartOllamaGemini3nChat()
        {
            var mc = new ModelConversation(model: OllamaModels.Gemma3n_e4b_tools_test);
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
            var pdftext = Documents.ConvertPdfToText("..\\..\\..\\..\\data\\test4.pdf");
            Assert.True(pdftext.IsSuccess);
            var resp = mc.Prompt("What is the amount due in the invoice? Document:{0}", pdftext.Value[0]);
            //var resp = mc.Prompt("What is the first question on the text: \n Text:{0}", pdftext.Value[0]);
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
        public async Task CanAskGemini3nAboutPdfImage()
        {
            var mc = new ModelConversation();
            var pdfbin = File.ReadAllBytes("..\\..\\..\\..\\data\\test.pdf");
            var pdftext = Documents.ConvertPdfToImages("..\\..\\..\\..\\data\\test4.pdf", type:"jpg");
            Assert.True(pdftext.IsSuccess);
            var resp = mc.Prompt("Who is the attached invoice addressed to?", Documents.ResizeImage(pdftext.Value[0], 256, 256), "image/jpg");
            //var resp = mc.Prompt("What is the first question on the text: \n Text:{0}", pdftext.Value[0]);
           
            Assert.NotNull(resp);
            string s = "";
            await foreach (var response in resp)
            {
                Assert.NotNull(response);
                s += response;
            }
            Assert.NotNull(s);
            Console.WriteLine(s);
        }

        [Fact]
        public async Task CanAskGemini3nAboutImage()
        {
            var mc = new ModelConversation();
            var image = File.ReadAllBytes("..\\..\\..\\..\\data\\train16.jpg");
            var text = Documents.OcrImage(image);
            Assert.True(text.IsSuccess);
            var resp = mc.Prompt("Who is the following invoice document addressed to and what is the client's IBAN number? Document: {0}", text.Value);
            Assert.NotNull(resp);
            string s = "";
            await foreach (var response in resp)
            {
                Assert.NotNull(response);
                s += response;
            }
            Assert.NotNull(s);
            Console.WriteLine(s);
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
                Console.Write(response.Message.ToString());
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
            me = config["Email:ManagerEmail"] ?? throw new ArgumentNullException("Email:ManagerEmail"); 
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
        public async Task AgentCanSearchEmail()
        {
            var ac = new AgentConversation("You are an assistant that helps people.", "Email Agent");
            ac.AddPlugin(ms, "Email");
            var p = ac.Prompt($"Search for emails from {me}. If you find any then send an email to the sender with the subject reply and the body my reply to you.");
            await foreach (var response in p)
            {
                Assert.NotNull(response);
                Console.WriteLine(response.Message.ToString());
            }
        }
        [Fact]
        public async Task AgentCanAskQuestions()
        {
          
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

        [Fact]
        public async Task AgentCanSearchFiles()
        {
            var ac = new AgentConversation("You are an assistant that helps people.", "Files Agent");
            ac.AddPlugin(fp, "Files");
            var p = ac.Prompt("Search for files with the name test. If you find any then search each one for the text 'bazinga' and print the file name if you find one, otherwise print bar.");
            await foreach (var response in p)
            {
                Assert.NotNull(response);
                Console.WriteLine(response.Message.ToString());
            }   
        }
        
        static IConfigurationRoot config;
        static string user, password, displayName, me;
        static MailPlugin ms;
        static FilesPlugin fp;
    }
}