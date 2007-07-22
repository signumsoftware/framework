Imports MNavegacionDatosDN
Imports Framework.LogicaNegocios.Transacciones

Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN

Public Class MNavDatosLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
        mTL = pTL
        mRec = pRec
    End Sub


    Public Function RecuperarColHuellas(ByVal pRelEntNavVinc As MNavegacionDatosDN.RelEntNavVincDN) As Framework.DatosNegocio.ColIHuellaEntidadDN


        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        Try

            If pRelEntNavVinc.DireccionLectura = DireccionesLectura.Reversa Then

                Dim pr As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pRelEntNavVinc.PropVinc.PropertyInfoVinc, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
                RecuperarColHuellas = gi.RecuperarColHuellasRelInversa(pr, pRelEntNavVinc.RelacionEntidadesNav.TipoDestino)



            Else

                Dim pr As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pRelEntNavVinc.PropVinc.PropertyInfoVinc, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
                RecuperarColHuellas = gi.RecuperarColHuellasRelDirecta(pr, pRelEntNavVinc.RelacionEntidadesNav.TipoDestino)




            End If




            tlproc.Confirmar()
        Catch ex As Exception
            tlproc.Cancelar()
            Throw
        End Try



    End Function


    Public Sub RegistrarEnsamblado(ByVal pEnsamblado As Reflection.Assembly)


        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        Try


            Dim listaTipos As New List(Of Type)
            listaTipos.AddRange(pEnsamblado.GetTypes)
            RegistrarTipos(listaTipos)



            tlproc.Confirmar()
        Catch ex As Exception
            tlproc.Cancelar()
            Throw
        End Try




    End Sub

    Public Sub RegistrarTipos(ByVal pTipos As List(Of Type))

        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        Try

            For Each pTipo As Type In pTipos

                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadDN)) Then
                    RegistrarTipo(pTipo)
                End If

            Next

            tlproc.Confirmar()
        Catch ex As Exception
            tlproc.Cancelar()
            Throw
        End Try

    End Sub


    Public Sub RegistrarTipo(ByVal pTipo As Type)

        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        Try

            Debug.WriteLine(pTipo.FullName)


            Dim entidadNavOrigen As MNavegacionDatosDN.EntidadNavDN = RecuperarEntidadNavDNoNueva(pTipo)


            For Each prop As Reflection.PropertyInfo In pTipo.GetProperties

                Debug.WriteLine(prop.Name)

                Dim tipoDestino As System.Type = prop.PropertyType
                If (Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.Implementa(tipoDestino, GetType(Framework.DatosNegocio.IEntidadDN)) OrElse tipoDestino.IsInterface) AndAlso Not tipoDestino Is GetType(Framework.DatosNegocio.ICampoUsuario) AndAlso Not tipoDestino Is GetType(Framework.DatosNegocio.IValidador) Then


                    If prop.PropertyType.IsInterface Then


                        RegistrarTipoInterface(entidadNavOrigen, prop)


                    Else

                        Dim tipoFijado As System.Type

                        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaTipada(prop.PropertyType) Then
                            tipoFijado = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(prop.PropertyType, Nothing)
                        Else
                            tipoFijado = prop.PropertyType
                        End If

                        Me.RegistrarRelacion(entidadNavOrigen, tipoFijado, prop.Name)

                    End If
                End If
                'Dim cardinalidad As MNavegacionDatosDN.CardinalidadRelacion = CardinalidadRelacion.CeroAUno
                'Dim tipoDestino As System.Type
                'If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsColeccion(prop.PropertyType) Then
                '    tipoDestino = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(prop.PropertyType, Nothing)
                '    cardinalidad = CardinalidadRelacion.CeroAMuchos
                'Else
                '    tipoDestino = prop.PropertyType
                'End If

                'If (Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.Implementa(tipoDestino, GetType(Framework.DatosNegocio.IEntidadDN)) OrElse tipoDestino.IsInterface) AndAlso Not tipoDestino Is GetType(Framework.DatosNegocio.ICampoUsuario) AndAlso Not tipoDestino Is GetType(Framework.DatosNegocio.IValidador) Then

                '    Dim entidadNavDestino As MNavegacionDatosDN.EntidadNavDN = RecuperarEntidadNavDNoNueva(tipoDestino)
                '    Dim pnav As New MNavegacionDatosDN.RelacionEntidadesNavDN(entidadNavOrigen, prop.Name, cardinalidad, entidadNavDestino)
                '    Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, mRec)

                '    gi.Guardar(pnav)

                'End If


            Next



            tlproc.Confirmar()
        Catch ex As Exception
            tlproc.Cancelar()
            Throw
        End Try

    End Sub

    Public Sub RegistrarTipoInterface(ByVal pEntidadNavOrigen As MNavegacionDatosDN.EntidadNavDN, ByVal pPropiedad As Reflection.PropertyInfo)


        Dim nombreCampo As String = PropiedadDeInstanciaDN.RecuperarNombreCampoVinculado(pPropiedad)

        If Not String.IsNullOrEmpty(nombreCampo) Then
            ' como no hay datos para el tipo en concreto verificar si lo hay para la interfae en general
            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            Dim colTiposImplementan As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
            colTiposImplementan.AddRangeObject(gdmi.TiposQueImplementanInterface(nombreCampo, pPropiedad))

            For Each vc As Framework.TiposYReflexion.DN.VinculoClaseDN In colTiposImplementan
                RegistrarRelacion(pEntidadNavOrigen, vc.TipoClase, pPropiedad.Name)
            Next

        End If




    End Sub




    Private Sub RegistrarRelacion(ByVal entidadNavOrigen As MNavegacionDatosDN.EntidadNavDN, ByVal pTipoDestino As System.Type, ByVal pNombrePropiedad As String)


        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        Try


            Dim cardinalidad As MNavegacionDatosDN.CardinalidadRelacion = CardinalidadRelacion.CeroAUno
            Dim tipoDestino As System.Type
            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsColeccion(pTipoDestino) Then
                tipoDestino = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipoDestino, Nothing)
                cardinalidad = CardinalidadRelacion.CeroAMuchos
            Else
                tipoDestino = pTipoDestino
            End If

            If (Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.Implementa(tipoDestino, GetType(Framework.DatosNegocio.IEntidadDN)) OrElse tipoDestino.IsInterface) AndAlso Not tipoDestino Is GetType(Framework.DatosNegocio.ICampoUsuario) AndAlso Not tipoDestino Is GetType(Framework.DatosNegocio.IValidador) Then
                Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, mRec)
                gi.Guardar(entidadNavOrigen) ' para evitar que si la entidad origen y destino refieren a una misma vc esta se repita en la bd

                Dim entidadNavDestino As MNavegacionDatosDN.EntidadNavDN = RecuperarEntidadNavDNoNueva(tipoDestino)
                Dim pnav As New MNavegacionDatosDN.RelacionEntidadesNavDN(entidadNavOrigen, pNombrePropiedad, cardinalidad, entidadNavDestino)
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, mRec)
                gi.Guardar(pnav)

            End If

            tlproc.Confirmar()
        Catch ex As Exception
            tlproc.Cancelar()
            Throw
        End Try


    End Sub

    'Public Sub RegistrarTipo(ByVal pTipo As Type)


    '    Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

    '    Try

    '        Debug.WriteLine(pTipo.FullName)



    '        If pTipo.IsInterface Then
    '            ' hay que pedir al mapeado de acceso a datos el mapeado de esta interface
    '            ' y para cada clase mmapeadora llmar a registrar RegistrarTipoClase



    '            ' RegistrarTipoInterface(pTipo)



    '        Else

    '            RegistrarTipoClase(pTipo)

    '        End If





    '        tlproc.Confirmar()
    '    Catch ex As Exception
    '        tlproc.Cancelar()
    '        Throw
    '    End Try


    'End Sub

    Public Function RecuperarEntidadNavDNoNueva(ByVal pTipo As System.Type) As EntidadNavDN

        RecuperarEntidadNavDNoNueva = RecuperarEntidadNavDN(pTipo)

        If RecuperarEntidadNavDNoNueva Is Nothing Then
            RecuperarEntidadNavDNoNueva = New MNavegacionDatosDN.EntidadNavDN(pTipo)
        End If

    End Function

    Public Function RecuperarEntidadNavDN(ByVal pTipo As System.Type) As EntidadNavDN
        ' todo ad que recupere por el tipo desde la bd

        Dim lista As List(Of EntidadNavDN)
        lista = Me.RecuperarListaCondicional(Of EntidadNavDN)(New BuscadorEntidadNavAD(pTipo))

        If lista Is Nothing Then
            Return Nothing

        Else
            Select Case lista.Count

                Case Is = 0
                    Return Nothing
                Case Is = 1
                    Return lista(0)
                Case Else
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Error de intefridad en la bd RecuperarEntidadNav recuperó:" & lista.Count)
            End Select


        End If




    End Function


    Public Function RecuperarRelaciones(ByVal pTipo As System.Type) As ColRelacionEntidadesNavDN




        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

        Try

            Dim col As New ColRelacionEntidadesNavDN
            Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, mRec)

            col.AddRange(Me.RecuperarListaCondicional(Of MNavegacionDatosDN.RelacionEntidadesNavDN)(New BuscadorRelacionesAEntidadNavAD(pTipo)))



            ' convertir las relaciones de juellas tipadas a relaciones con el tipo referido
            Beep()

            Dim referidosPorHuellas As New ColRelacionEntidadesNavDN

            Dim ReferenciasAHuellas As New ColRelacionEntidadesNavDN

            For Each re As RelacionEntidadesNavDN In col
                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(re.TipoOrigen) Then
                    ReferenciasAHuellas.Add(re)
                    referidosPorHuellas.AddRange(Me.RecuperarListaCondicional(Of MNavegacionDatosDN.RelacionEntidadesNavDN)(New BuscadorRelacionesAEntidadNavAD(re.TipoOrigen)))
                End If
            Next


            ' eliminar las referencias de las huellas
            ' col.EliminarEntidadDN(ReferenciasAHuellas, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)

            ' añadir las referencias de las huellas
            col.AddRange(referidosPorHuellas)


            ' añadir las entidades contra huellas no tipadas



            ' añadir las notas


            RecuperarRelaciones = col

            tlproc.Confirmar()
        Catch ex As Exception
            tlproc.Cancelar()
            Throw
        End Try






    End Function



    'Public Sub AñadirColRelacionesNotas(ByVal col As ColRelacionEntidadesDN)



    'End Sub


    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="pTipo"></param>
    ''' <param name="pIRecuperadorInstanciaMap">debe ser un recuperador de mapeados que filtre por el usuario</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarEntNavVincDN(ByVal pTipo As System.Type, ByVal pIRecuperadorInstanciaMap As MV2DN.IRecuperadorInstanciaMap) As EntNavVincDN

        ' de alguna manera se debieran asociar los mapeados de visualizacion a el rol y recuperar el map de visisvilidad para los roles del principal




        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

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



            tlproc.Confirmar()
        Catch ex As Exception
            tlproc.Cancelar()
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


