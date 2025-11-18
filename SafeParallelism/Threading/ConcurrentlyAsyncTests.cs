//ref.: https://learn.microsoft.com/pt-br/dotnet/csharp/programming-guide/concepts/async/
using System.Diagnostics;
using Xunit;

namespace SafeParallelism.Threading;

public class ConcurrentlyAsyncTests
{
    static readonly Stopwatch _stopWatch = new();

    [Fact]
    public async Task PrepareBreakfastConcurrentlyAsync_ShouldReturnIn1Secs()
    {
        _stopWatch.Start();
        var breakfast = await PrepareBreakfastConcurrentlyAsync();
        _stopWatch.Stop();

        AssertPrepareBreakfast(breakfast);
        Assert.Equal(1, Math.Round(_stopWatch.Elapsed.TotalSeconds));
    }

    [Fact]
    public async Task PrepareBreakfastSequentiallyAsync_ShouldReturnIn2Secs()
    {
        _stopWatch.Start();
        var breakfast = await PrepareBreakfastSequentiallyAsync();
        _stopWatch.Stop();

        AssertPrepareBreakfast(breakfast);
        Assert.NotEqual(1, Math.Round(_stopWatch.Elapsed.TotalSeconds));
    }

    static void AssertPrepareBreakfast(Breakfast breakfast)
    {
        Assert.NotNull(breakfast);
        Assert.NotNull(breakfast.Egg);
        Assert.NotNull(breakfast.Coffee);
    }

    static async Task<Breakfast> PrepareBreakfastConcurrentlyAsync()
    {
        Task<Egg> eggTask = FryEggsAsync();
        Task<Coffee> coffeeTask = MakeCoffeAsync();

        var egg = await eggTask;
        var coffee = await coffeeTask;
        return new Breakfast(egg, coffee);
    }
    static async Task<Breakfast> PrepareBreakfastSequentiallyAsync()
    {
        Egg egg = await FryEggsAsync();
        Coffee coffee = await MakeCoffeAsync();
        return new Breakfast(egg, coffee);
    }
    private static async Task<Egg> FryEggsAsync()
    {
        await Task.Delay(1000);
        return new Egg();
    }
    private static async Task<Coffee> MakeCoffeAsync()
    {
        await Task.Delay(1000);

        return new Coffee();
    }
}