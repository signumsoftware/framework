<Serializable()> _
Public Class ArchivoOrdenesDN

#Region "Atributos"
    Private mOrden As Orden
    Private mArchivo As ArchivoDN
#End Region

#Region "Constructores"
    Public Sub New()

    End Sub

    Public Sub New(ByVal pArchivo As ArchivoDN, ByVal pOrden As Orden)
        Dim mensaje As String = ""

        If (ValArchivo(pArchivo, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If

        mArchivo = pArchivo
        mOrden = pOrden
    End Sub
#End Region

#Region "Propiedades"
    Public Property Archivo() As ArchivoDN
        Get
            Return mArchivo
        End Get
        Set(ByVal Value As ArchivoDN)
            Dim mensaje As String = ""

            If (ValArchivo(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mArchivo = Value
        End Set
    End Property

    Public Property Orden() As Orden
        Get
            Return mOrden
        End Get
        Set(ByVal Value As Orden)
            mOrden = Value
        End Set
    End Property
#End Region


#Region "Metodos Validacion"
    Public Shared Function ValArchivo(ByVal pArchivo As ArchivoDN, ByRef pMensaje As String) As Boolean
        If (pArchivo Is Nothing) Then
            pMensaje = "Error: el archivo no puede ser nulo."
            Return False
        End If

        Return True
    End Function
#End Region

End Class

Public Enum Orden As Integer
    Actualizar = 0
    Crear = 1
    Borrar = 2
End Enum
