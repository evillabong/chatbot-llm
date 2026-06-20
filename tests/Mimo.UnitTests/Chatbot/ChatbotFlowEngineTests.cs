using Mimo.Core.DTOs.Chatbot;
using Mimo.Core.Interfaces;
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
    private static readonly Dictionary<string, string> NoVars = new();

    private FlowStepResult Process(FlowDefinition flow, string? node, string input,
        IReadOnlyDictionary<string, string>? vars = null) =>
        _engine.Process(flow, node, vars ?? NoVars, input);

    [Fact]
    public void Start_RendersEntryMenu_WithOptions()
    {
        var r = Process(SampleFlow(), node: null, input: "hola");

        Assert.Equal("start", r.NextNodeId);
        Assert.False(r.Escalate);
        Assert.Contains("¿En qué te ayudamos?", r.Text);
        Assert.Contains("1) Pedido", r.Text);
        Assert.Contains("2) Agente", r.Text);
    }

    [Fact]
    public void ValidOption_FollowsMessageNode_AndChainsBackToMenu()
    {
        var r = Process(SampleFlow(), node:"start", input: "1");

        Assert.Equal("start", r.NextNodeId);          // pedido -> Next=start (vuelve al menú)
        Assert.False(r.Escalate);
        Assert.Contains("Tu pedido va en camino.", r.Text);
        Assert.Contains("1) Pedido", r.Text);
    }

    [Fact]
    public void InvalidOption_RepromptsSameMenu()
    {
        var r = Process(SampleFlow(), node:"start", input: "99");

        Assert.Equal("start", r.NextNodeId);
        Assert.False(r.Escalate);
        Assert.Contains("No entendí", r.Text);
        Assert.Contains("1) Pedido", r.Text);
    }

    [Fact]
    public void EscalateOption_SignalsEscalation_AndEndsFlow()
    {
        var r = Process(SampleFlow(), node:"start", input: "2");

        Assert.True(r.Escalate);
        Assert.Null(r.NextNodeId);
        Assert.Contains("funcionario", r.Text);
    }

    [Fact]
    public void StaleNode_RestartsAtEntry()
    {
        var r = Process(SampleFlow(), node:"nodo-inexistente", input: "1");

        Assert.Equal("start", r.NextNodeId);
        Assert.Contains("¿En qué te ayudamos?", r.Text);
    }

    // ── Corte 2: nodos Input (captura/validación) + variables ───────────────────

    private static FlowDefinition InputFlow() => new(
        EntryNodeId: "ask",
        Nodes:
        [
            new FlowNode("ask", FlowNodeType.Input, "¿Cuál es tu correo?",
                Next: "done", Variable: "email",
                Validation: @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                ValidationError: "Correo no válido."),
            new FlowNode("done", FlowNodeType.Message, "Gracias, te escribimos a {email}.")
        ]);

    [Fact]
    public void Input_InvalidValue_RepromptsAndDoesNotCapture()
    {
        var r = Process(InputFlow(), node: "ask", input: "no-es-correo");

        Assert.Equal("ask", r.NextNodeId);
        Assert.Contains("Correo no válido.", r.Text);
        Assert.False(r.Variables.ContainsKey("email"));
    }

    [Fact]
    public void Input_ValidValue_CapturesVariable_AndSubstitutesInNextMessage()
    {
        var r = Process(InputFlow(), node: "ask", input: "ana@andinashop.com");

        Assert.Null(r.NextNodeId);
        Assert.Equal("ana@andinashop.com", r.Variables["email"]);
        Assert.Contains("te escribimos a ana@andinashop.com", r.Text);
    }
}
