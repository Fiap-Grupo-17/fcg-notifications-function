using FCG.Notifications.Functions.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FCG.Notifications.Functions.Tests.Functions;

public class OnUserCreatedTests
{
    private const string Consumer = "UserCreatedEvent";

    private static OnUserCreated Criar(EmailNotifierSpy spy, ProcessedEventStoreFake store) =>
        new(spy, store);

    [Fact]
    public async Task FluxoFeliz_Checa_Envia_ERegistraDepois()
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();
        var messageId = Guid.NewGuid();

        await Criar(spy, store).Run(Envelopes.UserCreated(messageId), NullLogger.Instance, CancellationToken.None);

        spy.BoasVindas.Should().Be(1);
        store.AlreadyProcessedCalls.Should().Be(1); // checou ANTES
        store.TryRegisterCalls.Should().Be(1);      // registrou DEPOIS
    }

    [Fact]
    public async Task Duplicado_JaProcessado_NaoEnviaNemRegistraDeNovo()
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();
        var messageId = Guid.NewGuid();
        store.Semear(messageId, Consumer); // simula "já processado"

        await Criar(spy, store).Run(Envelopes.UserCreated(messageId), NullLogger.Instance, CancellationToken.None);

        spy.BoasVindas.Should().Be(0);
        store.TryRegisterCalls.Should().Be(0);
    }

    [Fact]
    public async Task EnvelopeInvalido_NaoEnvia_NaoRegistra()
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();

        await Criar(spy, store).Run("payload não-json {", NullLogger.Instance, CancellationToken.None);

        spy.TotalEnvios.Should().Be(0);
        store.TryRegisterCalls.Should().Be(0);
    }

    [Fact]
    public async Task MessageIdVazio_NaoEnvia()
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();

        await Criar(spy, store).Run(Envelopes.UserCreated(Guid.Empty), NullLogger.Instance, CancellationToken.None);

        spy.TotalEnvios.Should().Be(0);
    }
}
