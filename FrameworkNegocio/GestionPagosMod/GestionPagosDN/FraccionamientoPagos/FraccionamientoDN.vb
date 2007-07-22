Imports Framework.DatosNegocio

<Serializable()> _
Public Class FraccionamientoDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mNumeroPagos As Integer
    Protected mFrecuenciaMensual As Integer

#End Region

#Region "Propiedades"

    Public Property NumeroPagos() As Integer
        Get
            Return mNumeroPagos
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mNumeroPagos)
        End Set
    End Property

    Public Property FrecuenciaMensual() As Integer
        Get
            Return mFrecuenciaMensual
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mFrecuenciaMensual)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If Not String.IsNullOrEmpty(mNombre) Then
            cadena = mNombre & " "
        End If

        Return MyBase.ToString()
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If String.IsNullOrEmpty(mNombre) Then
            pMensaje = "El nombre del tipo de fraccionamiento no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mNumeroPagos <= 0 Then
            pMensaje = "El número de importes debe ser mayor que 0"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function
#End Region


End Class


Public Class FraccionamientoXML
    Implements Framework.DatosNegocio.IXMLAdaptador

    <Xml.Serialization.XmlAttribute()> Public NumeroPagos As Integer
    <Xml.Serialization.XmlAttribute()> Public FrecuenciaMensual As Integer
    <Xml.Serialization.XmlAttribute()> Public Nombre As String


    Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim entidad As FraccionamientoDN
        entidad = pEntidad

        NumeroPagos = entidad.NumeroPagos
        FrecuenciaMensual = entidad.FrecuenciaMensual
        Nombre = entidad.Nombre

    End Sub

    Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto
        Dim entidad As FraccionamientoDN
        entidad = pEntidad

        entidad.NumeroPagos = NumeroPagos
        entidad.FrecuenciaMensual = FrecuenciaMensual
        entidad.Nombre = Nombre

    End Sub
End Class


<Serializable()> _
Public Class ColFraccionamientoDN
    Inherits ArrayListValidable(Of FraccionamientoDN)

#Region "Propiedades"

    ''' <summary>
    ''' Devuelve todos los valores de la colección ordenados en función del
    ''' número de pagos
    ''' </summary>
    Public ReadOnly Property ListaOrdenada() As List(Of FraccionamientoDN)
        Get
            Dim lista As New List(Of FraccionamientoDN)()
            Dim sl As New SortedList()

            For Each fr As FraccionamientoDN In Me
                sl.Add(fr.NumeroPagos, fr)
            Next

            Dim e As IDictionaryEnumerator = sl.GetEnumerator
            While e.MoveNext
                lista.Add(e.Value)
            End While

            Return lista
        End Get
    End Property

#End Region


End Class