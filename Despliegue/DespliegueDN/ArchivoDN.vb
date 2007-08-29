'''Clase abstracta con informacion sobre un archivo (DLL o Generico)
<Serializable()> _
Public MustInherit Class ArchivoDN
    Implements IComparable

#Region "Atributos"
    Private mRuta As String
    Private mTam As Int64
    Private mHash As Byte()
#End Region

#Region "Propiedades"
    Public Property Ruta() As String
        Get
            Return mRuta
        End Get
        Set(ByVal Value as String)
            Dim mensaje As String = ""

            If (ValRuta(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mRuta = Value
        End Set
    End Property

    Public Property Tam() As Int64
        Get
            Return mTam
        End Get
        Set(ByVal Value As Int64)
            Dim mensaje As String = ""

            If (ValTam(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mTam = Value
        End Set
    End Property

    'Public Property Hash() As Byte()
    '    Get
    '        Return mHash
    '    End Get
    '    Set(ByVal Value As Byte())
    '        mHash = Value
    '    End Set
    'End Property
#End Region

#Region "Constructores"
    Public Sub New()

    End Sub

    Public Sub New(ByVal pRuta As String, ByVal pTam As Int64, ByVal pHash As Byte())
        Ruta = pRuta
        Tam = pTam
        mHash = pHash
    End Sub
#End Region

#Region "Metodos"
    Public Function Comparable(ByVal pObj As Object) As Boolean
        If ((pObj.GetType Is Me.GetType) And (CType(pObj, ArchivoDN).mRuta = Me.mRuta)) Then
            Return True
        End If

        Return False
    End Function
#End Region

#Region "Metodos Validacion"
    Public Shared Function ValRuta(ByVal pRuta As String, ByRef pMensaje As String) As Boolean
        If (pRuta Is Nothing OrElse pRuta = String.Empty) Then
            pMensaje = "Error: la ruta no puede ser nula o vacia"
            Return False
        End If

        If (pRuta.Chars(0) = "\") Then
            pMensaje = "Error: la ruta no es valida. No debe comenzar por el caracter \"
            Return False
        End If

        If (pRuta.IndexOfAny(System.IO.Path.GetInvalidPathChars()) <> -1) Then
            pMensaje = "Error: la ruta no es valida. Contiene caracteres invalidos"
            Return False
        End If

        Return True
    End Function

    Public Shared Function ValTam(ByVal pTam As Int64, ByRef pMensaje As String) As Boolean
        If (pTam < 0) Then
            pMensaje = "Error: El numero no puede ser negativo"
            Return False
        End If
        Return True
    End Function
#End Region

#Region "Metodos Abstractos"
    Public Overridable Function CompareTo(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo
        If obj.GetType() Is Me.GetType() Then
            Dim arch As ArchivoDN = CType(obj, ArchivoDN)
            Dim result As Int32
            result = Me.mHash.Length.CompareTo(arch.mHash.Length)
            If (result <> 0) Then
                Return result
            End If
            Dim i As Int32
            For i = 0 To Me.mHash.Length - 1
                result = mHash(i).CompareTo(arch.mHash(i))
                If (result <> 0) Then
                    Return result
                End If
            Next
            Return 0
        Else
            Return -1
        End If
    End Function
#End Region

End Class
