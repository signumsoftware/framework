Imports Framework.FachadaLogica
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN
Imports Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN
Imports Framework.GestorInformes.AdaptadorInformesQueryBuilding.LN


Public Class AdaptadorInformesQueryBuildingFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

    Public Function GenerarEsquemaXMLEnArchivo_Archivo(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As Byte()
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º invocamos el método del ln
                Dim miln As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.LN.AdaptadorInformesQueryBuildingLN()
                GenerarEsquemaXMLEnArchivo_Archivo = miln.GenerarEsquemaXMLEnPlantilla_Archivo(AdaptadorIQB)

                '3º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try
        End Using
    End Function


    Public Function GenerarInforme_Archivo(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As Byte()
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º invocamos el método del ln
                Dim miln As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.LN.AdaptadorInformesQueryBuildingLN()
                GenerarInforme_Archivo = miln.GenerarInforme_Archivo(AdaptadorIQB)

                '3º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try
        End Using
    End Function

    Public Function GenerarEsquemaXML(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As Xml.XmlDocument
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º invocamos el método del ln
                Dim miln As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.LN.AdaptadorInformesQueryBuildingLN()
                GenerarEsquemaXML = miln.GenerarEsquemaXML(AdaptadorIQB)

                '3º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try
        End Using
    End Function



End Class

