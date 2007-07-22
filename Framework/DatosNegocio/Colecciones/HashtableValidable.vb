#Region "Importaciones"

Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary

#End Region

''' <summary>Esta clase representa una Hashtable tipada.</summary>
''' <remarks>El control de los tipos se realiza en tiempo de ejecucion.</remarks>

<Serializable()> _
Public Class HashtableValidable
    Inherits Hashtable
    Implements IValidable

#Region "Atributos"
    'Validador del objeto
    Private mValidador As IValidador
#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto.</summary>
    ''' <param name="pValidador" type="IValidador">
    ''' El validador que vamos a usar para validar el arraylist.
    ''' </param>
    Public Sub New(ByVal pValidador As IValidador)
        MyBase.New()
        mValidador = pValidador
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene el validador que valida la HashtableValidable.</summary>
    Public ReadOnly Property Validador() As IValidador Implements IValidable.Validador
        Get
            Return mValidador
        End Get
    End Property
#End Region

#Region "Metodos"
    ''' <summary>Añade un elemento a la HashtableValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de Hashtable para incluir la validacion.
    ''' </remarks>
    ''' <param name="pClave" type="Object">
    ''' Clave con la que añadimos el objeto.
    ''' </param>
    ''' <param name="pElemento" type="Object">
    ''' El objeto que añadimos a la HashtableValidable.
    ''' </param>
    Public Overrides Sub Add(ByVal pClave As Object, ByVal pElemento As Object)
        If (Not ValTipoDeDatos(pElemento)) Then
            Throw New ApplicationException("Error: se ha intentado añadir un tipo incorrecto a la hashtable.")
        End If

        MyBase.Add(pClave, pElemento)
    End Sub

    ''' <summary>Obtiene una copia en profundidad de la HashtableValidable</summary>
    ''' <returns>La copia de la HashtableValidable</returns>
    Public Overrides Function Clone() As Object
        Dim formateador As BinaryFormatter
        Dim memoria As MemoryStream

        formateador = New BinaryFormatter
        memoria = New MemoryStream

        'Nos serializamos y volvemos a poner el puntero de lectura/escritura al principio
        formateador.Serialize(memoria, Me)
        memoria.Seek(0, IO.SeekOrigin.Begin)

        'Nos desserializamos para conseguir la copia
        Return formateador.Deserialize(memoria)
    End Function

    ''' <summary>
    ''' Indica si la validacion que se realiza sobre la HashtableValidable es la misma que la del validador
    ''' pasado por parametro.
    ''' </summary>
    ''' <param name="pValidador" type="IValidador">
    ''' El validador contra el que vamos a comparar.
    ''' </param>
    ''' <returns>Si la validacion que realizan los dos validadores es la misma o no.</returns>
    Public Function ValidacionIdentica(ByVal pValidador As IValidador) As Boolean Implements IValidable.ValidacionIdentica
        If (Me.mValidador.Formula = pValidador.Formula) Then
            Return True

        Else
            Return False
        End If
    End Function
#End Region

#Region "Metodos Auxiliares"
    ''' <summary>Indica si un objeto es valido para esta HashtableValidable o no.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' El objeto que queremos validar.
    ''' </param>
    ''' <returns>Si el objeto es valido o no.</returns>
    Private Function ValTipoDeDatos(ByVal pObjeto As Object) As Boolean
        Dim mensaje As String = String.Empty
        If (Not mValidador Is Nothing) Then
            Return Me.mValidador.Validacion(mensaje, pObjeto)
        End If

        Return True
    End Function
#End Region

End Class
