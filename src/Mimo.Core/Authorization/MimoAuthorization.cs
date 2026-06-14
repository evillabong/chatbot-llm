namespace Mimo.Core.Authorization;

/// <summary>
/// Nombres de roles y políticas de autorización usados en toda la plataforma.
/// Centralizados aquí para evitar literales duplicados entre el aprovisionamiento
/// de tenants (creación del rol Administrador) y la definición de políticas en la API.
/// </summary>
public static class MimoAuthorization
{
    /// <summary>Nombres de roles predefinidos del tenant.</summary>
    public static class Roles
    {
        /// <summary>Rol con acceso total a la configuración y a todos los tickets del tenant.</summary>
        public const string Administrator = "Administrador";

        /// <summary>
        /// Administrador de la plataforma (Mimo.Admin.Api). Es transversal a todos los tenants,
        /// no es un rol dentro de un tenant. Se emite como claim "role" en el token de SuperAdmin.
        /// </summary>
        public const string SuperAdmin = "SuperAdmin";
    }

    /// <summary>Nombres de políticas de autorización registradas en la API.</summary>
    public static class Policies
    {
        /// <summary>
        /// Requiere que el funcionario tenga el rol Administrador.
        /// Aplica a la gestión de funcionarios, roles y administración de la base de conocimiento.
        /// </summary>
        public const string TenantAdmin = "TenantAdmin";

        /// <summary>
        /// Requiere que el usuario sea un funcionario autenticado (claim agent_id presente).
        /// Aplica a las operaciones de atención: tickets, chat interno y lectura de conocimiento.
        /// </summary>
        public const string Agent = "Agent";

        /// <summary>
        /// Requiere el rol SuperAdmin. Única política de Mimo.Admin.Api.
        /// </summary>
        public const string SuperAdmin = "SuperAdmin";
    }
}
