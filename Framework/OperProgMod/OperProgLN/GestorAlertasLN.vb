Imports Framework.LogicaNegocios.Transacciones
''' <summary>
'''  el objetivo de esta clase es recuperar las operaciones que deberan ser ejecutadas en una fecha dada y proceder a su ejecución
''' para ello debe recuperar las entidades sobre las que se ejecutará la operacion y contruir una operacion realizada para ejecutar la aoperacion
''' es posible que la oepración no pueda ejecutarse dado el estado de la dn y del flujo donde se encuentre.
''' </summary>
''' <remarks></remarks>
Public Class GestorAlertasLN

    Protected Shared mColAlertasaProcesar As New Framework.OperProg.OperProgDN.ColAlertaDN
    Protected midSesion As String
    Protected mActor As Usuarios.DN.PrincipalDN





    ' metodo encargado de sisncronizar la coleccion de alertas pendientes con el estado en base de datos
    Public Function RefrescarAlertas() As Framework.OperProg.OperProgDN.ColAlertaDN




    End Function

    Public Function EjecutarAlertasPendientes() As DataSet


        ' aquellas operaciones culla fecha de ejecución diste de ahora un intervalo dado

        Dim dts As New DataSet
        For Each alerta As Framework.OperProg.OperProgDN.AlertaDN In Me.mColAlertasaProcesar
            EjecutarAlerta(midSesion, mActor, alerta, dts, Nothing)
        Next




    End Function



    ''' <summary>
    '''	++Objetivo
    '''
    ''' ++Precondiciones
    '''   
    ''' ++Postcondiciones
    '''			   
    ''' ++ Notas
    '''	si el array list de entidades es mayor que uno se ejecutará la operacion para tidas las huellas a entidad referidas por la alerta que sean aceptadas por la operacion
    ''' </summary>
    ''' <param name="pidSesion"></param>
    ''' <param name="pactor"></param>
    ''' <param name="pAlerta"></param>
    ''' <param name="pDts"></param>
    ''' <remarks></remarks>
    Public Sub EjecutarAlerta(ByVal pidSesion As String, ByVal pactor As Usuarios.DN.PrincipalDN, ByVal pAlerta As Framework.OperProg.OperProgDN.AlertaDN, ByVal pDts As DataSet, ByVal pParametros As Object)


        Using tr0 As New Transaccion




            For Each he As Framework.DatosNegocio.HEDN In pAlerta.ColIHEntidad

                Using tr As New Transaccion(Not pAlerta.DebenEjecutaEnMismaTransaccion)
                    Try

                        Dim fs As New Framework.Procesos.ProcesosFS.OperacionesFS(Transaccion.Actual, Transaccion.Actual.TransacionesRecurso)
                        'Dim coltransicionRealizada As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN
                        'coltransicionRealizada = fs.RecuperarTransicionesAutorizadasSobre(pidSesion, pactor, he)
                        Dim opr As Framework.Procesos.ProcesosDN.OperacionRealizadaDN
                        opr = New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()
                        opr.Operacion = pAlerta.Operacion
                        fs.EjecutarOperacion(pidSesion, pactor, he, opr, pParametros)
                        tr.Confirmar()

                    Catch ex As Exception

                        If pAlerta.DebenEjecutarSeTodas Then
                            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("no se pudiereon ejecutar todas las operaciones para la alerta: " & pAlerta.GUID & " - " & pAlerta.ToStringEntidad & " entidad fallada " & he.GUIDReferida & " - " & he.ToStringEntidadReferida)
                        End If

                        tr.Cancelar()
                    End Try
                End Using

            Next



            tr0.Confirmar()

        End Using
















    End Sub


End Class
