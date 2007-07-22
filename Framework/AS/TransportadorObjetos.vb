#Region "Importaciones"

Imports System.IO
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary

#End Region


Namespace Framework.AS

    ''' <summary>
    ''' Esta clase se encarga del manejo de objetos en la comunicación cliente servidor a través de un servicio web.
    ''' </summary>
    ''' <remarks>
    ''' Esta clase empaqueta y desempaqueta objetos serializándolos para poder transportarlos del cliente al servidor y viceversa
    ''' vía un servicio web. Los objetos viajan serializados segun el formateador asignado. El formateador por defecto es el binario.
    ''' </remarks>
    ''' <seealso cref="ObjetoTransporte">Framework.Utilidades.ObjetoTransporte</seealso>

    Public Class TransportadorObjetos

#Region "Atributos"

        'Formateador para serializar los objetos
        Private mFormateador As IFormatter
#End Region

#Region "Constructores"

        ''' <overloads>El constructor esta sobrecargado.</overloads>
        ''' <summary>
        ''' Constructor por defecto.
        ''' </summary>
        ''' <Remarks>
        ''' El formateador por defecto es el formateador binario.
        ''' </Remarks>
        Public Sub New()
            mFormateador = New BinaryFormatter
        End Sub

        ''' <summary>
        ''' Constructor que acepta el tipo de formateador que vamos a usar.
        ''' </summary>
        ''' <param name="pFormateador" type="IFormatter">
        ''' El formateador que vamos a usar para serializar los objetos.
        ''' </param>
        Public Sub New(ByVal pFormateador As IFormatter)
            Dim mensaje As String = String.Empty

            If (ValFormateador(pFormateador, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mFormateador = pFormateador
        End Sub
#End Region

#Region "Propiedades"

        ''' <summary>
        ''' Obtiene o asigna el formateador que se utiliza para serializar.
        ''' </summary>
        Public Property Formateador() As IFormatter
            Get
                Return mFormateador
            End Get
            Set(ByVal Value As IFormatter)
                Dim mensaje As String = String.Empty

                If (ValFormateador(Value, mensaje) = False) Then
                    Throw New ApplicationException(mensaje)
                End If

                mFormateador = Value
            End Set
        End Property
#End Region

#Region "Metodos Validacion"

        ''' <summary>
        ''' Comprueba si un formateador es correcto para su utilizacion para serilizar objetos.
        ''' </summary>
        ''' <param name="pFormateador" type="IFormatter">
        ''' El formateador que queremos comprobar.
        ''' </param>
        ''' <param name="pMensaje" type="String">
        ''' String donde vamos a devolver un mensaje de error en caso de que no se supere la validacion.
        ''' </param>
        ''' <return>
        ''' Si el formateador era correcto o no.
        ''' </return>
        Public Shared Function ValFormateador(ByVal pFormateador As IFormatter, ByRef pMensaje As String) As Boolean
            If (pFormateador Is Nothing) Then
                pMensaje = "Error: el formateador no puede ser nulo."
                Return False
            End If

            Return True
        End Function
#End Region

#Region "Metodos"

        ''' <summary>
        ''' Empaqueta un objeto dentro de un ObjetoTransporte de forma serializada.
        ''' </summary>
        ''' <param name="pDatos" type="Object">
        ''' Los datos que vamos a empaquetar de forma serializada.
        ''' </param>
        ''' <return>
        ''' El ObjetoTransporte que empaqueta el objeto que queremos transportar.
        ''' </return>
        Public Function Empaquetar(ByVal pDatos As Object) As ObjetoTransporte
            Dim transporte As ObjetoTransporte
            Dim memoria As MemoryStream

            Try
                If (pDatos Is Nothing) Then
                    Return Nothing

                Else
                    transporte = New ObjetoTransporte
                    memoria = New MemoryStream

                    'Serializamos el objeto en memoria
                    mFormateador.Serialize(memoria, pDatos)

                    'Lo guardamos en el objeto de transporte
                    transporte.Datos = memoria.GetBuffer
                End If

                'Cerramos el flujo
                memoria.Close()

                Return transporte

            Catch ex As Exception
                Throw ex
            End Try
        End Function

        ''' <summary>
        ''' Desempaqueta los datos contenidos en un ObjetoTransporte.
        ''' </summary>
        ''' <param name="pPaquete" type="ObjetoTransporte">
        ''' Los datos que vamos a desempaquetar.
        ''' </param>
        ''' <return>
        ''' El objeto original que transportabamos serializado.
        ''' </return>
        Public Function Desempaquetar(ByVal pPaquete As ObjetoTransporte) As Object
            Dim memoria As MemoryStream
            Dim objeto As Object

            Try
                If (pPaquete.Datos Is Nothing) Then
                    Return Nothing
                End If

                'Pasamos el objeto a memoria
                memoria = New MemoryStream(pPaquete.Datos)

                'Lo desserializamos y cerramos el flujo
                objeto = mFormateador.Deserialize(memoria)
                memoria.Close()

                Return objeto

            Catch ex As Exception
                Throw ex
            End Try
        End Function

        ''' <summary>
        ''' Empaqueta un objeto dentro de un paquete publicado por un servicio web.
        ''' </summary>
        ''' <remarks>
        ''' Para poder utilizar un paquete dentro de este método deberá tener una propiedad pública llamada "Datos" declarada
        ''' como un array de bytes.
        ''' </remarks>
        ''' <param name="pDatos" type="Object">
        ''' Los datos que vamos a empaquetar de forma serializada.
        ''' </param>
        ''' <param name="pPaqueteSW" type="Object">
        ''' El paquete publicado por el servicio web donde vamos a guardar el objeto.
        ''' </param>
        Public Sub EmpaquetarSW(ByVal pDatos As Object, ByVal pPaqueteSW As Object)
            Try
                If (pDatos Is Nothing) Then
                    Return
                End If

                'Empaquetamos los datos y los pasamos al paquete web
                pPaqueteSW.Datos = Empaquetar(pDatos).Datos

            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        ''' <summary>
        ''' Desempaqueta un objeto contenido en un paquete publicado por un servicio web.
        ''' </summary>
        ''' <remarks>
        ''' Para poder desempaquetar el objeto en este método, el paquete deberá tener una propiedad pública llamada "Datos" declarada
        ''' como un array de bytes.
        ''' </remarks>
        ''' <param name="pPaqueteSW" type="Object">
        ''' Los datos que vamos a desempaquetar.
        ''' </param>
        ''' <return>
        ''' El objeto original que transportabamos serializado.
        ''' </return>
        Public Function DesempaquetarSW(ByVal pPaqueteSW As Object) As Object
            Dim transporteAux As ObjetoTransporte

            'Movemos los datos a un objeto auxiliar y los desempaquetamos
            transporteAux = New ObjetoTransporte
            transporteAux.Datos = pPaqueteSW.Datos

            Return Desempaquetar(transporteAux)
        End Function
#End Region

    End Class

End Namespace

