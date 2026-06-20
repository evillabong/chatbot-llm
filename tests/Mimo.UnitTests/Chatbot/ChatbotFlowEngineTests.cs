using Mimo.Core.DTOs.Chatbot;
using Mimo.Infrastructure.Chatbot;

namespace Mimo.UnitTests.Chatbot;

/// <summary>
/// Pruebas del motor determinista del chatbot por opciones (#27): inicio en el nodo de entrada,
/// navegación por opciones, encadenado de nodos de mensaje, re-prompt ante opción inválida y escalado.
/// </summary>
public class ChatbotFlowEngineTests
{
    private static FlowDefinition SampleFlow() => new(
        EntryNodeId: "start",
        Nodes:
        [
            new FlowNode("start", FlowNodeType.Menu, "¿En qué te ayudamos?", Options:
            [
                new FlowOption("1", "Pedido", "pedido"),
                new FlowOption("2", "Agente", "agente")
            ]),
            new FlowNode("pedido", FlowNodeType.Message, "Tu pedido va en camino.", Next: "start"),
            new FlowNode("agente", FlowNodeType.Escalate, "Te conecto con un funcionario.")
        ]);

    private readonly ChatbotFlowEngine _engine = new();

    [Fact]
    public void Start_RendersEntryMenu_WithOptions()
    {
        var r = _engine.Process(SampleFlow(), currentNodeId: null, input: "hola");

        Assert.Equal("start", r.NextNodeId);
        Assert.False(r.Escalate);
        Assert.Contains("¿En qué te ayudamos?", r.Text);
        Assert.Contains("1) Pedido", r.Text);
        Assert.Contains("2) Agente", r.Text);
    }

    [Fact]
    public void ValidOption_FollowsMessageNode_AndChainsBackToMenu()
    {
        var r = _engine.Process(SampleFlow(), currentNodeId: "start", input: "1");

        Assert.Equal("start", r.NextNodeId);          // pedido -> Next=start (vuelve al menú)
        Assert.False(r.Escalate);
        Assert.Contains("Tu pedido va en camino.", r.Text);
        Assert.Contains("1) Pedido", r.Text);
    }

    [Fact]
    public void InvalidOption_RepromptsSameMenu()
    {
        var r = _engine.Process(SampleFlow(), currentNodeId: "start", input: "99");

        Assert.Equal("start", r.NextNodeId);
        Assert.False(r.Escalate);
        Assert.Contains("No entendí", r.Text);
        Assert.Contains("1) Pedido", r.Text);
    }

    [Fact]
    public void EscalateOption_SignalsEscalation_AndEndsFlow()
    {
        var r = _engine.Process(SampleFlow(), currentNodeId: "start", input: "2");

        Assert.True(r.Escalate);
        Assert.Null(r.NextNodeId);
        Assert.Contains("funcionario", r.Text);
    }

    [Fact]
    public void StaleNode_RestartsAtEntry()
    {
        var r = _engine.Process(SampleFlow(), currentNodeId: "nodo-inexistente", input: "1");

        Assert.Equal("start", r.NextNodeId);
        Assert.Contains("¿En qué te ayudamos?", r.Text);
    }
}
