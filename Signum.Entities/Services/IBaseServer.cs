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
        List<Lite> RetrieveAllLite(Type liteType, Implementations implementations);

        [OperationContract, NetDataContract]
        List<Lite> FindLiteLike(Type liteType, Implementations implementations, string subString, int count);

        [OperationContract, NetDataContract]
        Implementations FindImplementations(PropertyRoute entityPath);

        [OperationContract, NetDataContract]
        Dictionary<Type, TypeDN> ServerTypes();

        [OperationContract, NetDataContract]
        DateTime ServerNow();

        [OperationContract, NetDataContract]
        List<Lite<TypeDN>> TypesAssignableFrom(Type type);

        [OperationContract, NetDataContract]
        string GetToStr(Type type, int id);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class SuggestUserInterfaceAttribute : Attribute
    {
        public bool value; 

        public SuggestUserInterfaceAttribute() : this(true)
        {

        }
        public SuggestUserInterfaceAttribute(bool value)
        {
            this.value = value; 
        }

        static ConcurrentDictionary<MethodBase, bool?> dictionary = new ConcurrentDictionary<MethodBase, bool?>();  

        internal static bool? Suggests(MethodBase mi)
        {
            return dictionary.GetOrAdd(mi, _=>
            {
                var attr = mi.SingleAttributeInherit<SuggestUserInterfaceAttribute>();
                     
                if (attr == null)
                    return null;

                return attr.value; 
            }); 
        }
    }
}
