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
        Entity Retrieve(Type type, PrimaryKey id);

        [OperationContract, NetDataContract]
        Entity Save(Entity entity); 

        [OperationContract, NetDataContract]
        List<Entity> RetrieveAll(Type type);

        [OperationContract, NetDataContract]
        List<Lite<Entity>> RetrieveAllLite(Type type);

        [OperationContract, NetDataContract]
        List<Entity> SaveList(List<Entity> list);

        [OperationContract, NetDataContract]
        List<Lite<Entity>> FindAllLite(Implementations implementations);

        [OperationContract, NetDataContract]
        List<Lite<Entity>> FindLiteLike(Implementations implementations, string subString, int count);

        [OperationContract, NetDataContract]
        Dictionary<PropertyRoute, Implementations> FindAllImplementations(Type root);

        [OperationContract, NetDataContract]
        Dictionary<Type, HashSet<Type>> FindAllMixins();

        [OperationContract, NetDataContract]
        Dictionary<Type, TypeEntity> ServerTypes();

        [OperationContract, NetDataContract]
        DateTime ServerNow();

        [OperationContract, NetDataContract]
        string GetToStr(Type type, PrimaryKey id);

        [OperationContract, NetDataContract]
        bool Exists(Type type, PrimaryKey id);

        [OperationContract, NetDataContract]
        long Ticks(Lite<Entity> entity);

        [OperationContract, NetDataContract]
        Dictionary<string, PrimaryKey> GetSymbolIds(Type type);

        [OperationContract, NetDataContract]
        Dictionary<string, SemiSymbol> GetSemiSymbolFromDatabase(Type type);

        [OperationContract, NetDataContract]
        Dictionary<Type, Type> ImportPrimaryKeyDefinitions();
    }
}
