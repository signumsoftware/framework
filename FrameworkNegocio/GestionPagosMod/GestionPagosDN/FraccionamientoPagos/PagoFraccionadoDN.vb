Imports Framework.DatosNegocio

<Serializable()> _
Public Class PagoFraccionadoDN
    Inherits EntidadDN

    Protected mNumOrdenPago As Integer
    Protected mImporte As Double

    Public Property NumOrdenPago() As Integer
        Get
            Return mNumOrdenPago
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mNumOrdenPago)
        End Set
    End Property

    Public Property Importe() As Double
        Get
            Return mImporte
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mImporte)
        End Set
    End Property

    Public Overrides Function ToString() As String
        Return mImporte.ToString("C")
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class


<Serializable()> _
Public Class ColPagoFraccionadoDN
    Inherits ArrayListValidable(Of PagoFraccionadoDN)

    Public ReadOnly Property ListaOrdenada() As List(Of PagoFraccionadoDN)
        Get
            Dim lista As New List(Of PagoFraccionadoDN)
            Dim sl As New SortedList()
            For Each pf As PagoFraccionadoDN In Me
                sl.Add(pf.NumOrdenPago, pf)
            Next
            Dim e As IDictionaryEnumerator = sl.GetEnumerator
            While e.MoveNext
                lista.Add(e.Value)
            End While
            Return lista
        End Get
    End Property

End Class


Public Class PagoFraccionadoXML
    Implements Framework.DatosNegocio.IXMLAdaptador

    <Xml.Serialization.XmlAttribute()> Public NumOrdenPago As Integer
    <Xml.Serialization.XmlAttribute()> Public Importe As Double

    Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim entidad As PagoFraccionadoDN
        entidad = pEntidad

        NumOrdenPago = entidad.NumOrdenPago
        Importe = entidad.Importe

    End Sub

    Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto
        Dim entidad As PagoFraccionadoDN
        entidad = pEntidad

        entidad.NumOrdenPago = NumOrdenPago
        entidad.Importe = Importe

    End Sub

End Class