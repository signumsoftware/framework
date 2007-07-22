Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Ficheros
Imports Framework.Ficheros.FicherosDN
Public Class RutaAlmacenamientoFicherosLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Contructor"
    Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region


#Region "Métodos"

    ''' <summary>
    ''' recupera la RecuperarRafActivo de alamacenamiento viente
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarRafActivo() As RutaAlmacenamientoFicherosDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Dim ad As FicherosAD.RutaAlmacenamientoFicherosAD
            ad = New FicherosAD.RutaAlmacenamientoFicherosAD(tlproc, Me.mRec)

            Dim col As ColRutaAlmacenamientoFicherosDN

            col = ad.RecuperarColRafActivo()

            If col.Count = 0 Then
                Dim nuevaR As RutaAlmacenamientoFicherosDN
                Dim hfln As Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN
                hfln = New Ficheros.FicherosLN.RutaAlmacenamientoFicherosLN(tlproc, Me.mRec)

                nuevaR = hfln.AbrirSiguienteRaf()
                If nuevaR IsNot Nothing Then
                    col.Add(nuevaR)
                End If

                If col.Count = 0 Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No hay rutas de almacenamiento disponibles")
                End If
            End If


            ' poner a qui la logica de recuperacion de la ruta con prioridad
            ' cojemos la primera

            Return col.Item(0)


            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try
    End Function

    Public Function RecuperarColRafxEstado(ByVal pEstadoRaf As RutaAlmacenamientoFicherosEstado) As ColRutaAlmacenamientoFicherosDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Dim ad As FicherosAD.RutaAlmacenamientoFicherosAD
            ad = New FicherosAD.RutaAlmacenamientoFicherosAD(tlproc, Me.mRec)

            Dim col As ColRutaAlmacenamientoFicherosDN

            col = ad.RecuperarColRafxEstado(pEstadoRaf)

            If col.Count = 0 Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No hay rutas de almacenamiento disponibles")
            End If

            Return col

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try

    End Function

    Public Sub IncidentarRaf(ByVal pRaf As RutaAlmacenamientoFicherosDN)

        pRaf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Incidentada
        MyBase.Guardar(Of RutaAlmacenamientoFicherosDN)(pRaf)
    End Sub

    Public Function CerrarRaf(ByVal pRaf As RutaAlmacenamientoFicherosDN) As RutaAlmacenamientoFicherosDN
        pRaf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Cerrada
        Return MyBase.Guardar(Of RutaAlmacenamientoFicherosDN)(pRaf)
    End Function

    Public Sub AbrirRaf(ByVal pRaf As RutaAlmacenamientoFicherosDN)

        If pRaf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Disponible Then
            pRaf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Abierta
            MyBase.Guardar(Of RutaAlmacenamientoFicherosDN)(pRaf)
        Else
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Solo se pueden abrir las RAF en estado creado")
        End If
    End Sub

    Public Function CerraryAbrirSiguienteRaf(ByVal pRaf As RutaAlmacenamientoFicherosDN) As RutaAlmacenamientoFicherosDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            CerrarRaf(pRaf)
            Dim colrad As ColRutaAlmacenamientoFicherosDN
            Dim raf As RutaAlmacenamientoFicherosDN
            colrad = RecuperarColRafxEstado(RutaAlmacenamientoFicherosEstado.Disponible)
            raf = colrad(0)
            raf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Abierta
            Me.Guardar(raf)

            tlproc.Confirmar()

            Return raf

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try

    End Function
    Public Function AbrirSiguienteRaf() As RutaAlmacenamientoFicherosDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso


            Dim colrad As ColRutaAlmacenamientoFicherosDN
            Dim raf As RutaAlmacenamientoFicherosDN
            colrad = RecuperarColRafxEstado(RutaAlmacenamientoFicherosEstado.Disponible)
            raf = colrad(0)
            raf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Abierta
            Me.Guardar(raf)

            tlproc.Confirmar()

            Return raf

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try

    End Function
    Public Function IncidentaryAbrirSiguienteRaf(ByVal pRaf As RutaAlmacenamientoFicherosDN) As RutaAlmacenamientoFicherosDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            IncidentarRaf(pRaf)

            Dim colrad As ColRutaAlmacenamientoFicherosDN
            Dim raf As RutaAlmacenamientoFicherosDN
            colrad = RecuperarColRafxEstado(RutaAlmacenamientoFicherosEstado.Disponible)
            raf = colrad(0)
            raf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Abierta
            Me.Guardar(raf)

            tlproc.Confirmar()

            Return raf

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try

    End Function

    Public Overloads Function Guardar(ByVal pRaf As RutaAlmacenamientoFicherosDN) As RutaAlmacenamientoFicherosDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            If pRaf.EstadoRAF = RutaAlmacenamientoFicherosEstado.Cerrada Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No puede modificarse  RutaAlmacenamientoFicherosDN si esta cerrada ")
            Else
                Guardar = MyBase.Guardar(Of RutaAlmacenamientoFicherosDN)(pRaf)
            End If

            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try

    End Function

    Public Overloads Function Recuperar(ByVal pIdRaf As String) As RutaAlmacenamientoFicherosDN
        Return MyBase.Recuperar(Of RutaAlmacenamientoFicherosDN)(pIdRaf)
    End Function

    Public Function RecuperarListadoRutas() As IList(Of RutaAlmacenamientoFicherosDN)
        Return MyBase.RecuperarLista(Of RutaAlmacenamientoFicherosDN)()
    End Function

#End Region

End Class
