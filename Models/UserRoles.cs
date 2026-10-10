namespace LinchpinBackend.Models;

/// <summary>Perfis de acesso do sistema (os valores são os mesmos usados pelo front-end).</summary>
public static class UserRoles
{
    public const string Admin = "administrador";
    public const string Analyst = "analista";
    public const string Technician = "tecnico";
    public const string Maintenance = "manutencao";

    public static readonly string[] All = [Admin, Analyst, Technician, Maintenance];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);

    public static string DefaultImage(string role) => role switch
    {
        Admin => "assets/images/users/user-default-admin.png",
        Analyst => "assets/images/users/user-default-analist.png",
        Technician => "assets/images/users/user-default-tec.png",
        Maintenance => "assets/images/users/user-default-manitence.png",
        _ => "assets/images/users/user-default-analist.png"
    };
}
