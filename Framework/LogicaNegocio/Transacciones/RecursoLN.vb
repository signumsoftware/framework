#Region "Importaciones"

Imports System.Collections.Generic

#End Region

Namespace Transacciones

    ''' <summary>
    ''' Esta clase representa un recurso que contiene la informacion necesaria para poder acceder al almacen
    ''' de datos.
    ''' </summary>
    ''' <remarks>
    ''' El recurso no asume ni sabe nada sobre su almacen de datos, solo tiene la informacion para acceder a el.
    ''' </remarks>
    Public Class RecursoLN
        Implements IRecursoLN

#Region "Atributos"
        'Identificador del recurso
        Private mID As String

        'Tipo del recurso
        Private mTipo As String

        'Nombre del recurso
        Private mNombre As String

        'Datos asociados al recurso para acceder a su almacen de datos
        Private mHtDatos As IDictionary(Of String, Object)
#End Region

#Region "Constructores"
        ''' <summary>Constructor por defecto.</summary>
        ''' <param name="pId" type="String">
        ''' ID del recurso.
        ''' transaccion.
        ''' </param>
        ''' <param name="pNombre" type="String">
        ''' Nombre del recurso.
        ''' </param>
        ''' <param name="pTipo" type="String">
        ''' Tipo del recurso.
        ''' </param>
        ''' <param name="pHTDatos" type="Hashtable">
        ''' Datos adicionales del recurso para acceder al almacen de datos.
        ''' </param>
        Public Sub New(ByVal pId As String, ByVal pNombre As String, ByVal pTipo As String, ByVal pHTDatos As IDictionary(Of String, Object))
            mID = pId
            mTipo = pTipo
            mHtDatos = pHTDatos
            mNombre = pNombre
        End Sub
#End Region

#Region "Propiedades"
        ''' <summary>Obtiene o asigna el id del recurso.</summary>
        ''' <remarks>No se puede asignar el id.</remarks>
        Public Property ID() As String Implements IRecursoLN.ID
            Get
                Return mID
            End Get
            Set(ByVal Value As String)
                Throw New NotSupportedException
            End Set
        End Property

        ''' <summary>Obtiene o asigna el nombre del recurso.</summary>
        ''' <remarks>No se puede asignar el nombre.</remarks>
        Public Property Nombre() As String Implements IRecursoLN.Nombre
            Get
                Return Me.mNombre
            End Get
            Set(ByVal Value As String)
                Throw New NotSupportedException
            End Set
        End Property

        ''' <summary>Obtiene o asigna el tipo del recurso.</summary>
        ''' <remarks>No se puede asignar el tipo.</remarks>
        Public Property Tipo() As String Implements IRecursoLN.Tipo
            Get
                Return Me.mTipo
            End Get
            Set(ByVal Value As String)
                Throw New NotSupportedException
            End Set
        End Property

        ''' <summary>Obtiene o asigna un dato del recurso.</summary>
        ''' <remarks>No se puede asignar un dato (lanza una NotSupportedException).</remarks>
        Public Property Dato(ByVal key As String) As Object Implements IRecursoLN.Dato
            Get
                Return Me.mHtDatos.Item(key)
            End Get
            Set(ByVal Value As Object)
                Throw New NotSupportedException
            End Set
        End Property
#End Region

    End Class
End Namespace
