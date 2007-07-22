Public Class frmDocsEntrantesctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    'constructor vacío para levantar el ensamblado de manera dinámica
    Public Sub New()

    End Sub

    ''constructor sobrecargado para la base
    'Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador)
    '    MyBase.New(pNavegador)
    'End Sub

#Region "métodos"
    Public Function RecuperarArbolEntidades() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.RecuperarArbolTiposEntNegocio()
    End Function

    'Public Function RecuperarSiguienteOperacionAProcesar(ByVal pTipoOperacion As String, ByVal pIDTipoCanal As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
    '    Dim mias As New ClienteAS.ClienteAS

    '    Select Case pTipoOperacion
    '        Case Is = "nuevos documentos"
    '            Return mias.RecuperarOperacionAProcesarNuevos(pIDTipoCanal)
    '        Case Is = "ya clasificados"
    '            Return mias.RecuperarOperacionAProcesarClasificados
    '    End Select

    'End Function

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

    Public Function RecuperarOperacionEnCursoPara() As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mias As New ClienteAS.ClienteAS()
        Return mias.RecuperarOperacionEnCursoPara
    End Function
#End Region
End Class
