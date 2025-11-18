using System.Diagnostics;
using Xunit;

namespace SafeParallelism.Threading;

public class ThreadPoolTests
{
    [Theory]
    [InlineData(4, 1000, 100, 2)]  // Cenário com poucos threads mínimos
    [InlineData(1000, 1000, 100, 1)]  // Cenário com muitos threads mínimos
    public void ThreadPoolPerformance(int minThreads, int numberOfTasks, int sleep, int expectedExecutionSecs)
    {
        // Configura o número mínimo de threads
        ThreadPool.SetMinThreads(minThreads, minThreads);

        // Inicia o cronômetro para medir o tempo de execução
        Stopwatch stopwatch = new();
        stopwatch.Start();

        // Cria um contador para sincronizar a conclusão das tarefas
        CountdownEvent countdown = new(numberOfTasks);

        // Enfileira as tarefas simulando trabalho no pool de threads
        for (int i = 0; i < numberOfTasks; i++)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                // Simula trabalho (ex: espera de I/O ou processamento)
                Thread.Sleep(sleep); // Simula uma tarefa leve
                countdown.Signal();
            });
        }

        // Espera todas as tarefas terminarem
        countdown.Wait();

        stopwatch.Stop();

        // Verifica se o tempo não é excessivamente alto (ajustar o valor conforme o ambiente)
        Assert.Equal(expectedExecutionSecs, stopwatch.Elapsed.Seconds);
    }

    [Fact]
    public async Task CurrentManagedThreadId()
    {
        var principalThreadId = Environment.CurrentManagedThreadId;
        for (int i = 0; i < 5000; i++)
        {
            Task.Run(async () =>
            {
                Assert.NotEqual(principalThreadId, Environment.CurrentManagedThreadId);
            });
        }
    }
}