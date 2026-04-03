namespace Signum.Authorization.AuthToken;

public class AuthTokenConfigurationEmbedded : EmbeddedEntity
{
    [Unit("mins")]
    public int RefreshTokenEvery { get; set; } = 30;

    [DateInPastValidator]
    public DateTime? RefreshAnyTokenPreviousTo { get; set; }
}
