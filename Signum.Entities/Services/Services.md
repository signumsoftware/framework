# Services

Signum.Service is the folder in Signum.Entities that contains the WCF Service Contracts of the server required by **Signum.Windows**. 

The great benefit of Signum.Windows is that it provides controls that allow working with entities in a generic way, reducing dramatically the amount of code you have to write. For this to work the server has to provide a generic way of saving and retrieving any entity, this is defined in `IBaseServer` interface. 

There are other useful contracts:

* **IBaseServer:** Support for saving and retrieving entities, as well as other basic plumbing stuff like `FindAllMixins`,  `ServerTypes`, etc..
* **IDynamicQueryServer:** Supports the `SearchControl`:  `GetQueryDescription`, `ExecuteQuery`, etc.. 
* **IOperationServer:** Supports the remote execution of operations: `GetCanExecuteAll`,  `ExecuteOperation`, `Construct`, etc...

Additionally, many modules add their own interfaces. 

### NetDataContractAttribute

All of these interfaces share a common philosophy. They use `NetDataContractAttribute` that changes 
[`NetDataContractSerializer`](http://msdn.microsoft.com/en-us/library/system.runtime.serialization.netdatacontractserializer(v=vs.110).aspx).

`NetDataContractAttribute` uses is like WCF's Cinderella. A hidden gem completely despised by Microsoft. It allows shared-Type communication between client and server. That means that the same types are going to be used on both sides of the channel, so the assembly where these entities are defined has to be available on both sides as well. 

No need to generate a proxy class for your services, neither for the entities being sent, it behaves like old Remoting.

Microsoft is trying to discourage this approach because it tightly couples client and server: they have to use the same technology (.Net Framework) and have to evolve together. They are not wrong, but we were already prepared for that:

* We are using .Net on both sides
* Almost any change in an Entitiy will affect client and server (i.e. Validation). Very often it will impact Database also (adding field) and the UI (adding an imput box). So they were indirectly coupled already.

## Define the Shared Contract

Since we are using WCF, we just have to write the contract of our service as an plain old interface decorated with some attributes. Nothing new here.

Some modules, however, need client and server code to be deployed, and require the service to implement some custom methods defined in an interface. 

Accomplishing both things at the same time is as easy as defining our custom service interface as the union of all the needed interfaces (using interface implementation) and your own custom methods.

```C#
[ServiceContract(SessionMode = SessionMode.Required)]
public interface IServerSouthwind : IBaseServer, IDynamicQueryServer, IOperationServer,
    ILoginServer, IProcessServer, IQueryServer, IChartServer, IExcelReportServer, IUserQueryServer, IDashboardServer, IUserAssetsServer,
    IProfilerServer, IQueryAuthServer, IPropertyAuthServer, ITypeAuthServer, IPermissionAuthServer, IOperationAuthServer
{
    //Add custom method here...
    [OperationContract, NetDataContract]
    byte[] GenerateReport(Lite<EmployeeEntity> employee);
}
```

## Set-up the Server 

Implementing the shared module is easy because most of the method are already implemented in `ServerBasic` (Framework) or `ServerExtensions` (Extensions) so you'll only need to implement your own custom methods. 

Just create a `ServerSouthwind.svc` in our web server application like this:   

```C#
public class ServerSouthwind : ServerExtensions, IServerSouthwind
{
    protected override T Return<T>(MethodBase mi, string description, Func<T> function)
    {
        try
        {
            string longDescription = mi.Name + description == null ? null : (" " + description);

            using (TimeTracker.Start(longDescription))
            using (HeavyProfiler.Log("WCF", () => longDescription))
            using (ScopeSessionFactory.OverrideSession(session))
            {
                return function();
            }
        }
        catch (Exception e)
        {
            e.LogException(el =>
            {
                el.ControllerName = GetType().Name;
                el.ActionName = mi.Name;
                el.QueryString = description;
                el.Version = Schema.Current.Version.ToString();
            });
            throw new FaultException(e.Message);
        }
        finally
        {
            Statics.CleanThreadContextAndAssert();
        }
    }

    //Implement custom methods here...
    public byte[] GenerateReport(Lite<EmployeeEntity> employee)
    {
        return Return(MethodInfo.GetCurrentMethod(), null,
            () => ReportLogic.GenerateReport(employee));
    }
}
```

Additionally, you'll need to implement `Return` method that is executed for any operation calling `Return` or `Execute`. This method does all the necessary plumbing that you want to add in your facade (similar to FilterAttributes in ASP.Net MVC), for example: 

* `ScopeSessionFactory.OverrideSession` restore the session on every call.
* `TimeTracker.Start` saves aggregated performance information (in Profiler module)
* `HeavyProfiler.Log` saves detailed performance information if enabled (in Profiler module)
* `LogException` saves a .Net `System.Exception` as a `ExceptionEntity` in the database.
* convert your exception to `FaultException` to give detailed information to the client without shouting down the service
* `Statics.CleanThreadContextAndAssert` asserts that all the thread variables are clean, and also cleans them anyway if not. 

Finally, we have to configure the service in `web.condig`

```xml
  <system.serviceModel>
    <bindings>
      <wsHttpBinding>
        <binding name="SouthwindBinding" closeTimeout="00:05:00" openTimeout="00:05:00"
          receiveTimeout="00:20:00" sendTimeout="00:05:00" maxReceivedMessageSize="2147483647">
          <readerQuotas maxStringContentLength="2147483647" maxArrayLength="2147483647" />
        </binding>
      </wsHttpBinding>
    </bindings>
    <services>
      <service name="Southwind.Web.ServerSouthwind" behaviorConfiguration="SouthwindBehavior">
        <endpoint address="" binding="wsHttpBinding" bindingConfiguration="SouthwindBinding" contract="Southwind.Services.IServerSouthwind">
          <identity>
            <dns value="localhost"/>
          </identity>
        </endpoint>
        <endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange"/>
      </service>
    </services>
    <behaviors>
      <serviceBehaviors>
        <behavior name="SouthwindBehavior">
          <!-- To avoid disclosing metadata information, set the value below to false and remove the metadata endpoint above before deployment -->
          <serviceMetadata httpGetEnabled="true"/>
          <!-- To receive exception details in faults for debugging purposes, set the value below to true.  Set to false before deployment to avoid disclosing exception information -->
          <serviceDebug includeExceptionDetailInFaults="true"/>
        </behavior>
      </serviceBehaviors>
    </behaviors>
  </system.serviceModel>
```

> **Note:** Signum Framework doesn't impose anything else to WPF connection than IBaseServer and using NetDataContract on any method. You're still free to play with WCF configuration.

## Set-up the Client

In the client windows application, we need to configure the the `app.config`:


```xml
  <system.serviceModel>
    <bindings>
      <wsHttpBinding>
        <binding name="friendBinding" closeTimeout="01:00:00" openTimeout="01:00:00" receiveTimeout="01:00:00" sendTimeout="01:00:00" maxReceivedMessageSize="838860800">
          <readerQuotas maxArrayLength="2147483647" maxStringContentLength="2147483647"/>
        </binding>
      </wsHttpBinding>
    </bindings>
    <client>
      <endpoint address="http://localhost/Southwind.Web/ServerSouthwind.svc" binding="wsHttpBinding" bindingConfiguration="friendBinding" contract="Southwind.Services.IServerSouthwind" name="server"/>
    </client>
  
  </system.serviceModel>
```

Then create the method that instantiates a `ChannelFactory<IServerSouthwind>` and transparent proxy (channel): 

```C#
static ChannelFactory<IServerSouthwind> channelFactory;
private static IServerSouthwind RemoteServer()
{
    if (channelFactory == null)
        channelFactory = new ChannelFactory<IServerSouthwind>("server");

    IServerSouthwind result = channelFactory.CreateChannel();
    return result;
}
``` 

And finally associate the factory method  with Server static class

```C#
Server.SetNewServerCallback(RemoteServer);
```

The server class instantiates the server when necessary and re-instantiates it if there's a connection problem or the session is expired. 


In order to call the server from the client, use the `Server.Return` method:

```C#
Server.Return((IServerSouthwind server)=>server.GenerateReport(myEmployee)); 
```

This method has two benefits: 

* If the server faulted (or session expired) the method re-creates the server and retries the call automatically, asking to log-in if necessary (authorization module). 

* The method simplifies writing independent modules that do not depend on the global contract (i.e.:`IServerSouthwind`) but in some module-specific contract (i.e.: `IOperationContract`) 
