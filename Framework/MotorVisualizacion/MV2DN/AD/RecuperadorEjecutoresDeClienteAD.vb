'Imports Framework.Procesos.ProcesosDN
'Public Class RecuperadorEjecutoresDeClienteAD
'    Implements IGestorEjecutoresDeCliente




'    Private Shared mColEjecutoresDeCliente As New ColEjecutoresDeClienteDN



'    Public Function RecuperarEjecutoresDeCliente(ByVal pNombreCleinte As String) As EjecutoresDeClienteDN Implements IGestorEjecutoresDeCliente.RecuperarEjecutoresDeCliente

'        RecuperarEjecutoresDeCliente = mColEjecutoresDeCliente.RecuperarXNombreCliente(pNombreCleinte)


'        If RecuperarEjecutoresDeCliente Is Nothing Then
'            ' pedir el ejecutor al servidor

'            Dim pro As Framework.Procesos

'            RecuperarEjecutoresDeCliente = CreacionPruebeas()
'            mColEjecutoresDeCliente.Add(RecuperarEjecutoresDeCliente)
'        End If


'    End Function

'    Public Function RecuperarMethodInfoEjecutor(ByVal pNombreCleinte As String, ByVal pVerbo As VerboDN) As System.Reflection.MethodInfo Implements IGestorEjecutoresDeCliente.RecuperarMethodInfoEjecutor


'        Dim miEjecutoresDeCliente As EjecutoresDeClienteDN = RecuperarEjecutoresDeCliente(pNombreCleinte)
'        Return miEjecutoresDeCliente.RecuperarMethodInfoEjecutor(pVerbo)


'    End Function

'    Public Function RecuperarTipoEjecutor(ByVal pNombreCleinte As String, ByVal pVerbo As VerboDN) As System.Type Implements IGestorEjecutoresDeCliente.RecuperarTipoEjecutor


'        Dim miEjecutoresDeCliente As EjecutoresDeClienteDN = RecuperarEjecutoresDeCliente(pNombreCleinte)
'        Return miEjecutoresDeCliente.RecuperarTipoEjecutor(pVerbo)


'    End Function





'    Public Function EjecutarMethodInfo(ByVal pNombreCleinte As String, ByVal pVerbo As VerboDN, ByVal pDN As Object) As Object Implements IGestorEjecutoresDeCliente.EjecutarMethodInfo
'        Dim mi As Reflection.MethodInfo = Me.RecuperarMethodInfoEjecutor(pNombreCleinte, pVerbo)
'        Dim Parametros(0) As Object
'        Parametros(0) = pDN


'        Dim tipo As System.Type = Me.RecuperarTipoEjecutor(pNombreCleinte, pVerbo)
'        Dim controlador As Object = Activator.CreateInstance(tipo)


'        Return mi.Invoke(controlador, Parametros)

'    End Function

'    Public Function IEjecutorEjecutar(ByVal pNombreCleinte As String, ByVal pVerbo As Framework.Procesos.ProcesosDN.VerboDN, ByVal pDN As Object) As Object Implements Framework.Procesos.ProcesosDN.IGestorEjecutoresDeCliente.IEjecutorEjecutar
'        Dim tipo As System.Type = Me.RecuperarTipoEjecutor(pNombreCleinte, pVerbo)
'        Dim controlador As IEjecutorOperacionLN = Activator.CreateInstance(tipo)

'        IEjecutorEjecutar = controlador.EjecutarOperacion(pDN, pVerbo)

'    End Function
'End Class
