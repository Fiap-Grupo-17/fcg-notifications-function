using FCG.Notifications.Functions.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FCG.Notifications.Functions.Tests.Functions;

public class OnPaymentProcessedTests
{
    private static OnPaymentProcessed Criar(EmailNotifierSpy spy, ProcessedEventStoreFake store) =>
        new(spy, store);

    [Fact]
    public async Task Approved_EnviaEmailDeConfirmacao()
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();

        await Criar(spy, store).Run(Envelopes.PaymentProcessed(Guid.NewGuid(), status: "Approved"), NullLogger.Instance, CancellationToken.None);

        spy.CompraAprovada.Should().Be(1);
        spy.CompraRecusada.Should().Be(0);
    }

    [Fact]
    public async Task Rejected_EnviaEmailDeRecusa()
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();

        await Criar(spy, store).Run(Envelopes.PaymentProcessed(Guid.NewGuid(), status: "Rejected"), NullLogger.Instance, CancellationToken.None);

        spy.CompraRecusada.Should().Be(1);
        spy.CompraAprovada.Should().Be(0);
    }

    [Theory]
    [InlineData("approved")]
    [InlineData("APPROVED")]
    public async Task Status_CaseInsensitive_TrataComoAprovado(string status)
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();

        await Criar(spy, store).Run(Envelopes.PaymentProcessed(Guid.NewGuid(), status: status), NullLogger.Instance, CancellationToken.None);

        spy.CompraAprovada.Should().Be(1);
        spy.CompraRecusada.Should().Be(0);
    }

    [Fact]
    public async Task Duplicado_NaoEnvia()
    {
        var spy = new EmailNotifierSpy();
        var store = new ProcessedEventStoreFake();
        var messageId = Guid.NewGuid();
        store.Semear(messageId, "PaymentProcessedEvent");

        await Criar(spy, store).Run(Envelopes.PaymentProcessed(messageId, status: "Approved"), NullLogger.Instance, CancellationToken.None);

        spy.TotalEnvios.Should().Be(0);
        store.TryRegisterCalls.Should().Be(0);
    }
}
