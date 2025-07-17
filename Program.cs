using Npgsql;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;

static class Constants
{
    public const string ConnectionString = "Host=localhost;Username=rebus_test;Password=Password01;Database=rebus_test";
    public static volatile bool DomainEventHandled;
}

class DomainEvent;

class DomainEventHandler : IHandleMessages<DomainEvent>
{
    public Task Handle(DomainEvent message)
    {
        Constants.DomainEventHandled = true;
        Console.WriteLine("*** Received DomainEvent.");
        return Task.CompletedTask;
    }
}

class TestMessage;

class TestMessageHandler(IBus bus) : IHandleMessages<TestMessage>
{
    public async Task Handle(TestMessage message)
    {
        await using var conn = new NpgsqlConnection(Constants.ConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await bus.Send(new DomainEvent());

        throw new Exception("Test exception to fail transaction");
    }
}

class Program
{
    const string QueueName = "test_queue";

    static async Task Main()
    {
        using var activator = new BuiltinHandlerActivator();

        activator.Register((bus, _) => new TestMessageHandler(bus));
        activator.Register(() => new DomainEventHandler());

        var inMemNetwork = new InMemNetwork();
        
        var bus = Configure.With(activator)
            .Transport(t => t.UsePostgreSql(Constants.ConnectionString, QueueName, QueueName))
            //.Transport(x => x.UseInMemoryTransport(inMemNetwork, QueueName, registerSubscriptionStorage: false))
            .Routing(r =>
            {
                r.TypeBased()
                    .Map<DomainEvent>(QueueName)
                    .Map<TestMessage>(QueueName);
            })
            .Start();
        

        await bus.Send(new TestMessage());

        await Task.Delay(2000);
        
        if (Constants.DomainEventHandled)
        {
            Console.WriteLine("*** DomainEvent was handled successfully.");
        }
        else
        {
            Console.WriteLine("*** DomainEvent was not handled.");
        }
    }
}
