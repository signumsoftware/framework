using Signum.Entities;
using Signum.Entities.Isolation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace Signum.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class IsolationServiceBehaviorAttribute : Attribute, IServiceBehavior
    {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var ep in serviceDescription.Endpoints)
                foreach (var op in ep.Contract.Operations)
                    op.Behaviors.Add(new IsolationOperationBehavior());
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
    }


    public sealed class IsolationOperationBehavior : IOperationBehavior
    {
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            clientOperation.Formatter = new IsolationClientFormatter(clientOperation.Formatter);
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Formatter = new IsolationDispatchFormatter(dispatchOperation.Formatter);
        }

        public void Validate(OperationDescription operationDescription) { }
    }


    public class IsolationClientFormatter : IClientMessageFormatter
    {
        private readonly IClientMessageFormatter m_inner;

        public IsolationClientFormatter(IClientMessageFormatter inner)
        {
            m_inner = inner;
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            var result = m_inner.DeserializeReply(message, parameters);
            int index = message.Headers.FindHeader("isolation", "urn:context");
            if (index != -1)
            {
                var reader = message.Headers.GetReaderAtHeader(index);
                IsolationDN.CurrentThreadVariable.Value = Lite.Parse<IsolationDN>(reader.ReadString());
            }
            return result;
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var msg = m_inner.SerializeRequest(messageVersion, parameters);
            var iso = IsolationDN.CurrentThreadVariable.Value;
            if (iso != null)
            {
                msg.Headers.Add(MessageHeader.CreateHeader("isolation", "urn:context", iso.KeyLong(), false));
                IsolationDN.CurrentThreadVariable.Value = null;
            }
            return msg;
        }
    }


    public class IsolationDispatchFormatter : IDispatchMessageFormatter
    {
        private readonly IDispatchMessageFormatter m_inner;

        public IsolationDispatchFormatter(IDispatchMessageFormatter inner)
        {
            m_inner = inner;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            int index = message.Headers.FindHeader("isolation", "urn:context");
            if (index != -1)
            {
                var reader = message.Headers.GetReaderAtHeader(index);
                IsolationDN.CurrentThreadVariable.Value = Lite.Parse<IsolationDN>(reader.ReadString());
            }

            if(m_inner != null)
            m_inner.DeserializeRequest(message, parameters);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var msg = m_inner.SerializeReply(messageVersion, parameters, result);
            var iso = IsolationDN.CurrentThreadVariable.Value;
            if (iso != null)
            {
                msg.Headers.Add(MessageHeader.CreateHeader("isolation", "urn:context", iso.KeyLong(), false));
                IsolationDN.CurrentThreadVariable.Value = null;
            }
            return msg;
        }
    }
}
