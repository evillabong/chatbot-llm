using Microsoft.EntityFrameworkCore;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;
using System.Text.Json;

namespace Mimo.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos principal de MIMO.
/// Soporta multi-tenancy mediante esquemas separados por tenant en PostgreSQL.
/// El search_path debe establecerse por el TenantResolutionMiddleware antes de cada petición.
/// </summary>
public class MimoDbContext : DbContext
{
    public MimoDbContext(DbContextOptions<MimoDbContext> options) : base(options) { }

    // Tablas del esquema public (catálogo global)
    public DbSet<Tenant> Tenants => Set<Tenant>();

    // Tablas del esquema por tenant
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Tenant ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(t => t.Slug).HasColumnName("slug").HasMaxLength(50).IsRequired();
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(t => t.Plan).HasColumnName("plan").HasMaxLength(50);
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.UpdatedAt).HasColumnName("updated_at");

            // Configuración serializada como JSON
            e.Property(t => t.Configuration)
                .HasColumnName("configuration")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<TenantConfiguration>(v, JsonSerializerOptions.Default) ?? new());
        });

        // ── Agent ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Agent>(e =>
        {
            e.ToTable("agents");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.TenantId).HasColumnName("tenant_id");
            e.Property(a => a.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
            e.Property(a => a.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            e.Property(a => a.Alias).HasColumnName("alias").HasMaxLength(100).IsRequired();
            e.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(a => a.MaxConcurrentSessions).HasColumnName("max_concurrent_sessions").HasDefaultValue(5);
            e.Property(a => a.CreatedAt).HasColumnName("created_at");
        });

        // ── Role ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.TenantId).HasColumnName("tenant_id");
            e.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(r => r.Description).HasColumnName("description");
            e.Property(r => r.PriorityLevel).HasColumnName("priority_level").HasDefaultValue(0);
            e.Property(r => r.CanViewAllTickets).HasColumnName("can_view_all_tickets").HasDefaultValue(false);
            e.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(r => r.CreatedAt).HasColumnName("created_at");
        });

        // ── AgentRole (pivote) ────────────────────────────────────────────────
        modelBuilder.Entity<AgentRole>(e =>
        {
            e.ToTable("agent_roles");
            e.HasKey(ar => new { ar.AgentId, ar.RoleId });
            e.Property(ar => ar.AgentId).HasColumnName("agent_id");
            e.Property(ar => ar.RoleId).HasColumnName("role_id");

            e.HasOne(ar => ar.Agent).WithMany(a => a.AgentRoles).HasForeignKey(ar => ar.AgentId);
            e.HasOne(ar => ar.Role).WithMany(r => r.AgentRoles).HasForeignKey(ar => ar.RoleId);
        });

        // ── DocumentCategory ──────────────────────────────────────────────────
        modelBuilder.Entity<DocumentCategory>(e =>
        {
            e.ToTable("document_categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.TenantId).HasColumnName("tenant_id");
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(c => c.ParentCategoryId).HasColumnName("parent_category_id");

            e.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Document ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.TenantId).HasColumnName("tenant_id");
            e.Property(d => d.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
            e.Property(d => d.Content).HasColumnName("content").IsRequired();
            e.Property(d => d.Visibility).HasColumnName("visibility").HasConversion<string>();
            e.Property(d => d.CategoryId).HasColumnName("category_id");
            e.Property(d => d.RelatedRoleId).HasColumnName("related_role_id");
            e.Property(d => d.Tags).HasColumnName("tags").HasColumnType("text[]");
            e.Property(d => d.PriorityLevel).HasColumnName("priority_level").HasDefaultValue(0);
            // El embedding se mapea como vector en pgvector (dimensión 1536)
            e.Property(d => d.Embedding).HasColumnName("embedding").HasColumnType("vector(1536)");
            e.Property(d => d.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(d => d.CreatedBy).HasColumnName("created_by");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Conversation ──────────────────────────────────────────────────────
        modelBuilder.Entity<Conversation>(e =>
        {
            e.ToTable("conversations");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.TenantId).HasColumnName("tenant_id");
            e.Property(c => c.ExternalUserId).HasColumnName("external_user_id").HasMaxLength(200).IsRequired();
            e.Property(c => c.ExternalUserName).HasColumnName("external_user_name").HasMaxLength(200);
            e.Property(c => c.Channel).HasColumnName("channel").HasConversion<string>();
            e.Property(c => c.Status).HasColumnName("status").HasConversion<string>();
            e.Property(c => c.IsAuthenticated).HasColumnName("is_authenticated").HasDefaultValue(false);
            e.Property(c => c.CustomerEmail).HasColumnName("customer_email").HasMaxLength(200);
            e.Property(c => c.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(30);
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.LastMessageAt).HasColumnName("last_message_at");
            e.Property(c => c.ResolvedAt).HasColumnName("resolved_at");

            e.HasMany(c => c.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId);
            e.HasOne(c => c.Ticket).WithOne(t => t.Conversation).HasForeignKey<Ticket>(t => t.ConversationId);
        });

        // ── Message ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.ConversationId).HasColumnName("conversation_id");
            e.Property(m => m.Role).HasColumnName("role").HasConversion<string>();
            e.Property(m => m.SenderId).HasColumnName("sender_id");
            e.Property(m => m.Content).HasColumnName("content").IsRequired();
            e.Property(m => m.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            e.Property(m => m.CreatedAt).HasColumnName("created_at");
        });

        // ── Ticket ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.ConversationId).HasColumnName("conversation_id");
            e.Property(t => t.AssignedRoleId).HasColumnName("assigned_role_id");
            e.Property(t => t.AssignedAgentId).HasColumnName("assigned_agent_id");
            e.Property(t => t.Priority).HasColumnName("priority").HasConversion<string>();
            e.Property(t => t.Status).HasColumnName("status").HasConversion<string>();
            e.Property(t => t.InternalNotes).HasColumnName("internal_notes");
            e.Property(t => t.EscalationReason).HasColumnName("escalation_reason");
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.Property(t => t.AssignedAt).HasColumnName("assigned_at");
            e.Property(t => t.FirstResponseAt).HasColumnName("first_response_at");
            e.Property(t => t.ResolvedAt).HasColumnName("resolved_at");

            e.HasMany(t => t.TransferRecords).WithOne(tr => tr.Ticket).HasForeignKey(tr => tr.TicketId);
            e.HasOne(t => t.Survey).WithOne(s => s.Ticket).HasForeignKey<SatisfactionSurvey>(s => s.TicketId);
        });

        // ── TransferRecord ────────────────────────────────────────────────────
        modelBuilder.Entity<TransferRecord>(e =>
        {
            e.ToTable("transfer_records");
            e.HasKey(tr => tr.Id);
            e.Property(tr => tr.Id).HasColumnName("id");
            e.Property(tr => tr.TicketId).HasColumnName("ticket_id");
            e.Property(tr => tr.FromRoleId).HasColumnName("from_role_id");
            e.Property(tr => tr.FromAgentId).HasColumnName("from_agent_id");
            e.Property(tr => tr.ToRoleId).HasColumnName("to_role_id");
            e.Property(tr => tr.ToAgentId).HasColumnName("to_agent_id");
            e.Property(tr => tr.TransferredBy).HasColumnName("transferred_by");
            e.Property(tr => tr.Reason).HasColumnName("reason");
            e.Property(tr => tr.IsPartial).HasColumnName("is_partial").HasDefaultValue(false);
            e.Property(tr => tr.ContextNote).HasColumnName("context_note");
            e.Property(tr => tr.CreatedAt).HasColumnName("created_at");
        });

        // ── SatisfactionSurvey ────────────────────────────────────────────────
        modelBuilder.Entity<SatisfactionSurvey>(e =>
        {
            e.ToTable("satisfaction_surveys");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.TicketId).HasColumnName("ticket_id");
            e.Property(s => s.Rating).HasColumnName("rating");
            e.Property(s => s.Observations).HasColumnName("observations");
            e.Property(s => s.RecordedAt).HasColumnName("recorded_at");
        });

        // ── InternalChatMessage ───────────────────────────────────────────────
        modelBuilder.Entity<InternalChatMessage>(e =>
        {
            e.ToTable("internal_chat_messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.TenantId).HasColumnName("tenant_id");
            e.Property(m => m.FromAgentId).HasColumnName("from_agent_id");
            e.Property(m => m.ToAgentId).HasColumnName("to_agent_id");
            e.Property(m => m.RelatedTicketId).HasColumnName("related_ticket_id");
            e.Property(m => m.Content).HasColumnName("content").IsRequired();
            e.Property(m => m.IsRead).HasColumnName("is_read").HasDefaultValue(false);
            e.Property(m => m.CreatedAt).HasColumnName("created_at");
        });
    }
}
