Imports MotorBusquedaBasicasDN
Public Class GestorPersistenciaClienteLN
    Implements MV2DN.IGestorPersistencia


    Public Function RecuperarColInstancia(ByVal pColIHuellaEntidadDN As Framework.DatosNegocio.ColIHuellaEntidadDN) As Framework.DatosNegocio.ColIEntidadBaseDN Implements MV2DN.IGestorPersistencia.RecuperarColInstancia
        Dim MV2AS As New Framework.AS.MV2AS
        Return MV2AS.RecuperarColDNGenerico(pColIHuellaEntidadDN)


    End Function

    Public Function RecuperarColTiposCompatibles(ByVal pPropVinc As MV2DN.PropVinc) As System.Collections.Generic.IList(Of System.Type) Implements MV2DN.IGestorPersistencia.RecuperarColTiposCompatibles






        '' seleccionar el tipo a buscar en el mapeado de persistencia
        'Dim tipoaTratar As System.Type
        'If pPropVinc.EsPropiedadEncadenada Then
        '    tipoaTratar = pPropVinc.TipoRepresentado
        'Else
        '    If pPropVinc.EsColeccion Then
        '        tipoaTratar = (pPropVinc.TipoFijadoColPropiedad)
        '    Else
        '        tipoaTratar = (pPropVinc.TipoPropiedad)
        '    End If
        'End If


        '' buscar en el mapeado de persistencia
        'Dim MV2AS As New Framework.AS.MV2AS
        'Return MV2AS.RecuperarColTiposCompatibles(tipoaTratar)



    End Function

    Public Function RecuperarInstancia(ByVal pHuellaEntidadDN As Framework.DatosNegocio.HEDN) As Framework.DatosNegocio.IEntidadBaseDN Implements MV2DN.IGestorPersistencia.RecuperarInstancia
        Dim MV2AS As New Framework.AS.MV2AS
        Return MV2AS.RecuperarDNGenerico(pHuellaEntidadDN)

    End Function


    Public Function RecuperarParametroBusqueda(ByVal pIVincElemento As MV2DN.IVincElemento, ByVal pTipo As System.Type) As ParametroCargaEstructuraDN Implements MV2DN.IGestorPersistencia.RecuperarParametroBusqueda
        Return MotorBusquedaIuWinCtrl.NavegadorHelper.RecuperarParametroBusqueda(pIVincElemento, pTipo)
    End Function

    Public Function RecuperarParametroBusqueda(ByVal pIVincElemento As MV2DN.IVincElemento) As ParametroCargaEstructuraDN Implements MV2DN.IGestorPersistencia.RecuperarParametroBusqueda
        Return MotorBusquedaIuWinCtrl.NavegadorHelper.RecuperarParametroBusqueda(pIVincElemento)
    End Function


    Public Function GuardarColInstancia(ByVal pColHuellaEntidadDN As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadBaseDN)) As Framework.DatosNegocio.ColIEntidadBaseDN Implements MV2DN.IGestorPersistencia.GuardarColInstancia



    End Function

    Public Function GuardarInstancia(ByVal pIEntidadDN As Framework.DatosNegocio.IEntidadBaseDN) As Framework.DatosNegocio.IEntidadBaseDN Implements MV2DN.IGestorPersistencia.GuardarInstancia




    End Function


    Public Function RecuperarLista(ByVal pTipo As System.Type) As Framework.DatosNegocio.ColIEntidadBaseDN Implements MV2DN.IGestorPersistencia.RecuperarLista
        Dim mibaseas As New Framework.AS.DatosBasicosAS
        Dim col As New Framework.DatosNegocio.ColIEntidadBaseDN

        Dim lista As IList = mibaseas.RecuperarListaTipos(pTipo)

        For Each entidad As Framework.DatosNegocio.IEntidadBaseDN In lista
            col.Add(entidad)
        Next


        ' col.AddRange()
        Return col


    End Function

End Class
