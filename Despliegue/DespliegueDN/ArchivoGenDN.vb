<Serializable()> _
Public Class ArchivoGenDN
    Inherits ArchivoDN

#Region "Atributos"
    Private mCreacion As Date
    Private mModificacion As Date
    Private mUltimoAcceso As Date
#End Region

#Region "Propiedades"
    Public Property Creacion() As Date
        Get
            Return mCreacion
        End Get
        Set(ByVal Value As Date)
            mCreacion = Value
        End Set
    End Property

    Public Property Modificacion() As Date
        Get
            Return mModificacion
        End Get
        Set(ByVal Value As Date)
            mModificacion = Value
        End Set
    End Property

    Public Property UltimoAcceso() As Date
        Get
            Return mUltimoAcceso
        End Get
        Set(ByVal Value As Date)
            mUltimoAcceso = Value
        End Set
    End Property
#End Region

#Region "Constructores"
    Public Sub New(ByVal pRuta As String, ByVal pTam As Int64, ByVal pHash As Byte(), ByVal pCreacion As Date, ByVal pModificacion As Date, ByVal pUltimoAcceso As Date)
        MyBase.New(pRuta, pTam, pHash)
        Creacion = pCreacion
        Modificacion = pModificacion
        UltimoAcceso = pUltimoAcceso
    End Sub

    Public Sub New()
        MyBase.New()
    End Sub
#End Region

#Region "Metodos"
    'Public Overrides Function CompareTo(ByVal obj As Object) As Integer
    '    Dim aux As Integer
    '    Dim objc As ArchivoGenDN

    '    If Not Me.Comparable(obj) Then
    '        Return 0
    '    End If

    '    objc = CType(obj, ArchivoGenDN)

    '    aux = Me.Creacion.CompareTo(objc.Creacion)
    '    If aux <> 0 Then
    '        aux = Me.Modificacion.CompareTo(objc.Modificacion)
    '        If aux <> 0 Then
    '            aux = Me.UltimoAcceso.CompareTo(objc.UltimoAcceso)
    '            If aux <> 0 Then
    '                aux = Me.Tam.CompareTo(objc.Tam)
    '            End If
    '        End If
    '    End If
    '    Return aux
    'End Function
#End Region

End Class
