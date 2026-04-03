using Signum.UserAssets;

namespace Signum.Mailing.MicrosoftGraph.RemoteEmails;


public class RemoteEmailFolderConverter : IFilterValueConverter
{

    public Result<string?>? TryGetExpression(object? value, Type targetType)
    {
        if (value is RemoteEmailFolderModel mod)
        {
            return new Result<string?>.Success(mod.FolderId);
        }

        return null;
    }

    public Result<object?>? TryParseExpression(string? expression, Type targetType)
    {
        if (targetType == typeof(RemoteEmailFolderModel) && expression.HasText())
        {
            return new Result<object?>.Success(new RemoteEmailFolderModel { FolderId = expression, DisplayName = expression });
        }

        return null;
    }

    public Result<Type>? IsValidExpression(string? expression, Type targetType, Type? currentEntityType)
    {
        if (targetType == typeof(RemoteEmailFolderModel) && expression.HasText())
        {
            return new Result<Type>.Success(targetType);
        }

        return null;
    }
}

