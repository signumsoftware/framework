#Region "Importaciones"

Imports Framework.LogicaNegocios
Imports Framework.TiposYReflexion.DN

#End Region

Namespace LN
    Public Interface IBaseMotorLN

#Region "Metodos"
        Function Guardar(ByVal pObjeto As Object) As OperacionGuardarLN
        Function Baja(ByVal pObjeto As Object) As Object
        Function Recuperar(ByVal pID As String, ByVal pTipo As System.Type, ByVal pCampoContenedor As InfoTypeInstCampoRefDN) As Object
        Function Recuperar(ByVal pColID As IList, ByVal pTipo As System.Type, ByVal pCampoContenedor As InfoTypeInstCampoRefDN) As IList
#End Region

    End Interface
End Namespace
