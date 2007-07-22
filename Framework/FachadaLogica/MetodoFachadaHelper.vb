Imports Framework.Usuarios.DN

Public Class MetodoFachadaHelper
    Dim mIdentificadorMetodo As Guid

    ''Protected mRec As Framework.LogicaNegocios.Transacciones.IRecursoLN

    Public Sub New()
        mIdentificadorMetodo = New Guid
    End Sub

    ''' <summary>
    ''' traza la entrada a un metodo
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub EntradaMetodo(ByVal pIdentificadorSesion As String, ByVal pActor As PrincipalDN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)

        If TrazarEntradas Then
            Dim tms As TrazaMetodoSistemaDN
            tms = New TrazaMetodoSistemaDN(pActor, GetNombreMetodoLlamante, "", "", Now, pIdentificadorSesion)

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, pRec)
            gi.Guardar(tms)
        End If



    End Sub

    ''' <summary>
    ''' Traza la salida a un metodo
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub SalidaMetodo(ByVal pIdentificadorSesión As String, ByVal pActor As PrincipalDN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)

        If TrazarSalidas Then
            Dim tms As TrazaMetodoSistemaDN
            tms = New TrazaMetodoSistemaDN(pActor, GetNombreMetodoLlamante, "", "", Now, pIdentificadorSesión)

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, pRec)
            gi.Guardar(tms)
        End If


    End Sub

    Public Sub SalidaMetodoExcepcional(ByVal pIdentificadorSesión As String, ByVal pActor As PrincipalDN, ByVal pEx As Exception, ByVal pMensaje As String, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)

        If TrazarExcepciones Then
            Dim tms As TrazaMetodoSistemaDN
            tms = New TrazaMetodoSistemaDN(pActor, GetNombreMetodoLlamante, pEx.Message, pEx.ToString, Now, pIdentificadorSesión)

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, pRec)
            gi.Guardar(tms)
        End If

        Try
            TrazarError(pIdentificadorSesión, pActor, pEx, pMensaje)
        Catch ex As Exception

        End Try

    End Sub
    Private ReadOnly Property TrazarExcepciones() As Boolean
        Get
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("TrazarExcepciones") = "true" Then
                Return True
            End If
        End Get
    End Property
    Private ReadOnly Property TrazarEntradas() As Boolean
        Get
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("TrazarEntradas") = "true" Then
                Return True
            End If
        End Get
    End Property
    Private ReadOnly Property TrazarSalidas() As Boolean
        Get
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("TrazarSalidas") = "true" Then
                Return True
            End If
        End Get
    End Property
    Public Function GetNombreMetodoLlamante() As String
        Dim metodo As Reflection.MethodInfo
        Dim st As StackTrace
        Dim sf As StackFrame

        'Encontrar el metodo llamante de fachada
        st = New StackTrace()
        sf = st.GetFrame(2)
        metodo = sf.GetMethod()
        Return metodo.ReflectedType.Name + "." + metodo.Name
    End Function


    Public Sub TrazarError(ByVal pIdentificadorSesión As String, ByVal pActor As PrincipalDN, ByVal pEx As Exception, ByVal pMensaje As String)
        Try
            Dim entrada As New SalidaExcepcionalEntradaLog
            If pActor IsNot Nothing Then
                entrada.mActor = pActor.UsuarioDN.Name
            End If
            If pIdentificadorSesión Is Nothing Then
                pIdentificadorSesión = ""
            End If
            If pMensaje Is Nothing Then
                pMensaje = ""
            End If

            entrada.mComentario = "id sesion:" & pIdentificadorSesión & pMensaje
            entrada.mMensajeError = pEx.Message


            'Dim strAppDir As String = IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly.GetModule(0).FullyQualifiedName)

            Dim strAppDir As String
            strAppDir = Configuracion.AppConfiguracion.DatosConfig.Item("LogFachada")
            Dim filest As IO.FileStream
            filest = New IO.FileStream(strAppDir & System.Guid.NewGuid.ToString() & ".xml", IO.FileMode.Create)


            Dim xmlf As System.Xml.Serialization.XmlSerializer
            xmlf = New System.Xml.Serialization.XmlSerializer(GetType(SalidaExcepcionalEntradaLog))
            xmlf.Serialize(filest, entrada)
            filest.Dispose()
        Catch ex As Exception
            Throw
        End Try

    End Sub

End Class

<Serializable()> _
Public Class SalidaExcepcionalEntradaLog
    Public mActor As String
    Public mFecha As Date = Date.Now
    Public mComentario As String
    Public mMensajeError As String

End Class