namespace Dina.Tests.Speech;

public class AudioTests
{
    [Fact]
    public void Test1()
    {
        var cts = new CancellationTokenSource(8000);
        Result<List<byte[]>> c = Result.Failure<List<byte[]>>("ll"); 
        Audio.Capture(cts.Token, (s) =>
        {
            c = s;
            /*
            var b = new byte[s.Value.Sum(x => x.Length)]; 
            for (int i = 0, j = 0; i < s.Value.Count; i++)
            {
                Array.Copy(s.Value[i], 0, b, j, s.Value[i].Length);
                j += s.Value[i].Length;
            }*/
            var b = s.Value.ConcatArrays();
            Audio.WriteWav(b, "test.wav");
        });
        while(!c.IsSuccess)
        {
            Thread.Sleep(100);
        }
        Assert.True(c.IsSuccess);
    }
}