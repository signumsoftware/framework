using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Signum.Entities;
using System.Collections;
using System.Data;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using Signum.Utilities;
using System.Collections.Concurrent;
using Signum.Entities.Basics;


namespace Signum.Services
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IBaseServer
    {
        [OperationContract, NetDataContract]
        IdentifiableEntity Retrieve(Type type, int id);

        [OperationContract, NetDataContract]
        IdentifiableEntity Save(IdentifiableEntity entidad); 

        [OperationContract, NetDataContract]
        List<IdentifiableEntity> RetrieveAll(Type type);

        [OperationContract, NetDataContract]
        List<Lite<IdentifiableEntity>> RetrieveAllLite(Type type);

        [OperationContract, NetDataContract]
        List<IdentifiableEntity> SaveList(List<IdentifiableEntity> list);

        [OperationContract, NetDataContract]
        List<Lite<IdentifiableEntity>> FindAllLite(Implementations implementations);

        [OperationContract, NetDataContract]
        List<Lite<IdentifiableEntity>> FindLiteLike(Implementations implementations, string subString, int count);

        [OperationContract, NetDataContract]
        Dictionary<PropertyRoute, Implementations> FindAllImplementations(Type root);

        [OperationContract, NetDataContract]
        Dictionary<Type, TypeDN> ServerTypes();

        [OperationContract, NetDataContract]
        Dictionary<Type, EntityType> EntityTypes();

        [OperationContract, NetDataContract]
        DateTime ServerNow();

        [OperationContract, NetDataContract]
        string GetToStr(Type type, int id);

        [OperationContract, NetDataContract]
        bool Exists(Type type, int id);

    }
}
