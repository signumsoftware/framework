using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;
using System.Diagnostics;


namespace Signum.Services
{
    [DebuggerStepThrough]
    public class NetDataContractAttribute : Attribute, IOperationBehavior
    {
        public void AddBindingParameters(OperationDescription description, BindingParameterCollection parameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription description, ClientOperation proxy)
        {
            ReplaceDataContractSerializerOperationBehavior(description);
        }
        public void ApplyDispatchBehavior(OperationDescription description, DispatchOperation dispatch)
        {
            ReplaceDataContractSerializerOperationBehavior(description);
        }

        public void Validate(OperationDescription description)
        {
        }

        private static void ReplaceDataContractSerializerOperationBehavior(OperationDescription description)
        {
            DataContractSerializerOperationBehavior dcsOperationBehavior = description.Behaviors.Find<DataContractSerializerOperationBehavior>();
            if (dcsOperationBehavior != null)
            {
                description.Behaviors.Remove(dcsOperationBehavior);
                description.Behaviors.Add(new NetDataContractSerializerOperationBehavior(description));
            }
        }

        [DebuggerStepThrough]
        public class NetDataContractSerializerOperationBehavior : DataContractSerializerOperationBehavior
        {
            public NetDataContractSerializerOperationBehavior(OperationDescription operationDescription)
                : base(operationDescription)
            {
            }
            public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
            {
                return new NetDataContractSerializer();
            }
            public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
            {
                return new NetDataContractSerializer();
            }
        }
    }
}
