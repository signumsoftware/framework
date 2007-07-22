<%@ Application Language="VB" %>

<script runat="server">

    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Code that runs on application startup
        
        Dim mrecurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim colValConfigServer As System.Collections.Specialized.NameValueCollection
        Dim htd As New Generic.Dictionary(Of String, Object)
        Dim connectionstring As String
        
        colValConfigServer = ConfigurationManager.AppSettings

        If colValConfigServer.Item("ConnectionString") Is Nothing Then
            connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd=''"
        Else
            connectionstring = colValConfigServer.Item("ConnectionString")
        End If

        htd.Add("connectionstring", connectionstring)
        
        mrecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a BDsql", "sqls", htd)
        Me.Application.Add("recurso", mrecurso)
        Framework.Configuracion.AppConfiguracion.DatosConfig.Add("recurso", mrecurso)

        'Se carga en Datos de configuración los datos del WebConfig
        Dim clave As String
        For a As Integer = 0 To colValConfigServer.Count - 1
            clave = colValConfigServer.GetKey(a)
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add(clave, colValConfigServer.Item(clave))
        Next
        
        'Asignamos el mapeado de gestor de instanciación
        ' Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposAMVDocsEntrantesLN
        '  Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New FN.RiesgosVehiculos.Test.GestorMapPersistenciaCamposMotosTest
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GSAMV.AD.GestorMapPersistenciaCamposGSAMV()

        'Se cargan los datos necesarios en memoria del servidor
        CargarDatosCache(mrecurso)
        
    End Sub
    
    Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Code that runs on application shutdown
    End Sub
        
    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        ' Code that runs when an unhandled error occurs
    End Sub

    Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Code that runs when a new session is started
        
        Dim colValConfigServer As System.Collections.Specialized.NameValueCollection
        
        colValConfigServer = ConfigurationManager.AppSettings
        
        'Se carga en Datos de configuración los datos del WebConfig
        Dim clave As String
        For a As Integer = 0 To colValConfigServer.Count - 1
            clave = colValConfigServer.GetKey(a)
            If Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey(clave) Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Item(clave) = colValConfigServer.Item(clave)
            Else
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add(clave, colValConfigServer.Item(clave))
            End If
        Next
        
    End Sub

    Sub Session_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Code that runs when a session ends. 
        ' Note: The Session_End event is raised only when the sessionstate mode
        ' is set to InProc in the Web.config file. If session mode is set to StateServer 
        ' or SQLServer, the event is not raised.
    End Sub
   
    Sub CargarDatosCache(ByVal mrecurso As Framework.LogicaNegocios.Transacciones.RecursoLN)
        'Grafo tarificación
        
        If Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey("CargaTarificadorInicioAplicacion") AndAlso Framework.Configuracion.AppConfiguracion.DatosConfig("CargaTarificadorInicioAplicacion") Then
            Dim fs As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"))
            fs.CargarGrafoTarificacion(Nothing, Nothing)
        End If
        
        
   
        
        'Se carga la entidad emisora de pólizas
        'TODO: se debería recuperar por el NIF de AMV
        Dim mifs As New Framework.FachadaLogica.FachadaBaseFS(Nothing, Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"))
        
        Dim emi As FN.Seguros.Polizas.DN.EmisoraPolizasDN = mifs.RecuperarLista(Nothing, Nothing, GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
        Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = emi
        Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.GestionPagos.DN.EntidadFiscalGenericaPrincipal).FullName) = emi.EnidadFiscalGenerica
        
        
        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mrecurso)
            
            
            Dim bdln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colCosntatesConfigurablesSeguros As New FN.Seguros.Polizas.DN.ColConstatesConfigurablesSegurosDN
            colCosntatesConfigurablesSeguros.AddRangeObject(bdln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.ConstatesConfigurablesSegurosDN)))
            Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(FN.Seguros.Polizas.DN.ColConstatesConfigurablesSegurosDN).FullName) = colCosntatesConfigurablesSeguros
            
            
        End Using
        
    End Sub
    
</script>