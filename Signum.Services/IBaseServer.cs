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
        List<IdentifiableEntity> SaveList(List<IdentifiableEntity> list);

        [OperationContract, NetDataContract]
        List<Lazy> RetrieveAllLazy(Type lazyType, Type[] types);

        [OperationContract, NetDataContract]
        List<Lazy> FindLazyLike(Type lazyType, Type[] types, string subString, int count);

        [OperationContract, NetDataContract]
        Type[] FindImplementations(Type lazyType, MemberInfo[] members);

        [OperationContract, NetDataContract]
        Dictionary<Type, TypeDN> ServerTypes();
    }



    

    
}
