namespace Dina.Tests.Speech;

public class AudioTests
{
    [Fact]
    public void Test1()
    {
        var cts = new CancellationTokenSource(30000);
        Result<List<byte[]>> c = Result.Failure<List<byte[]>>("ll"); 
        Audio.Capture(cts.Token, (s) =>
        {
            c = s;
        });
        while(!c.IsSuccess)
        {
            Thread.Sleep(100);
        }
        Assert.True(c.IsSuccess);
    }
}