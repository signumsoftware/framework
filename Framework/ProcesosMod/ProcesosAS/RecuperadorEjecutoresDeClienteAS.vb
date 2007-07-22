Imports Framework.Procesos.ProcesosDN

Public Class RecuperadorEjecutoresDeClienteAS
    Implements IGestorEjecutoresDeCliente



    Private Shared mColEjecutoresDeCliente As New ColEjecutoresDeClienteDN



    Public Function RecuperarEjecutoresDeCliente(ByVal pNombreCleinte As String) As EjecutoresDeClienteDN Implements IGestorEjecutoresDeCliente.RecuperarEjecutoresDeCliente

        RecuperarEjecutoresDeCliente = mColEjecutoresDeCliente.RecuperarXNombreCliente(pNombreCleinte)


        If RecuperarEjecutoresDeCliente Is Nothing Then
            ' pedir el ejecutor al servidor
            'RecuperarEjecutoresDeCliente = CreacionPruebeas()

            Dim opas As New OperacionesAS
            RecuperarEjecutoresDeCliente = opas.RecuperarEjecutorCliente(pNombreCleinte)
            mColEjecutoresDeCliente.Add(RecuperarEjecutoresDeCliente)
        End If


    End Function

    Public Function RecuperarMethodInfoEjecutor(ByVal pNombreCleinte As String, ByVal pVerbo As VerboDN) As System.Reflection.MethodInfo Implements IGestorEjecutoresDeCliente.RecuperarMethodInfoEjecutor


        Dim miEjecutoresDeCliente As EjecutoresDeClienteDN = RecuperarEjecutoresDeCliente(pNombreCleinte)
        If Not miEjecutoresDeCliente Is Nothing Then
            Return miEjecutoresDeCliente.RecuperarMethodInfoEjecutor(pVerbo)
        Else
            Return Nothing
        End If


    End Function

    Public Function RecuperarTipoEjecutor(ByVal pNombreCleinte As String, ByVal pVerbo As VerboDN) As System.Type Implements IGestorEjecutoresDeCliente.RecuperarTipoEjecutor


        Dim miEjecutoresDeCliente As EjecutoresDeClienteDN = RecuperarEjecutoresDeCliente(pNombreCleinte)
        Return miEjecutoresDeCliente.RecuperarTipoEjecutor(pVerbo)


    End Function






    Public Function EjecutarMethodInfo(ByVal pNombreCleinte As String, ByVal pParametrosAplicables As Object, ByVal pTR As ProcesosDN.TransicionRealizadaDN, ByVal pDN As Object) As Object Implements IGestorEjecutoresDeCliente.EjecutarMethodInfo
        Dim mi As Reflection.MethodInfo = Me.RecuperarMethodInfoEjecutor(pNombreCleinte, pTR.Transicion.OperacionDestino.VerboOperacion)
        Dim Parametros(2) As Object
        Parametros(0) = pDN
        Parametros(1) = pTR
        Parametros(2) = pParametrosAplicables


        Dim tipo As System.Type = Me.RecuperarTipoEjecutor(pNombreCleinte, pTR.Transicion.OperacionDestino.VerboOperacion)
        Dim controlador As Object = Activator.CreateInstance(tipo)

        Try
            Return mi.Invoke(controlador, Parametros)

        Catch ex As Exception
            If ex.InnerException IsNot Nothing Then
                Throw ex.InnerException
            Else
                Throw
            End If
        End Try

    End Function


    Public Function IEjecutorEjecutar(ByVal pNombreCleinte As String, ByVal pOperacion As ProcesosDN.OperacionDN, ByVal pDN As Object) As Object Implements ProcesosDN.IGestorEjecutoresDeCliente.IEjecutorEjecutar

        'Dim tipo As System.Type = Me.RecuperarTipoEjecutor(pNombreCleinte, pVerbo)
        'Dim controlador As IEjecutorOperacionLN = Activator.CreateInstance(tipo)

        'IEjecutorEjecutar = controlador.EjecutarOperacion(pDN, pVerbo)


    End Function




    Public Function EjecutarVinculoMetodo(ByVal pContenedor As Object, ByVal pObjetoDatos As Object, ByVal pVinculoMetodo As TiposYReflexion.DN.VinculoMetodoDN) As Object Implements ProcesosDN.IGestorEjecutoresDeCliente.EjecutarVinculoMetodo

        Dim Parametros(2) As Object
        Parametros(0) = pContenedor
        Parametros(1) = pObjetoDatos
        Parametros(2) = pVinculoMetodo
        'Dim Parametros(0) As Object
        'Parametros(0) = New DatosEventArg(pContenedor, pObjetoDatos)

        Dim controlador As Object = Activator.CreateInstance(pVinculoMetodo.VinculoClase.TipoClase)
        Dim mi As Reflection.MethodInfo = pVinculoMetodo.RecuperarMethodInfo

        Return mi.Invoke(controlador, Parametros)


    End Function

End Class




'Public Class DatosEventArg
'    Inherits System.EventArgs
'    Public Sub New()

'    End Sub

'    Public Sub New(ByVal pContenedor As Object, ByVal pEntidad As Object)
'        Contenedor = pContenedor
'        Entidad = pEntidad
'    End Sub





'    Public Contenedor As Object
'    Public Entidad As Object

'End Class