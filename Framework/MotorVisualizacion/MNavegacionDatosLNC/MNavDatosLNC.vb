Imports MNavegacionDatosDN
Imports Framework.LogicaNegocios.Transacciones

Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN

Public Class MNavDatosLNC



    Public Function RecuperarColHuellas(ByVal pRelEntNavVinc As MNavegacionDatosDN.RelEntNavVincDN) As Framework.DatosNegocio.ColIHuellaEntidadDN


        'afsadfbsdgbsgfdb

        'Try

        '    If pRelEntNavVinc.DireccionLectura = DireccionesLectura.Reversa Then

        '        Dim pr As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pRelEntNavVinc.PropVinc.PropertyInfoVinc, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
        '        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        '        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
        '        RecuperarColHuellas = gi.RecuperarColHuellasRelInversa(pr, pRelEntNavVinc.RelacionEntidadesNav.TipoDestino)



        '    Else

        '        Dim pr As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pRelEntNavVinc.PropVinc.PropertyInfoVinc, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
        '        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        '        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
        '        RecuperarColHuellas = gi.RecuperarColHuellasRelDirecta(pr, pRelEntNavVinc.RelacionEntidadesNav.TipoDestino)




        '    End If




        '    tlproc.Confirmar()
        'Catch ex As Exception
        '    tlproc.Cancelar()
        '    Throw
        'End Try



    End Function




  


    Public Function RecuperarEntidadNavDNoNueva(ByVal pTipo As System.Type) As EntidadNavDN

        RecuperarEntidadNavDNoNueva = RecuperarEntidadNavDN(pTipo)


        If RecuperarEntidadNavDNoNueva Is Nothing Then

            ' recuperar entidad vinculoa clase para el tipo

            RecuperarEntidadNavDNoNueva = New MNavegacionDatosDN.EntidadNavDN(pTipo)
        End If

    End Function

    Public Function RecuperarEntidadNavDN(ByVal pTipo As System.Type) As EntidadNavDN
        ' todo ad que recupere por el tipo desde la bd


        'fgrfgerwfgwserg()

        Dim mias As New MNavegacionDatosAS.MNavDatosAS

        Return mias.RecuperarEntidadNavDN(pTipo)





        'Dim lista As List(Of EntidadNavDN)
        'lista = Me.RecuperarListaCondicional(Of EntidadNavDN)(New BuscadorEntidadNavAD(pTipo))

        'If lista Is Nothing Then
        '    Return Nothing

        'Else
        '    Select Case lista.Count

        '        Case Is = 0
        '            Return Nothing
        '        Case Is = 1
        '            Return lista(0)
        '        Case Else
        '            Throw New ApplicationException("Error de intefridad en la bd RecuperarEntidadNav recuperó:" & lista.Count)
        '    End Select


        'End If




    End Function


    Public Function RecuperarRelaciones(ByVal pTipo As System.Type) As ColRelacionEntidadesNavDN


        ' ddddddddd()


        Dim mias As New MNavegacionDatosAS.MNavDatosAS

        Return mias.RecuperarRelaciones(pTipo)






        'Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        'Try

        '    Dim col As New ColRelacionEntidadesDN
        '    Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, mRec)

        '    col.AddRange(Me.RecuperarListaCondicional(Of MNavegacionDatosDN.RelacionEntidadesNavDN)(New BuscadorRelacionesAEntidadNavAD(pTipo)))

        '    RecuperarRelaciones = col

        '    tlproc.Confirmar()
        'Catch ex As Exception
        '    tlproc.Cancelar()
        '    Throw
        'End Try






    End Function


    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="pTipo"></param>
    ''' <param name="pIRecuperadorInstanciaMap">debe ser un recuperador de mapeados que filtre por el usuario</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarEntNavVincDN(ByVal pTipo As System.Type, ByVal pIRecuperadorInstanciaMap As MV2DN.IRecuperadorInstanciaMap) As EntNavVincDN

        ' de alguna manera se debieran asociar los mapeados de visualizacion a el rol y recuperar el map de visisvilidad para los roles del principal




        'Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        Try





            '1º recupear las relaciones
            Dim colRealaciones As ColRelacionEntidadesNavDN
            colRealaciones = Me.RecuperarRelaciones(pTipo)



            ' recuperamos el mapeado de la intancia principa para hacer la interseccion con sus relaciones y no mostrar las relaciones oculatadas en el mapeado de visivilidad


            Dim miInstanciaVinc As New MV2DN.InstanciaVinc(pTipo, pIRecuperadorInstanciaMap.RecuperarInstanciaMap(pTipo), pIRecuperadorInstanciaMap, Nothing)
            Dim entidadVinc As New EntNavVincDN(miInstanciaVinc)
            ' 2º para cada tipo de la relacion recuperar su mapeado de visualizacion

            For Each re As RelacionEntidadesNavDN In colRealaciones



                If re.DireccionDeLectura(pTipo) = DireccionesLectura.Directa Then
                    ' visivilidad directa
                    ' NO recuperamos la miInstanciaVincReversa porque el tipo simpre debe ser elmismo (re.EntidadDatosOrigen.VinculoClase.TipoClase )
                    VincularNuevaRelacion(re, miInstanciaVinc, entidadVinc, DireccionesLectura.Directa)

                Else
                    ' visibilidad reversa

                    ' recuperamos la miInstanciaVincReversa porque el tipo cambiara (re.EntidadDatosOrigen.VinculoClase.TipoClase)
                    Dim tipoReverso As System.Type = re.EntidadDatosOrigen.VinculoClase.TipoClase
                    Dim miInstanciaVincReversa As New MV2DN.InstanciaVinc(tipoReverso, pIRecuperadorInstanciaMap.RecuperarInstanciaMap(tipoReverso), pIRecuperadorInstanciaMap, Nothing)
                    VincularNuevaRelacion(re, miInstanciaVincReversa, entidadVinc, DireccionesLectura.Reversa)



                End If

            Next



            RecuperarEntNavVincDN = entidadVinc



            ' tlproc.Confirmar()
        Catch ex As Exception
            '  tlproc.Cancelar()
            Throw
        End Try





    End Function

    Public Function RecuperarEntNavVincDN(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN, ByVal pIRecuperadorInstanciaMap As MV2DN.IRecuperadorInstanciaMap) As EntNavVincDN
        Dim o As Object = pEntidad
        RecuperarEntNavVincDN = RecuperarEntNavVincDN(o.GetType, pIRecuperadorInstanciaMap)
        RecuperarEntNavVincDN.InstanciaVinc.DN = pEntidad
        RecuperarEntNavVincDN.Actualizar() ' este metodo podria estar como resultado de el evento de la liena anterion dentro de la calse EntNavVincDN

    End Function

    Private Sub VincularNuevaRelacion(ByVal pRelacionEntidadesNavDN As RelacionEntidadesNavDN, ByVal pInstanciaVinc As MV2DN.InstanciaVinc, ByVal pEntidadVinc As EntNavVincDN, ByVal pDireccionLectura As DireccionesLectura)

        Dim miPropVinc As MV2DN.PropVinc
        Dim mRelacionvinc As RelEntNavVincDN

        If pDireccionLectura = DireccionesLectura.Directa Then
            miPropVinc = pInstanciaVinc.ColPropVincTotal.RecuperarxNombreProp(pRelacionEntidadesNavDN.NombrePropiedad)
            If miPropVinc IsNot Nothing Then
                mRelacionvinc = New RelEntNavVincDN(pRelacionEntidadesNavDN, miPropVinc, pDireccionLectura)
                pEntidadVinc.ColREentNavVincDN.Add(mRelacionvinc)
            End If

        Else
            miPropVinc = pInstanciaVinc.ColPropVincTotal.RecuperarxNombreProp(pRelacionEntidadesNavDN.NombrePropiedad)
            If miPropVinc IsNot Nothing Then
                mRelacionvinc = New RelEntNavVincDN(pRelacionEntidadesNavDN, miPropVinc, pDireccionLectura)
                pEntidadVinc.ColREentNavVincDN.Add(mRelacionvinc)
            End If

        End If


    End Sub




End Class

Public Enum DireccionDeRecuperacion
    Origen
    Destino
End Enum


