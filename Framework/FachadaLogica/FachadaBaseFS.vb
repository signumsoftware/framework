Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN

Public Class FachadaBaseFS
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "Métodos"

    Function RecuperarColTiposCompatibles(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pTipo As System.Type) As Generic.IList(Of System.Type)

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            '  RecuperarColTiposCompatibles = miln.RecuperarColTiposCompatibles(pTipo)
            '-----------------------------------------------------------------------------


            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

            ' Return Nothing

        End Using



    End Function

    Public Function RecuperarPorValorIDenticoEnTipo(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pTipo As System.Type, ByVal pHashValor As String) As ArrayList
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            RecuperarPorValorIDenticoEnTipo = miln.RecuperarPorValorIDenticoEnTipo(pTipo, pHashValor)

            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)
        End Using

    End Function

    Public Function RecuperarGenerico(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal huellaEnt As Framework.DatosNegocio.IHuellaEntidadDN) As Object
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '3º verificacion de permisos por DN
            ' actor.Autorizado(TipoAutorizacionClase.recuperar, huellaEnt.TipoEntidadReferida)

            '-----------------------------------------------------------------------------
            '4º creacion de la ln y ejecucion del metodo
            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            RecuperarGenerico = miln.RecuperarGenerico(huellaEnt)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        End Using

    End Function

    Public Function RecuperarGenerico(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal idEnt As String, ByVal tipoEnt As System.Type) As Object
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '3º verificacion de permisos por DN
            '  actor.Autorizado(TipoAutorizacionClase.recuperar, tipoEnt)

            '-----------------------------------------------------------------------------
            '4º creacion de la ln y ejecucion del metodo
            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            RecuperarGenerico = miln.RecuperarGenerico(idEnt, tipoEnt)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        End Using

    End Function

    Public Function RecuperarLista(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pTipo As System.Type) As IList
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            RecuperarLista = miln.RecuperarLista(pTipo)

            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)
        End Using

    End Function

    Public Function RecuperarLista(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal listaIDs As ArrayList, ByVal pTipo As System.Type) As IList
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            RecuperarLista = miln.RecuperarLista(listaIDs, pTipo)

            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)
        End Using

    End Function

    Public Function RecuperarListaGenerico(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal colHuellasEnt As Framework.DatosNegocio.ColIHuellaEntidadDN) As Framework.DatosNegocio.ColIEntidadBaseDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
        Dim lista As Framework.DatosNegocio.ColIEntidadBaseDN

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            lista = New Framework.DatosNegocio.ColIEntidadBaseDN
            For Each huella As Framework.DatosNegocio.IHuellaEntidadDN In colHuellasEnt
                lista.Add(Me.RecuperarGenerico(idSesion, actor, huella))
            Next

            RecuperarListaGenerico = lista
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        End Using
    End Function

    Public Function BajaGenericoDN(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) As Framework.DatosNegocio.IEntidadBaseDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()

            miln.BajaGenericoDN(pEntidad)
            BajaGenericoDN = pEntidad
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)
        End Using

    End Function


    Public Function GuardarGenerico(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) As Framework.DatosNegocio.IEntidadBaseDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            miln.GuardarGenerico(pEntidad)
            GuardarGenerico = pEntidad
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)
        End Using

    End Function

    Public Sub GuardarListaTipos(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal lista As IList)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As ClaseBaseLN.BaseTransaccionConcretaLN
            miln = New ClaseBaseLN.BaseTransaccionConcretaLN()
            miln.GuardarListaTipos(lista)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)
        End Using

    End Sub

#End Region



End Class
