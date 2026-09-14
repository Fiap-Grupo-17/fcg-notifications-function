using FCG.Contracts.Events;
using FCG.Notifications.Functions.Tests.Fakes;
using FluentAssertions;

namespace FCG.Notifications.Functions.Tests.Mensageria;

public class MassTransitEnvelopeSerializerTests
{
    [Fact]
    public void Deserialize_UserCreated_ExtraiMessageIdEPayloadCamelCase()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var json = Envelopes.UserCreated(messageId, userId, nome: "Raul", email: "raul@fcg.com");

        var env = MassTransitEnvelopeSerializer.Deserialize<UserCreatedEvent>(json);

        env.Should().NotBeNull();
        env!.MessageId.Should().Be(messageId);
        env.Message.Should().NotBeNull();
        env.Message!.UserId.Should().Be(userId);
        env.Message.Nome.Should().Be("Raul");
        env.Message.Email.Should().Be("raul@fcg.com");
    }

    [Fact]
    public void Deserialize_Payment_PrecoComoString_ConverteParaDecimal()
    {
        var json = Envelopes.PaymentProcessed(Guid.NewGuid(), status: "Approved", price: 199.90m);

        var env = MassTransitEnvelopeSerializer.Deserialize<PaymentProcessedEvent>(json);

        env.Should().NotBeNull();
        env!.Message.Should().NotBeNull();
        env.Message!.Price.Should().Be(199.90m); // veio como "199.90" (string) no envelope
        env.Message.Status.Should().Be("Approved");
    }

    [Fact]
    public void Deserialize_JsonMalformado_RetornaNull_SemLancar()
    {
        // Payload não-JSON: não deve lançar (evita loop de poison message); handler dá ACK.
        var act = () => MassTransitEnvelopeSerializer.Deserialize<UserCreatedEvent>("isto não é json {");

        act.Should().NotThrow();
        act().Should().BeNull();
    }
}
