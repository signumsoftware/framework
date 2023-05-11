using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Basics;

public interface IEmailOwnerEntity : IEntity
{
}

[DescriptionOptions(DescriptionOptions.Description | DescriptionOptions.Members)]
public class EmailOwnerData : IEquatable<EmailOwnerData>
{
    public Lite<IEmailOwnerEntity>? Owner { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public CultureInfoEntity? CultureInfo { get; set; }
    public Guid? AzureUserId { get; set; }

    public override bool Equals(object? obj) => obj is EmailOwnerData eod && Equals(eod);
    public bool Equals(EmailOwnerData? other)
    {
        return Owner != null && other != null && other.Owner != null && Owner.Equals(other.Owner);
    }


    public override int GetHashCode()
    {
        return Owner == null ? base.GetHashCode() : Owner.GetHashCode();
    }

    public override string ToString()
    {
        return "{0} <{1}> ({2})".FormatWith(DisplayName, Email, Owner);
    }
}
