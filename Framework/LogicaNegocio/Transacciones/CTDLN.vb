#Region "Importaciones"

Imports System.Collections
Imports System.Collections.Generic

Imports Framework.DatosNegocio

#End Region

Namespace Transacciones

    ''' <summary>Esta clase representa a un controlador de transacciones distribuidas.</summary>
    ''' <remarks>Falta incluir el hilo que comprueba que las transacciones no han caducado por tiempo.</remarks>
    Public Class CTDLN
        Implements ICTDLN

#Region "Atributos"
        'Tabla donde se guardan las transacciones logicas que estamos controlando
        Private Shared mHTTransaccionesLogicas As Dictionary(Of String, TransaccionLogicaLN)
#End Region

#Region "Constructores Estaticos"
        ''' <summary>Constructor estatico por defecto.</summary>
        Shared Sub New()
            mHTTransaccionesLogicas = New Dictionary(Of String, TransaccionLogicaLN)
        End Sub
#End Region

#Region "Metodos"
        ''' <summary>Metodo que inicia una transaccion logica.</summary>
        ''' <remarks>
        ''' Si ya se esta dentro de una transaccion logica se devulve un proxy de transaccion logica que no puede
        ''' ni iniciar ni cancelar la transaccion. Habria que pensar si al cancelar se desencadena una excepcion por el 
        ''' proxy para que burbujee y la transaccion se cancele por el proceso que la inicio.
        ''' </remarks>
        ''' <param name="pTLPadre" type="ITransaccionLogicaLN">
        ''' Padre de la transaccion que queremos iniciar.
        ''' </param>
        ''' <param name="pTLProceso" type="ITransaccionLogicaLN">
        ''' Parametro donde vamos a devolver la transaccion que iniciamos.
        ''' </param>
        Public Sub IniciarTransaccion(ByRef pTLPadre As ITransaccionLogicaLN, ByRef pTLProceso As ITransaccionLogicaLN) Implements ICTDLN.IniciarTransaccion
            'Si el padre no es nothing esta transaccion ya ha comenzado
            If (Not pTLPadre Is Nothing) Then
                'Si la transaccion ya termino devolvemos error
                If (pTLPadre.Estado = EstadoTransaccionLN.Cerrada) Then
                    Throw New ApplicationException("Error: el estado de la transaccion es Cerrada.")
                End If

                'En caso contrario devolvemos un proxy a la transaccion del padre
                pTLProceso = New TransaccionLogicaProxyLN(pTLPadre)

            Else
                'Si el padre era nothing creamos una transaccion
                Dim tl As ITransaccionLogicaLN

                tl = New TransaccionLogicaLN(Me)

                'Le añadimos el manejador de eventos de cancelacion
                AddHandler tl.CancelarTL, AddressOf CancelarTransaccionManejador

                'La guardamos en la tabla de transacciones
                mHTTransaccionesLogicas.Add(tl.ID, tl)

                'Devolvemos la nueva transaccion
                pTLPadre = tl
                pTLProceso = tl
            End If
        End Sub

        ''' <summary>Metodo que confirma una transaccion logica.</summary>
        ''' <remarks>
        ''' Al confirmar la transaccion se confirman todas las transacciones embebidas.
        ''' </remarks>
        ''' <param name="pTL" type="ITransaccionLogicaLN">
        ''' Transaccion que queremos confirmar.
        ''' </param>
        Public Sub ConfirmarTransaccion(ByVal pTL As ITransaccionLogicaLN) Implements ICTDLN.ConfirmarTransaccion
            Dim transRec As ITransaccionRecursoLN
            Dim estado As IEstadoTLLN

            'Si la transaccion no es nothing y la tenemos en la tabla de transacciones,
            'confirmamos todas las transacciones embebidas dentro de esta
            If (Not pTL Is Nothing) Then
                If (CTDLN.mHTTransaccionesLogicas.ContainsKey(pTL.ID)) Then
                    Dim col As IEnumerable(Of ITransaccionRecursoLN) = pTL.TransacionesRecurso.Values

                    For Each transRec In col
                        transRec.Gestor.ConfirmarTransaccion(transRec)
                    Next
                    ' eliminamos la transaccion loguica del repositorio
                    CTDLN.mHTTransaccionesLogicas.Remove(pTL.ID)
                End If

                'Marcamos el estado de la transaccion como cerrada
                estado = pTL
                estado.Estado = EstadoTransaccionLN.Cerrada
            End If
        End Sub

        ''' <summary>Metodo que cancela una transaccion logica.</summary>
        ''' <remarks>
        ''' Al cancelar la transaccion se cancelan todas las transacciones embebidas.
        ''' </remarks>
        ''' <param name="pTL" type="ITransaccionLogicaLN">
        ''' Transaccion que queremos cancelar.
        ''' </param>
        Public Sub CancelarTransaccion(ByVal pTL As ITransaccionLogicaLN) Implements ICTDLN.CancelarTransaccion
            'Dim transRec As ITransaccionRecursoLN
            'Dim estado As IEstadoTLLN

            ''Si la transaccion no es nothing y la tenemos en la tabla de transacciones,
            ''cancelamos todas las transacciones embebidas dentro de esta
            'If (Not pTL Is Nothing) Then
            '    If (CTDLN.mHTTransaccionesLogicas.ContainsKey(pTL.ID)) Then
            '        Dim a As IEnumerable

            '        a = CType(pTL.TransacionesRecurso, IEnumerable)
            '        Dim b As IEnumerator = a.GetEnumerator()

            '        While (b.MoveNext = True)
            '            transRec = CType(b.Current, DictionaryEntry).Value
            '            transRec.Gestor.CancelarTransaccion(transRec)
            '        End While
            '    End If

            '    'Marcamos el estado de la transaccion como cerrada
            '    estado = pTL
            '    estado.Estado = EstadoTransaccionLN.Cerrada
            'End If

            Dim transRec As ITransaccionRecursoLN
            Dim estado As IEstadoTLLN

            'Si la transaccion no es nothing y la tenemos en la tabla de transacciones,
            'cancelamos todas las transacciones embebidas dentro de esta
            If (Not pTL Is Nothing) Then
                If (CTDLN.mHTTransaccionesLogicas.ContainsKey(pTL.ID)) Then
     

                    Dim dic As Dictionary(Of String, ITransaccionRecursoLN)
                    dic = pTL.TransacionesRecurso

                    For Each transRec In dic.Values
                        transRec.Gestor.CancelarTransaccion(transRec)
                    Next


                End If

                'Marcamos el estado de la transaccion como cerrada
                estado = pTL
                estado.Estado = EstadoTransaccionLN.Cerrada
            End If


        End Sub
#End Region

#Region "Manejadores Eventos"
        ''' <summary>Metodo que se encarga del evento de cancelacion de una transaccion.</summary>
        ''' <param name="pSender" type="Object">
        ''' Objeto que emite el evento.
        ''' </param>
        ''' <param name="pE" type="System.EventArgs">
        ''' Argumentos del evento.
        ''' </param>
        Private Sub CancelarTransaccionManejador(ByVal pSender As Object, ByVal pE As System.EventArgs)
            Me.CancelarTransaccion(pSender)
        End Sub
#End Region

    End Class
End Namespace
