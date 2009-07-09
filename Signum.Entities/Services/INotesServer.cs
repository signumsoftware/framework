using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.Basics;

namespace Signum.Services
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface INotesServer
    {
        List<Lazy<INoteDN>> RetrieveNotes(Lazy<IdentifiableEntity> lazy); 
    }
}
