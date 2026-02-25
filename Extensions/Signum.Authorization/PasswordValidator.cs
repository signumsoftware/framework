using System.Text.RegularExpressions;

namespace Signum.Authorization;

public static class PasswordValidator
{
    public static string SpecialCharacters = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
    
    public static int MinimumPasswordLength = 5;

    public static PasswordValidationResult ValidatePassword(string password, Lite<RoleEntity>? userRole = null)
    {
        var result = new PasswordValidationResult();

        if (string.IsNullOrEmpty(password))
        {
            result.IsValid = false;
            return result;
        }

        var minLength = GetMinimumPasswordLength(userRole);
        if (password.Length < minLength)
        {
            result.IsValid = false;
            result.ErrorMessage = LoginAuthMessage.ThePasswordMustHaveAtLeast0Characters.NiceToString(minLength);
            return result;
        }

        result.IsValid = true;

        var complexityResult = CheckPasswordComplexity(password);
        if (!complexityResult.MeetsComplexity)
        {
            result.ComplexityWarning = LoginAuthMessage.PasswordComplexityWarning.NiceToString();
        }

        return result;
    }

    public static int GetMinimumPasswordLength(Lite<RoleEntity>? userRole)
    {
        if (userRole == null)
            return MinimumPasswordLength;

        var role = RoleEntity.RetrieveFromCache(userRole);
        if (role.MinPasswordLength.HasValue)
            return role.MinPasswordLength.Value;

        return MinimumPasswordLength;
    }

    public static PasswordComplexityResult CheckPasswordComplexity(string password)
    {
        var result = new PasswordComplexityResult
        {
            HasUppercase = password.Any(char.IsUpper),
            HasLowercase = password.Any(char.IsLower),
            HasDigit = password.Any(char.IsDigit),
            HasSpecialChar = password.Any(c => SpecialCharacters.Contains(c))
        };

        result.MeetsComplexity = result.HasUppercase && result.HasLowercase && result.HasDigit && result.HasSpecialChar;

        return result;
    }
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ComplexityWarning { get; set; }
}

public class PasswordComplexityResult
{
    public bool HasUppercase { get; set; }
    public bool HasLowercase { get; set; }
    public bool HasDigit { get; set; }
    public bool HasSpecialChar { get; set; }
    public bool MeetsComplexity { get; set; }
}
