Imports Framework.DatosNegocio

<Serializable()> _
Public Class GrupoFraccionamientosDN
    Inherits EntidadDN

    Protected mColGrupoPagosF As ColGrupoPagosFraccionadosDN

    Public Sub New()
        MyBase.New()
        mColGrupoPagosF = New ColGrupoPagosFraccionadosDN()
    End Sub

    Public Property ColGrupoPagosF() As ColGrupoPagosFraccionadosDN
        Get
            Return mColGrupoPagosF
        End Get
        Set(ByVal value As ColGrupoPagosFraccionadosDN)
            CambiarValorCol(Of ColGrupoPagosFraccionadosDN)(value, mColGrupoPagosF)
        End Set
    End Property

    Public Overrides Function ToXML() As String
        Return ToXML(GetType(GrupoFraccionamientosXML))
    End Function

    Public Overrides Function FromXML(ByVal ptr As System.IO.TextReader) As Object
        Return FromXML(GetType(GrupoFraccionamientosXML), ptr)
    End Function

    ''' <summary>
    ''' Devuelve el número de pagos más alto de entre todos los tipos de fraccionamiento
    ''' que contienen los grupos de pagos fraccionados que contiene el objeto
    ''' </summary>
    Public ReadOnly Property MaximoNumeroPagos() As Integer
        Get
            Dim valorMaximo As Integer
            If Not Me.mColGrupoPagosF Is Nothing Then
                For Each gpf As GrupoPagosFraccionadosDN In Me.mColGrupoPagosF
                    Dim np As Integer = gpf.TipoFraccionamiento.NumeroPagos
                    If np > valorMaximo Then
                        valorMaximo = np
                    End If
                Next
            End If
            Return valorMaximo
        End Get
    End Property

End Class


Public Class GrupoFraccionamientosXML
    Implements Framework.DatosNegocio.IXMLAdaptador

    Public ColGrupoPagosF As New List(Of GrupoPagosFraccionadosXML)

    Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim entidad As GrupoFraccionamientosDN
        entidad = pEntidad

        entidad.ColGrupoPagosF.ToListIXMLAdaptador(New GrupoPagosFraccionadosXML(), ColGrupoPagosF)

    End Sub

    Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto
        Dim entidad As GrupoFraccionamientosDN
        entidad = pEntidad

        For Each gpfxml As GrupoPagosFraccionadosXML In ColGrupoPagosF
            Dim gpf As New GrupoPagosFraccionadosDN()
            gpfxml.XMLAdaptadorToObjeto(gpf)
            entidad.ColGrupoPagosF.Add(gpf)
        Next

    End Sub
End Class