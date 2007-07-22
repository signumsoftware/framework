#Region "importaciones"

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.DatosNegocio
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.AccesoDatos.MotorAD.AD

#End Region

Public Class BaseGenericLN
    Inherits BaseTransaccionLN


#Region "Contructor"
    Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region

    ''' <summary>
    ''' Solo se puede utilizar si el código es generico sin lógica
    ''' <param name="id"></param>
    ''' </summary>
    Protected Function Recuperar(Of T As EntidadDN)(ByVal huella As Framework.DatosNegocio.IHuellaEntidadDN) As T


        If Not huella.TipoEntidadReferida Is GetType(T) Then
            Throw New LogicaNegocios.ApplicationExceptionLN("los tipos no son compatibles")
        End If

        Return Recuperar(Of T)(huella.IdEntidadReferida)

    End Function

    Protected Function Recuperar(Of T As EntidadDN)(ByVal id As String) As T
        Return Recuperar(Of T)(id, mTL)
    End Function

    Private Function Recuperar(Of T As EntidadDN)(ByVal id As String, ByVal trans As ITransaccionLogicaLN) As T
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ctd As CTDLN
        Dim gestor As GestorInstanciacionLN
        Dim result As T = Nothing

        ctd = New CTDLN()

        Try
            ctd.IniciarTransaccion(trans, tlproc)

            gestor = New GestorInstanciacionLN(tlproc, Me.mRec)
            result = gestor.Recuperar(Of T)(id)

            tlproc.Confirmar()
            Return result
        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Function

    Protected Function RecuperarLista(Of T As EntidadBaseDN)() As List(Of T)
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim aMAD As AccesorMotorAD = Nothing

        Try
            tlproc = ObtenerTransaccionDeProceso()
            aMAD = New AccesorMotorAD(tlproc, mRec, New ConstructorAL(GetType(T)))

            '  Dim alIDs As ArrayList = aMAD.BuscarGenericoIDS("tl" + GetType(T).Name, Nothing)
            Dim alIDs As ArrayList = aMAD.BuscarGenericoIDS(GetType(T), Nothing)

            Dim objetos As List(Of T) = Me.RecuperarLista(Of T)(alIDs)

            tlproc.Confirmar()

            Return objetos

        Catch ex As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Function

    Protected Function RecuperarLista(Of T As EntidadBaseDN)(ByVal listaIDs As ArrayList) As List(Of T)
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gestor As GestorInstanciacionLN

        Try
            tlproc = ObtenerTransaccionDeProceso()

            gestor = New GestorInstanciacionLN(tlproc, Me.mRec)

            Dim alObj As ArrayList = CType(gestor.Recuperar(listaIDs, GetType(T), Nothing), ArrayList)
            Dim objetos As List(Of T) = New List(Of T)(alObj.Count)
            For i As Integer = 0 To alObj.Count - 1
                objetos.Add(CType(alObj(i), T))
            Next

            tlproc.Confirmar()

            Return objetos

        Catch ex As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw

        End Try

    End Function

    Protected Function RecuperarListaCondicional(Of T As EntidadDN)(ByVal constructor As IConstructorBusquedaAD) As List(Of T)
        Dim amd As AccesorMotorAD = Nothing
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ctd As CTDLN
        Dim alIds As ArrayList = Nothing
        Dim gestor As GestorInstanciacionLN

        ctd = New CTDLN()
        Try
            ctd.IniciarTransaccion(mTL, tlproc)

            amd = New AccesorMotorAD(tlproc, mRec, constructor)
            alIds = amd.BuscarGenericoIDS(GetType(T))
            gestor = New GestorInstanciacionLN(tlproc, Me.mRec)
            Dim alObj As ArrayList = CType(gestor.Recuperar(alIds, GetType(T), Nothing), ArrayList)
            Dim objetos As List(Of T) = New List(Of T)(alObj.Count)
            For i As Integer = 0 To (alObj.Count - 1)
                objetos.Add(CType(alObj(i), T))
            Next

            tlproc.Confirmar()
            Return objetos
        Catch ex As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Function

    ''' <summary>
    ''' Solo se puede utilizar si el código es genérico sin lógica
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="dato"></param>
    Protected Function Guardar(Of T As EntidadDN)(ByVal dato As T) As T
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ctd As CTDLN
        Dim gestor As GestorInstanciacionLN

        ctd = New CTDLN()
        Try
            ctd.IniciarTransaccion(mTL, tlproc)

            gestor = New GestorInstanciacionLN(tlproc, Me.mRec)
            gestor.Guardar(dato)

            tlproc.Confirmar()

            Return dato
        Catch ex As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Function

    ''' <summary>
    ''' Solo se puede utilizar si el código es genérico sin lógica
    ''' </summary>
    ''' <param name="lista">Lista de objetos a guardar</param>
    Protected Sub GuardarLista(Of T As EntidadDN)(ByVal lista As List(Of T))
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ctd As CTDLN
        Dim gestor As GestorInstanciacionLN

        ctd = New CTDLN()
        Try
            ctd.IniciarTransaccion(mTL, tlproc)

            gestor = New GestorInstanciacionLN(tlproc, Me.mRec)
            For i As Integer = 0 To lista.Count - 1
                gestor.Guardar(lista(i))
            Next

            tlproc.Confirmar()
        Catch ex As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Sub

    ''' <summary>
    ''' Solo se puede utilizar si el código es genérico sin lógica
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="id"></param>
    Protected Sub Baja(Of T As EntidadDN)(ByVal id As String)
        Dim dato As IDatoPersistenteDN = CType(Recuperar(Of T)(id), IDatoPersistenteDN)
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ctd As CTDLN
        Dim gestor As GestorInstanciacionLN

        ctd = New CTDLN()
        Try
            ctd.IniciarTransaccion(mTL, tlproc)

            gestor = New GestorInstanciacionLN(tlproc, Me.mRec)
            dato.Baja = True
            gestor.Baja(dato)

            tlproc.Confirmar()
        Catch ex As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Sub

    ''' <summary>
    ''' Solo se puede utilizar si el código es genérico sin lógica
    ''' <param name="id"></param>
    ''' </summary>
    Protected Sub Reactivar(Of T As EntidadDN)(ByVal id As String)
        Dim dato As IDatoPersistenteDN = CType(Recuperar(Of T)(id), IDatoPersistenteDN)
        dato.Baja = False
        Guardar(Of T)(CType(dato, T))
    End Sub

End Class



