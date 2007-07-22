Public Interface IGestorEjecutoresDeCliente
    Function RecuperarEjecutoresDeCliente(ByVal pNombreRolAplicacion As String) As EjecutoresDeClienteDN
    Function RecuperarTipoEjecutor(ByVal pNombreRolAplicacion As String, ByVal pVerbo As VerboDN) As Type
    Function RecuperarMethodInfoEjecutor(ByVal pNombreRolAplicacion As String, ByVal pVerbo As VerboDN) As Reflection.MethodInfo
    Function EjecutarMethodInfo(ByVal pNombreRolAplicacion As String, ByVal ComponenteSolicitante As Object, ByVal pTransicionRealizadaDN As ProcesosDN.TransicionRealizadaDN, ByVal pDN As Object) As Object
    ''' <summary>
    ''' </summary>
    ''' <param name="pNombreRolAplicacion">
    '''     
    ''' es el nombre del ROL de la máquina dentro del grafo de operaciones
    ''' las oepraciones son realizadas por roles en concreto.
    ''' lo normal es que esita un rol cliente y un rol servidor dentro del sistema
    ''' pero es posible que hallan servidores especializados donde se deban realizar algunos pasos del grafo
    ''' en este caso tendrán otro nombre
    ''' l valor debira ser recuperado del web config
    ''' 
    ''' </param>
    ''' <param name="pOperacion"></param>
    ''' <param name="pDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function IEjecutorEjecutar(ByVal pNombreRolAplicacion As String, ByVal pOperacion As ProcesosDN.OperacionDN, ByVal pDN As Object) As Object
    Function EjecutarVinculoMetodo(ByVal pContenedor As Object, ByVal pObjetoDatos As Object, ByVal pVinculoMetodo As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Object

End Interface
