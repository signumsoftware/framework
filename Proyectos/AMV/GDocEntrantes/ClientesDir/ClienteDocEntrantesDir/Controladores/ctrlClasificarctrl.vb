Public Class ctrlClasificarctrl
    Inherits MotorIU.ControlesP.ControladorControlBase

    Public Sub New(ByVal pMotor As MotorIU.Motor.INavegador, ByVal pControl As MotorIU.ControlesP.IControlP)
        MyBase.New(pMotor, pControl)
    End Sub

#Region "métodos"
    Public Function RecuperarArbolEntidades() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim mias As New ClienteAS.ClienteAS
        Dim operador As AmvDocumentosDN.OperadorDN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = Me.Marco.DatosMarco("Principal")
        operador = principal.UsuarioDN.HuellaEntidadUserDN.EntidadReferida

        Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        cabecera = mias.RecuperarArbolTiposEntNegocio()
        'If Not operador Is Nothing Then
        '    cabecera.NodoTipoEntNegoio.PodarNodosHijosNoContenedoresHojas(operador.ColTipoEntNegoio, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        'End If
        Return cabecera
    End Function

    Public Function RecuperarSiguienteOperacionAProcesar(ByVal pTipoOperacion As String, ByVal pIDTipoCanal As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.RecuperarOperacionAProcesarNuevos(pIDTipoCanal)
        'Select Case pTipoOperacion
        '    Case Is = "nuevos documentos"
        '        Return mias.RecuperarOperacionAProcesarNuevos(pIDTipoCanal)
        '    Case Is = "ya clasificados"
        '        Throw (New Exception("No se ha implementado la recuperación de Operaciones ya clasificadas desde aquí"))
        'End Select

    End Function

    Public Function RecuperarSiguienteOperacionAProcesarPostClasificados(ByVal pTipoEntNegocio As AmvDocumentosDN.TipoEntNegoioDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS

        Return mias.RecuperarOperacionAProcesarClasificados(pTipoEntNegocio)

    End Function

    'Public Function GuardarOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
    '    Dim mias As New ClienteAS.ClienteAS
    '    Return mias.GuardarOperacion(pOperacion)
    'End Function

    Public Function AnularOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.Anular(pOperacion)
    End Function

    Public Function RechazarOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.Rechazar(pOperacion)
    End Function

    Public Function IncidentarOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.Incidentar(pOperacion)
    End Function

    Public Function ClasificarOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN, ByVal pColEntidades As AmvDocumentosDN.ColEntNegocioDN) As AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.Clasificar(pOperacion, pColEntidades)
    End Function

    Public Function ClasificarYCerraOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.ClasificarYCerrar(pOperacion)
    End Function

    Public Function RecuperarColCanales() As AmvDocumentosDN.ColCanalEntradaDocsDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.RecuperarCanales()
    End Function

    Public Function BalizaNumCanalesTipoEntNeg() As Data.DataSet
        Dim mias As New ClienteAS.ClienteAS()
        Return mias.BalizaNumCanalesTipoEntNeg()
    End Function

    Public Function RecuperarColTipoCanales() As AmvDocumentosDN.ColTipoCanalDN
        Dim mias As New ClienteAS.ClienteAS()
        Return mias.RecuperarColTipoCanales
    End Function


    Public Function RecuperarColTipoFichero() As Framework.Ficheros.FicherosDN.ColTipoFicheroDN
        Dim mias As New ClienteAS.ClienteAS()
        Return mias.RecuperarColTipoFichero
    End Function





#End Region
End Class
