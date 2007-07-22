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
            connectionstring = "server=localhost;database=AMVDbd;user=sa;pwd=''"
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
        
        'Asignamos el mapeado de Toyota POR al gestor de instanciación
        ' Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposAMVDocsEntrantesLN
        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.Usuarios.Test.GestorMapPersistenciaCamposUsuariosTest
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPeristenciaCampos()
       
        

        ' If colValConfigServer.Item("EnviarMail") Then
            
        'Dim despachado As New Framework.Mensajeria.GestorMails.DespachadorMails()
        'despachado.Start()
        ' End If
    
      
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
       
</script>