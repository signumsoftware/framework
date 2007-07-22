Imports Framework.Procesos
Imports Framework.Procesos.ProcesosDN
Imports Framework.LogicaNegocios.Transacciones


Public Class GestorEjecutoresLN
    Implements IGestorEjecutoresDeCliente


    Private mColEjecutoresDeCliente As New ColEjecutoresDeClienteDN

    Public Function RecuperarEjecutoresDeCliente(ByVal pNombreCleinte As String) As EjecutoresDeClienteDN Implements IGestorEjecutoresDeCliente.RecuperarEjecutoresDeCliente

        Using tr As New Transaccion()
            RecuperarEjecutoresDeCliente = mColEjecutoresDeCliente.RecuperarXNombreCliente(pNombreCleinte)


            If RecuperarEjecutoresDeCliente Is Nothing Then
                ' pedir el ejecutor al servidor
                'RecuperarEjecutoresDeCliente = CreacionPruebeas()

                Dim opln As New OperacionesLN

                RecuperarEjecutoresDeCliente = opln.RecuperarEjecutorCliente(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String))
                mColEjecutoresDeCliente.Add(RecuperarEjecutoresDeCliente)
            End If
            tr.Confirmar()

        End Using



    End Function

    Public Function RecuperarMethodInfoEjecutor(ByVal pNombreCleinte As String, ByVal pVerbo As VerboDN) As System.Reflection.MethodInfo Implements IGestorEjecutoresDeCliente.RecuperarMethodInfoEjecutor

        Using tr As New Transaccion()
            Dim miEjecutoresDeCliente As EjecutoresDeClienteDN = RecuperarEjecutoresDeCliente(pNombreCleinte)
            RecuperarMethodInfoEjecutor = miEjecutoresDeCliente.RecuperarMethodInfoEjecutor(pVerbo)

            tr.Confirmar()

        End Using


    End Function

    Public Function RecuperarTipoEjecutor(ByVal pNombreCleinte As String, ByVal pVerbo As VerboDN) As System.Type Implements IGestorEjecutoresDeCliente.RecuperarTipoEjecutor

        Using tr As New Transaccion()

            Dim miEjecutoresDeCliente As EjecutoresDeClienteDN = RecuperarEjecutoresDeCliente(pNombreCleinte)
            RecuperarTipoEjecutor = miEjecutoresDeCliente.RecuperarTipoEjecutor(pVerbo)
            tr.Confirmar()

        End Using

    End Function


 


    Public Function EjecutarMethodInfo(ByVal pNombreCleinte As String, ByVal instanciaSolicitante As Object, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN, ByVal pDN As Object) As Object Implements IGestorEjecutoresDeCliente.EjecutarMethodInfo


        Using tr As New Transaccion


            Dim Parametros(2) As Object
            Parametros(0) = pDN
            Parametros(1) = pTransicionRealizada
            Parametros(2) = instanciaSolicitante

            Dim verbo As Framework.Procesos.ProcesosDN.VerboDN

            If pTransicionRealizada.Transicion.TipoTransicion = TipoTransicionDN.Inicio Or pTransicionRealizada.Transicion.TipoTransicion = TipoTransicionDN.InicioDesde Or pTransicionRealizada.Transicion.TipoTransicion = TipoTransicionDN.InicioObjCreado Then
                'verbo = pTransicionRealizada.OperacionRealizadaOrigen.VerboOperacion
                verbo = pTransicionRealizada.OperacionRealizadaDestino.VerboOperacion

            Else
                verbo = pTransicionRealizada.OperacionRealizadaDestino.VerboOperacion
            End If

            Dim mi As Reflection.MethodInfo = Me.RecuperarMethodInfoEjecutor(pNombreCleinte, verbo)
            Dim tipo As System.Type = Me.RecuperarTipoEjecutor(pNombreCleinte, verbo)
            Dim controlador As Object = Activator.CreateInstance(tipo)
            EjecutarMethodInfo = mi.Invoke(controlador, Parametros)
            tr.Confirmar()


        End Using
    End Function


    'BaseTransaccionConcretaLN

    Public Sub GuardarGenerico(ByVal objeto As Object, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN, ByVal instanciaContenedora As Object)

        Using tr As New Transaccion()

            Dim btc As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            btc.GuardarGenerico(objeto)
            '   btc.GuardarGenerico(pTransicionRealizada)

            tr.Confirmar()
        End Using

    End Sub

    ''' <summary>
    ''' Este método, impide que se modifique el objeto de a operación realizada, recuperando el original
    ''' de la base de datos
    ''' </summary>
    ''' <param name="objeto"></param>
    ''' <param name="pTransicionRealizada"></param>
    ''' <param name="instanciaContenedora"></param>
    ''' <remarks></remarks>
    Public Sub NoGuardarGenerico(ByVal objeto As Object, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN, ByVal instanciaContenedora As Object)
        Using tr As New Transaccion()

            Dim objEnt As Framework.DatosNegocio.IEntidadDN
            Dim objEntBD As Framework.DatosNegocio.IEntidadDN

            objEnt = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion

            If objEnt.Estado <> DatosNegocio.EstadoDatosDN.SinModificar Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se puede modificar la entidad")
            End If

            Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            objEntBD = gi.Recuperar(objEnt.ID, CType(objEnt, Object).GetType())

            If objEntBD.FechaModificacion <> objEnt.FechaModificacion Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se puede realizar la operación, error de concurrencia con la entidad")
            End If

            tr.Confirmar()

        End Using
    End Sub

    Public Function IEjecutorEjecutar(ByVal pNombreCleinte As String, ByVal pOperacion As ProcesosDN.OperacionDN, ByVal pDN As Object) As Object Implements ProcesosDN.IGestorEjecutoresDeCliente.IEjecutorEjecutar
        'Using tr As New Transaccion()

        '    Dim tipo As System.Type = Me.RecuperarTipoEjecutor(pNombreCleinte, pVerbo)
        '    Dim controlador As IEjecutorOperacionLN = Activator.CreateInstance(tipo)

        '    IEjecutorEjecutar = controlador.EjecutarOperacion(pDN, pVerbo)

        'End Using
        Throw New NotImplementedException
    End Function

    Public Function EjecutarVinculoMetodo(ByVal pContenedor As Object, ByVal pObjetoDatos As Object, ByVal pVinculoMetodo As TiposYReflexion.DN.VinculoMetodoDN) As Object Implements ProcesosDN.IGestorEjecutoresDeCliente.EjecutarVinculoMetodo
        Throw New NotImplementedException
    End Function

End Class