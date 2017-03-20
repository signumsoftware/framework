using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Signum.Entities.Services
{
    //http://www.olegsych.com/2008/07/simplifying-wcf-using-exceptions-as-faults/
    public class ExceptionMarshallingBehaviorAttribute : Attribute, IServiceBehavior, IEndpointBehavior, IContractBehavior
    {
        #region IContractBehavior Members

        void IContractBehavior.AddBindingParameters(ContractDescription contract, ServiceEndpoint endpoint, BindingParameterCollection parameters)
        {
        }

        void IContractBehavior.ApplyClientBehavior(ContractDescription contract, ServiceEndpoint endpoint, ClientRuntime runtime)
        {
            Debug.WriteLine(string.Format("Applying client ExceptionMarshallingBehavior to contract {0}", contract.ContractType));
            this.ApplyClientBehavior(runtime);
        }

        void IContractBehavior.ApplyDispatchBehavior(ContractDescription contract, ServiceEndpoint endpoint, DispatchRuntime runtime)
        {
            Debug.WriteLine(string.Format("Applying dispatch ExceptionMarshallingBehavior to contract {0}", contract.ContractType.FullName));
            this.ApplyDispatchBehavior(runtime.ChannelDispatcher);
        }

        void IContractBehavior.Validate(ContractDescription contract, ServiceEndpoint endpoint)
        {
        }

        #endregion

        #region IEndpointBehavior Members

        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection parameters)
        {
        }

        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime runtime)
        {
            Debug.WriteLine(string.Format("Applying client ExceptionMarshallingBehavior to endpoint {0}", endpoint.Address));
            this.ApplyClientBehavior(runtime);
        }

        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            Debug.WriteLine(string.Format("Applying dispatch ExceptionMarshallingBehavior to endpoint {0}", endpoint.Address));
            this.ApplyDispatchBehavior(dispatcher.ChannelDispatcher);
        }

        void IEndpointBehavior.Validate(ServiceEndpoint endpoint)
        {
        }

        #endregion

        #region IServiceBehavior Members

        void IServiceBehavior.AddBindingParameters(ServiceDescription service, ServiceHostBase host, Collection<ServiceEndpoint> endpoints, BindingParameterCollection parameters)
        {
        }

        void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription service, ServiceHostBase host)
        {
            Debug.WriteLine(string.Format("Applying dispatch ExceptionMarshallingBehavior to service {0}", service.ServiceType.FullName));
            foreach (ChannelDispatcher dispatcher in host.ChannelDispatchers)
            {
                this.ApplyDispatchBehavior(dispatcher);
            }
        }

        void IServiceBehavior.Validate(ServiceDescription service, ServiceHostBase host)
        {
        }

        #endregion

        #region Private Members

        private void ApplyClientBehavior(ClientRuntime runtime)
        {
            // Don't add a message inspector if it already exists
            foreach (IClientMessageInspector messageInspector in runtime.MessageInspectors)
            {
                if (messageInspector is ExceptionMarshallingMessageInspector)
                {
                    return;
                }
            }

            runtime.MessageInspectors.Add(new ExceptionMarshallingMessageInspector());
        }

        private void ApplyDispatchBehavior(ChannelDispatcher dispatcher)
        {
            // Don't add an error handler if it already exists
            foreach (IErrorHandler errorHandler in dispatcher.ErrorHandlers)
            {
                if (errorHandler is ExceptionMarshallingErrorHandler)
                {
                    return;
                }
            }

            dispatcher.ErrorHandlers.Add(new ExceptionMarshallingErrorHandler());
        }

        #endregion

        public class ExceptionMarshallingMessageInspector : IClientMessageInspector
        {
            [DebuggerStepThrough]
            void IClientMessageInspector.AfterReceiveReply(ref Message reply, object correlationState)
            {
                if (reply.IsFault)
                {
                    // Create a copy of the original reply to allow default processing of the message
                    MessageBuffer buffer = reply.CreateBufferedCopy(Int32.MaxValue);
                    Message copy = buffer.CreateMessage();  // Create a copy to work with
                    reply = buffer.CreateMessage();         // Restore the original message

                    object faultDetail = ReadFaultDetail(copy);
                    if (faultDetail is Exception exception)
                    {
                        throw exception;
                    }
                }
            }

            object IClientMessageInspector.BeforeSendRequest(ref Message request, IClientChannel channel)
            {
                return null;
            }

            private static object ReadFaultDetail(Message reply)
            {
                const string detailElementName = "Detail";

                using (XmlDictionaryReader reader = reply.GetReaderAtBodyContents())
                {
                    // Find <soap:Detail>
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.LocalName == detailElementName)
                        {
                            break;
                        }
                    }

                    // Did we find it?
                    if (reader.NodeType != XmlNodeType.Element || reader.LocalName != detailElementName)
                    {
                        return null;
                    }

                    // Move to the contents of <soap:Detail>
                    if (!reader.Read())
                    {
                        return null;
                    }

                    // Deserialize the fault
                    NetDataContractSerializer serializer = new NetDataContractSerializer();
                    try
                    {
                        return serializer.ReadObject(reader);
                    }
                    catch (FileNotFoundException)
                    {
                        // Serializer was unable to find assembly where exception is defined 
                        return null;
                    }
                }
            }
        }

        public class ExceptionMarshallingErrorHandler : IErrorHandler
        {
            bool IErrorHandler.HandleError(Exception error)
            {
                if (error is FaultException)
                {
                    return false; // Let WCF do normal processing
                }
                else
                {
                    return true; // Fault message is already generated
                }
            }

            void IErrorHandler.ProvideFault(Exception error, MessageVersion version, ref Message fault)
            {
                if (error is FaultException)
                {
                    // Let WCF do normal processing
                }
                else
                {
                    // Generate fault message manually
                    MessageFault messageFault = MessageFault.CreateFault(
                        new FaultCode("Sender"),
                        new FaultReason(error.Message),
                        error,
                        new NetDataContractSerializer());
                    fault = Message.CreateMessage(version, messageFault, null);
                }
            }
        }
    }
}
