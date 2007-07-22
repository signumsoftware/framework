Imports Framework.DatosNegocio
Imports MotorBusquedaBasicasDN
Public Interface IRecuperadorInstanciaMap

    Function RecuperarInstanciaMap(ByVal pNombreMapInstancia As String) As InstanciaMapDN
    Function RecuperarInstanciaMap(ByVal pTipo As System.Type) As InstanciaMapDN

End Interface



Public Interface IGestorPersistencia
    Function RecuperarColTiposCompatibles(ByVal pPropVinc As MV2DN.PropVinc) As IList(Of System.Type)
    Function RecuperarParametroBusqueda(ByVal pIVincElemento As MV2DN.IVincElemento) As ParametroCargaEstructuraDN
    Function RecuperarParametroBusqueda(ByVal pIVincElemento As MV2DN.IVincElemento, ByVal pTipo As System.Type) As ParametroCargaEstructuraDN

    Function RecuperarInstancia(ByVal pHuellaEntidadDN As HEDN) As IEntidadBaseDN

    Function RecuperarColInstancia(ByVal pColIHuellaEntidadDN As Framework.DatosNegocio.ColIHuellaEntidadDN) As Framework.DatosNegocio.ColIEntidadBaseDN
    Function RecuperarLista(ByVal pTipo As System.Type) As Framework.DatosNegocio.ColIEntidadBaseDN

    Function GuardarInstancia(ByVal pIEntidadBaseDN As IEntidadBaseDN) As IEntidadBaseDN
    Function GuardarColInstancia(ByVal pColHuellaEntidadDN As IList(Of IEntidadBaseDN)) As Framework.DatosNegocio.ColIEntidadBaseDN

End Interface
