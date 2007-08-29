#Region "Importaciones"

Imports System.IO
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary

#End Region

''' <summary>
''' Esta clase se encarga del manejo de objetos en la comunicación cliente servidor a través de un servicio web.
''' </summary>
''' <remarks>
''' Esta clase empaqueta y desempaqueta objetos serializándolos para poder transportarlos del cliente al servidor y viceversa
''' vía un servicio web. Los objetos viajan serializados segun el formateador asignado. El formateador por defecto es el binario.
''' </remarks>

Public Class Empaquetador

#Region "Atributos"

    'Formateador para serializar los objetos
    Private mFormateador As IFormatter

    Private Shared mDefault As New Empaquetador()
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
    Public Function Empaquetar(ByVal pDatos As Object) As Byte()

        If (pDatos Is Nothing) Then
            Return Nothing
        Else
            Using memoria As New MemoryStream

                'Serializamos el objeto en memoria
                mFormateador.Serialize(memoria, pDatos)

                'Lo guardamos en el objeto de transporte
                Return memoria.ToArray()
            End Using

        End If

    End Function

    Public Shared Function Empaqueta(ByVal pDatos As Object) As Byte()
        Return mDefault.Empaquetar(pDatos)
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
    Public Function Desempaquetar(ByVal pPaquete As Byte()) As Object

        If (pPaquete Is Nothing) Then
            Return Nothing
        End If

        'Pasamos el objeto a memoria
        Using memoria As New MemoryStream(pPaquete)
            Return mFormateador.Deserialize(memoria)
        End Using

    End Function

    Public Shared Function Desempaqueta(Of T)(ByVal pPaquete As Byte()) As T
        Return CType(mDefault.Desempaquetar(pPaquete), T)
    End Function

#End Region

End Class
