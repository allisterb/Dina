namespace Dina.Tests.Understanding
{
    public class ModelTests
    {
        static ModelTests()
        {
            Model.Initialize().Wait();
        }

        [Fact]
        public void CanInitialize() => Assert.True(Model.IsInitialized);

        [Fact]
        public async Task CanStartChat()
        {
            var chat = Model.StartChat("You are Dina, an agent to assist users with document intelligence tasks.");
            Assert.NotNull(chat);
            var responses = chat.SendAsync("Hello, how are you?");
            await foreach (var response in responses)
            {
                Assert.NotNull(response);
                Assert.NotEmpty(response);
                Console.WriteLine(response);
            }
        }

    }
}