Imports MotorBusquedaBasicasDN
Public Class ParametrosHelper

    Public Shared Function CrearParametroCargaEstructura(ByVal emap As MV2DN.EntradaMapNavBuscadorDN, ByVal entidad As Framework.DatosNegocio.IEntidadDN) As ParametroCargaEstructuraDN

        Dim colvc As New List(Of ValorCampo)

        Dim vc As New ValorCampo
        vc.NombreCampo = emap.NombreCampo
        vc.Operador = OperadoresAritmeticos.igual
        vc.Valor = emap.RecuperarValor(entidad)
        colvc.Add(vc)

        Dim pce As ParametroCargaEstructuraDN
        pce = New ParametroCargaEstructuraDN
        pce.CargarDesdeTexto(emap.DatosBusqueda)
        pce.TipodeEntidad = emap.Tipo
        pce.Titulo = emap.NombreVis
        pce.ListaValores = colvc

        Return pce

    End Function

    Public Shared Function SustituirParamettrosExterioresPorValores(ByVal pLista As List(Of ValorCampo), ByVal pHtDatosEsternos As Hashtable) As Boolean



        Dim ht As Hashtable = pHtDatosEsternos

        For Each cv As ValorCampo In pLista

            If cv.Valor.Contains("#") Then
                ' se trata de un valor de parametro a sustituir
                Dim ruta As String = cv.Valor.Replace("#", "")
                ' se considra la clave al primer valor de la ruta separa por punto algo parecido a @Emprea.id cla clave seria Empresa
                Dim clave As String = ruta.Split(".")(0)
                Dim entidad As Framework.DatosNegocio.IEntidadBaseDN



                Dim imap As New MV2DN.InstanciaMapDN
                Dim agmap As New MV2DN.AgrupacionMapDN(imap, Nothing)
                Dim pm As New MV2DN.PropMapDN
                pm.NombreProp = ruta.Substring(clave.Length + 1)
                agmap.ColPropMap.Add(pm)



                ' recuperamos la entidad de los datos generales exteriores que pasan en una has table como claves
                If ht.ContainsKey(clave) Then
                    entidad = ht.Item(clave)
                Else
                    Throw New ApplicationException("los datos exteriores no continen la clave:" & clave)
                End If


                Dim ivinc As New MV2DN.InstanciaVinc(entidad, imap, Nothing, Nothing)
                Dim pv As MV2DN.PropVinc = ivinc.ColAgrupacionVinc(0).ColPropVinc(0)
                cv.Valor = pv.Value

            ElseIf cv.Valor.Contains("%") Then
                ' se trata de un valor de parametro a sustituir
                Dim comentario As String = cv.Valor.Replace("%", "")
                cv.Valor = InputBox(comentario)


            End If

            cv.Eliminable = False

        Next

    End Function


    Public Shared Function SustituirParamettrosPorValores(ByVal pElementoVinc As MV2DN.IVincElemento, ByVal pLista As List(Of ValorCampo), ByVal pHtDatosEsternos As Hashtable) As Boolean



        If pHtDatosEsternos IsNot Nothing AndAlso pHtDatosEsternos.Values.Count > 0 Then
            SustituirParamettrosExterioresPorValores(pLista, pHtDatosEsternos)
        End If

        For Each cv As ValorCampo In pLista


            If cv.Valor.Contains("@") Then
                ' se trata de un valor de parametro a sustituir
                Dim ruta As String = cv.Valor.Replace("@", "")

                Dim imap As New MV2DN.InstanciaMapDN
                Dim agmap As New MV2DN.AgrupacionMapDN(imap, Nothing)
                Dim pm As New MV2DN.PropMapDN
                pm.NombreProp = ruta
                agmap.ColPropMap.Add(pm)



                Dim ivinc As New MV2DN.InstanciaVinc(pElementoVinc.InstanciaVinc.DN, imap, Nothing, Nothing)
                'ivinc.DN = pElementoVinc.InstanciaVinc.DN
                Dim pv As MV2DN.PropVinc = ivinc.ColAgrupacionVinc(0).ColPropVinc(0)
                ' cv.Valor = pv.ValorObjetivo
                cv.Valor = pv.Value
                cv.Eliminable = False
            End If

        Next



    End Function




End Class
