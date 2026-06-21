using Microsoft.EntityFrameworkCore;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data.Configurations.Tenant;

namespace Mimo.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos para el esquema de un tenant específico.
/// El search_path de PostgreSQL se configura en el middleware de resolución de tenant
/// antes de que este contexto ejecute cualquier consulta.
/// </summary>
public class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AgentRole> AgentRoles => Set<AgentRole>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TransferRecord> TransferRecords => Set<TransferRecord>();
    public DbSet<SatisfactionSurvey> SatisfactionSurveys => Set<SatisfactionSurvey>();
    public DbSet<InternalChatMessage> InternalChatMessages => Set<InternalChatMessage>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<ChatbotFlow> ChatbotFlows => Set<ChatbotFlow>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<KnowledgeQuerySignal> KnowledgeQuerySignals => Set<KnowledgeQuerySignal>();
    public DbSet<KnowledgeSuggestion> KnowledgeSuggestions => Set<KnowledgeSuggestion>();
    public DbSet<McpServer> McpServers => Set<McpServer>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Registra todas las configuraciones de entidades del tenant
        modelBuilder.ApplyConfiguration(new AgentConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new AgentRoleConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyConfiguration(new TicketConfiguration());
        modelBuilder.ApplyConfiguration(new TransferRecordConfiguration());
        modelBuilder.ApplyConfiguration(new SatisfactionSurveyConfiguration());
        modelBuilder.ApplyConfiguration(new InternalChatMessageConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookDeliveryConfiguration());
        modelBuilder.ApplyConfiguration(new ChatbotFlowConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new KnowledgeQuerySignalConfiguration());
        modelBuilder.ApplyConfiguration(new KnowledgeSuggestionConfiguration());
        modelBuilder.ApplyConfiguration(new McpServerConfiguration());
        modelBuilder.ApplyConfiguration(new WorkTaskConfiguration());

        // Habilita la extensión pgvector para el esquema activo
        modelBuilder.HasPostgresExtension("vector");
    }
}
