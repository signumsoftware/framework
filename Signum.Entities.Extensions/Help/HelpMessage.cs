using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Signum.Entities.Help
{
    public enum HelpMessage
    {
        [Description("{0} is a {1}")]
        _0IsA1,
        [Description("{0} is a {1} and shows {2}")]
        _0IsA1AndShows2,
        [Description("{0} is a calculated {1}")]
        _0IsACalculated1,
        [Description("{0} is a collection of elements {1}")]
        _0IsACollectionOfElements1,
        [Description("amount")]
        Amount,
        [Description("any")]
        Any,
        Appendices,
        Buscador,
        [Description("Call {0} over {1} of the {2}")]
        Call0Over1OfThe2,
        [Description("character")]
        Character,
        [Description("check")]
        Check,
        [Description("Constructs a new {0}")]
        ConstructsANew0,
        [Description("date")]
        Date,
        [Description("date and time")]
        DateTime,
        [Description("expressed in ")]
        ExpressedIn,
        [Description("from {0} of the {1} ")]
        From0OfThe1,
        [Description("from many {0}")]
        FromMany0,
        HelpDocumentation,
        HelpNotLoaded,
        [Description("integer")]
        Integer,
        [Description("Key {0} not found")]
        Key0NotFound,
        [Description("of the {0}")]
        OfThe0,
        [Description("or null")]
        OrNull,
        [Description("Property {0} does not exist in type {1}")]
        Property0NotExistsInType1,
        [Description("Query of {0}")]
        QueryOf0,
        [Description("Removes the {0} from the database")]
        RemovesThe0FromTheDatabase,
        [Description(". Should  ")]
        Should,
        [Description("string")]
        String,
        [Description("The {0}")]
        The0,
        [Description("the database version")]
        TheDatabaseVersion,
        [Description("the property {0}")]
        TheProperty0,
        [Description("value")]
        Value,
        [Description("value like {0}")]
        ValueLike0,
        [Description("your version")]
        YourVersion
    }

}
