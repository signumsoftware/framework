
Public Interface IFormateadorDatosACanal(Of T, k)
    ReadOnly Property MiTipoEnTexto() As String
    ReadOnly Property TipoDatosFuenteEnTexto() As String
    ReadOnly Property TipoDatosDestinoEnTexto() As String
    ReadOnly Property TipoDatosFuente() As System.Type
    ReadOnly Property TipoDatosDestino() As System.Type
    ReadOnly Property TipoCanal() As String
    Function Procesar(ByVal elemento As T) As k

End Interface

<Serializable()> _
Public MustInherit Class FormateadorDatosACanalBase(Of T, k)
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IFormateadorDatosACanal(Of T, k)




    Public mTipoCanal As String
    Private mMiTipoEnTexto As String
    Private mTipoDatosFuenteEnTexto As String
    Private mTipoDatosDestinoEnTexto As String

    Public Sub New()
        mMiTipoEnTexto = Me.GetType.ToString
        mTipoDatosFuenteEnTexto = GetType(T).ToString
        mTipoDatosDestinoEnTexto = GetType(k).ToString

    End Sub


    Public ReadOnly Property MiTipoEnTexto() As String Implements IFormateadorDatosACanal(Of T, k).MiTipoEnTexto
        Get
            Return mMiTipoEnTexto
        End Get
    End Property

    Public Function Procesar(ByVal elemento As T) As k Implements IFormateadorDatosACanal(Of T, k).Procesar

    End Function

    Public ReadOnly Property TipoDatosFuenteEnTexto() As String Implements IFormateadorDatosACanal(Of T, k).TipoDatosFuenteEnTexto
        Get
            Return mTipoDatosFuenteEnTexto
        End Get
    End Property

    Public ReadOnly Property TipoDatosDestinoEnTexto() As String Implements IFormateadorDatosACanal(Of T, k).TipoDatosDestinoEnTexto
        Get
            Return mTipoDatosDestinoEnTexto
        End Get
    End Property



    Public ReadOnly Property TipoDatosDestino() As System.Type Implements IFormateadorDatosACanal(Of T, k).TipoDatosDestino
        Get
            Return GetType(k)

        End Get
    End Property

    Public ReadOnly Property TipoDatosFuente() As System.Type Implements IFormateadorDatosACanal(Of T, k).TipoDatosFuente
        Get
            Return GetType(T)
        End Get
    End Property

    Public ReadOnly Property TipoCanal() As String Implements IFormateadorDatosACanal(Of T, k).TipoCanal
        Get
            Return Me.mTipoCanal
        End Get
    End Property
End Class