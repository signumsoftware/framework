#Region "importaciones"

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.DatosNegocio
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.AccesoDatos.MotorAD.AD

#End Region

Public Class BaseTransaccionConcretaLN
    Inherits Framework.LogicaNegocios.Transacciones.BaseGenericLN 'BaseTransaccionLN

#Region "Constructor"

    'Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
    '    MyBase.New(pTL, pRec)
    'End Sub

#End Region

#Region "Métodos"


    Public Function RecuperarObjetoReverso(ByVal entidadReferida As EntidadDN, ByVal pTiporeferidor As System.Type, ByVal pNombrePropiedadReferidora As String) As EntidadDN
        Using tr As New Transaccion



            ' recuperar padre dada una entidad referida

            Dim gi As New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Dim pdi As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pTiporeferidor, pNombrePropiedadReferidora, entidadReferida.ID, entidadReferida.GUID)

            If pdi Is Nothing Then
                Throw New ApplicationException("No se encuentra la propiedad")
            End If

            Dim il As IList = gi.RecuperarColHuellasRelInversa(pdi, entidadReferida.GetType)

            Select Case il.Count

                Case Is = 0
                    RecuperarObjetoReverso = Nothing

                Case Is = 1
                    gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    Dim ihe As Framework.DatosNegocio.IHuellaEntidadDN = gi.Recuperar(CType(il(0), HEDN))
                    If ihe Is Nothing Then
                        RecuperarObjetoReverso = Nothing
                    Else
                        RecuperarObjetoReverso = ihe.EntidadReferida
                    End If


                Case Is > 1
                    Throw New ApplicationException("Tansolo se debia encontrar uno")

            End Select


            tr.Confirmar()

        End Using




    End Function

    Public Sub GuardarListaTipos(ByVal lista As IList)
        Dim gestor As GestorInstanciacionLN
        Dim aMAD As AccesorMotorAD = Nothing

        Using tr As New Transaccion()

            If Not lista Is Nothing AndAlso lista.Count > 0 Then

                Dim o As IEntidadDN
                For Each o In lista
                    gestor = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    If o.Baja Then
                        gestor.Baja(o)
                    Else
                        gestor.Guardar(o)
                    End If
                Next

            End If

            tr.Confirmar()

        End Using

    End Sub

    Public Overloads Function RecuperarLista(ByVal pTipo As System.Type) As IList
        Dim gestor As GestorInstanciacionLN
        Dim aMAD As AccesorMotorAD = Nothing
        'Dim alObj As ArrayList

        Using tr As New Transaccion()
            'aMAD = New AccesorMotorAD(Transaccion.Actual, Recurso.Actual, New ConstructorAL())
            'aMAD = New AccesorMotorAD(Transaccion.Actual, Recurso.Actual, New ConstructorSQLSQLsAD())

            'alObj = New ArrayList()

            'Dim alIDs As ArrayList = aMAD.BuscarGenericoIDS(pTipo, Nothing)

            'alObj = RecuperarLista(alIDs, pTipo)

            'tr.Confirmar()

            'Return alObj


            gestor = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            RecuperarLista = gestor.RecuperarLista(pTipo)

            tr.Confirmar()

        End Using

    End Function

    Public Overloads Function RecuperarLista(ByVal listaIDs As ArrayList, ByVal pTipo As System.Type) As IList
        Dim gestor As GestorInstanciacionLN
        Dim aMAD As AccesorMotorAD = Nothing
        Dim alObj As ArrayList

        Using tr As New Transaccion()
            aMAD = New AccesorMotorAD(Transaccion.Actual, Recurso.Actual, New ConstructorAL(pTipo))

            alObj = New ArrayList()

            If listaIDs IsNot Nothing AndAlso listaIDs.Count > 0 Then
                gestor = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                alObj = CType(gestor.Recuperar(listaIDs, pTipo, Nothing), ArrayList)
            End If

            tr.Confirmar()

            Return alObj

        End Using

    End Function

    Public Overloads Function Recuperar(ByVal pTipo As System.Type, ByVal pId As String) As Object
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion()
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Recuperar = gi.Recuperar(pId, pTipo)

            tr.Confirmar()

        End Using

    End Function

    Public Function RecuperarPorValorIDenticoEnTipo(ByVal pTipo As System.Type, ByVal pHashValor As String) As ArrayList
        ' recuperar el tipo
        Dim amd As AccesorMotorAD = Nothing
        Dim alIds As ArrayList = Nothing
        Dim gestor As GestorInstanciacionLN

        Using tr As New Transaccion()
            Dim constructor As Framework.AccesoDatos.MotorAD.AD.ContructorValoresHashAD
            constructor = New Framework.AccesoDatos.MotorAD.AD.ContructorValoresHashAD(pTipo, pHashValor)

            amd = New AccesorMotorAD(Transaccion.Actual, Recurso.Actual, constructor)
            alIds = amd.BuscarGenericoIDS(pTipo)

            gestor = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Dim alObj As ArrayList = CType(gestor.Recuperar(alIds, pTipo, Nothing), ArrayList)

            tr.Confirmar()

            Return alObj

        End Using

    End Function

    Public Overloads Function RecuperarListaCondicional(ByVal pTipo As System.Type, ByVal constructor As IConstructorBusquedaAD) As IList
        Dim amd As AccesorMotorAD = Nothing
        Dim alIds As ArrayList = Nothing
        Dim gestor As GestorInstanciacionLN

        Using tr As New Transaccion()
            amd = New AccesorMotorAD(Transaccion.Actual, Recurso.Actual, constructor)
            alIds = amd.BuscarGenericoIDS(pTipo)

            gestor = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Dim alObj As ArrayList = CType(gestor.Recuperar(alIds, pTipo, Nothing), ArrayList)


            tr.Confirmar()

            Return alObj

        End Using

    End Function

    ''' <summary>
    ''' Método que recupera un objeto a partir de la huella
    ''' </summary>
    ''' <param name="huellaEnt"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarGenerico(ByVal huellaEnt As IHuellaEntidadDN) As Object
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            RecuperarGenerico = gi.Recuperar(huellaEnt)

            tr.Confirmar()
        End Using

    End Function

    ''' <summary>
    ''' Método que recupera un objeto de la base de datos con el Id y el tipo
    ''' </summary>
    ''' <param name="idEnt"></param>
    ''' <param name="tipoEnt"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarGenerico(ByVal idEnt As String, ByVal tipoEnt As System.Type) As Object
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            RecuperarGenerico = gi.Recuperar(idEnt, tipoEnt)

            tr.Confirmar()

        End Using

    End Function

    ''' <summary>
    ''' Método que guarda un objeto de la base de datos
    ''' </summary>
    ''' <param name="objeto"></param>
    ''' <remarks></remarks>
    Public Sub GuardarGenerico(ByVal objeto As Object)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        ' habria que controlar la utorizacon del rol sobre la entidad
        Using tr As New Transaccion()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(objeto)

            tr.Confirmar()
        End Using

    End Sub

    Public Sub BajaGenericoDN(ByRef objeto As Object)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        ' habria que controlar la utorizacon del rol sobre la entidad
        Dim dn As Framework.DatosNegocio.EntidadDN = objeto
        If Not dn.Baja Then
            Dim dp As IDatoPersistenteDN = dn
            dp.Baja = True
            objeto = dp
        End If
        Using tr As New Transaccion()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Baja(objeto)

            tr.Confirmar()
        End Using
    End Sub

#End Region



End Class


