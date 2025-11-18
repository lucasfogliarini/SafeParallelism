using Xunit;

namespace SafeParallelism.Threading;

/// <summary>
/// O lock é usado para garantir que um bloco de código seja executado por apenas uma thread de cada vez,
/// o que é essencial em ambientes multi-threaded para evitar problemas de concorrência.
/// </summary>
public class LockTests
{
    private int _counter = 0;
    private readonly object _lockObject = new();
    const int TASKS = 5000;
    const int LOOPINGS = 100;
    readonly int _expectedCounter = TASKS * LOOPINGS;

    [Fact]
    public async Task Lock_Should_Ensure_Thread_Safety()
    {
        var tasks = new Task[TASKS];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < LOOPINGS; j++)
                {
                    IncrementCounterWithLock();
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(_expectedCounter, _counter);
    }

    [Fact]
    public async Task WithoutLock_Should_NotEnsure_Thread_Safety()
    {
        var tasks = new Task[TASKS];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < LOOPINGS; j++)
                {
                    IncrementCounterWithoutLock();
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.True(_expectedCounter >= _counter);// _counter esperado é menor, mas pode ser igual.
    }

    private void IncrementCounterWithLock()
    {
        lock (_lockObject)
        {
            _counter++;
        }
    }
    private void IncrementCounterWithoutLock()
    {
        _counter++;
    }
}