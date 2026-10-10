namespace LinchpinBackend.Infrastructure;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "linchpin-backend";
    public string Audience { get; set; } = "linchpin-front";
    public int ExpiresHours { get; set; } = 8;
}
