namespace IkOtomasyon.Api.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ik-otomasyon";
    public string Audience { get; set; } = "ik-otomasyon";
    public string Secret { get; set; } = "CHANGE_ME_SECRET";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
