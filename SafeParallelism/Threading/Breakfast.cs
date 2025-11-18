//ref.: https://learn.microsoft.com/pt-br/dotnet/csharp/programming-guide/concepts/async/

namespace SafeParallelism.Threading;

internal class Coffee { }
internal class Egg { }
internal class Breakfast(Egg egg, Coffee coffee)
{
    public Egg Egg { get; set; } = egg;
    public Coffee Coffee { get; set; } = coffee;
}