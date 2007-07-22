

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.ClaseBaseLN
Imports MotorBusquedaBasicasDN




Public Class GestorBusquedaLN
    ' Inherits BaseGenericLN

#Region "Constructor"
    'Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
    '    MyBase.new(pTL, pRec)
    'End Sub

#End Region



    Public Function RecuperarTiposQueImplementan(ByVal pNombreCompletoCase As String, ByVal nombrePropiedad As String) As Framework.TiposYReflexion.DN.ColVinculoClaseDN

        Dim tipo As System.Type
        Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(pNombreCompletoCase, Nothing, tipo)
        Dim propiedad As System.Reflection.PropertyInfo = tipo.GetProperty(nombrePropiedad)

        Return Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposLN.TiposQueImplementanInterface(propiedad)
    End Function





    Public Function RecuperarEstructuraVista(ByVal pParametroCargaEstructura As ParametroCargaEstructuraDN) As MotorBusquedaDN.EstructuraVistaDN
        If String.IsNullOrEmpty(pParametroCargaEstructura.ConsultaSQL) Then
            Return RecuperarEstructuraVista(pParametroCargaEstructura.NombreVistaSel, pParametroCargaEstructura.CamposaCargarDatos)

        Else
            Return RecuperarEstructuraVista(pParametroCargaEstructura.ConsultaSQL, pParametroCargaEstructura.CamposaCargarDatos)

        End If


    End Function


    Public Function RecuperarEstructuraVista(ByVal nombreVista As String, ByVal CaposDeSeleccion As List(Of String)) As MotorBusquedaDN.EstructuraVistaDN



        'Dim tlproc As ITransaccionLogicaLN = Nothing
        'Dim mbAD As MotorBusquedaAD.GestorFiltroAD


        'Try
        '    tlproc = Me.ObtenerTransaccionDeProceso()
        '    mbAD = New MotorBusquedaAD.GestorFiltroAD(tlproc, Me.mRec)
        '    RecuperarEstructuraVista = mbAD.RecuperarEstructuraVista(nombreVista, CaposDeSeleccion)
        '    tlproc.Confirmar()

        'Catch e As Exception
        '    If (Not (tlproc Is Nothing)) Then
        '        tlproc.Cancelar()
        '    End If
        '    Throw e
        'End Try






        Using tr As New Transaccion


            Dim mbAD As MotorBusquedaAD.GestorFiltroAD

            mbAD = New MotorBusquedaAD.GestorFiltroAD(Transaccion.Actual, Recurso.Actual)
            RecuperarEstructuraVista = mbAD.RecuperarEstructuraVista(nombreVista, CaposDeSeleccion)
            tr.Confirmar()

        End Using




    End Function




    Public Function RecuperarDatos(ByVal pFiltro As MotorBusquedaDN.FiltroDN) As DataSet



        'Dim tlproc As ITransaccionLogicaLN = Nothing
        'Dim mbAD As MotorBusquedaAD.GestorFiltroAD


        'Try
        '    tlproc = Me.ObtenerTransaccionDeProceso()
        '    mbAD = New MotorBusquedaAD.GestorFiltroAD(tlproc, Me.mRec)
        '    RecuperarDatos = mbAD.RecuperarDatos(pFiltro)
        '    tlproc.Confirmar()

        'Catch e As Exception
        '    If (Not (tlproc Is Nothing)) Then
        '        tlproc.Cancelar()
        '    End If
        '    Throw e
        'End Try





        Using tr As New Transaccion

            Dim mbAD As MotorBusquedaAD.GestorFiltroAD


            mbAD = New MotorBusquedaAD.GestorFiltroAD(Transaccion.Actual, Recurso.Actual)
            RecuperarDatos = mbAD.RecuperarDatos(pFiltro)


            tr.Confirmar()

        End Using





    End Function


End Class
