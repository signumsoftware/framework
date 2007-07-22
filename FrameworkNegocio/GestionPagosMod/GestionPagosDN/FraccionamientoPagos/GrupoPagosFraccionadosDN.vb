Imports Framework.DatosNegocio

<Serializable()> _
Public Class GrupoPagosFraccionadosDN
    Inherits EntidadDN

    Protected mColPagoFraccionadoDN As ColPagoFraccionadoDN
    Protected mTipoFraccionamiento As FraccionamientoDN

    Public Sub New()
        MyBase.New()
        mColPagoFraccionadoDN = New ColPagoFraccionadoDN()
    End Sub

    Public Property ColPagoFraccionadoDN() As ColPagoFraccionadoDN
        Get
            Return mColPagoFraccionadoDN
        End Get
        Set(ByVal value As ColPagoFraccionadoDN)
            CambiarValorCol(Of ColPagoFraccionadoDN)(value, mColPagoFraccionadoDN)
        End Set
    End Property

    Public Property TipoFraccionamiento() As FraccionamientoDN
        Get
            Return mTipoFraccionamiento
        End Get
        Set(ByVal value As FraccionamientoDN)
            CambiarValorRef(Of FraccionamientoDN)(value, mTipoFraccionamiento)
        End Set
    End Property

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If mTipoFraccionamiento IsNot Nothing Then
            cadena = mTipoFraccionamiento.ToString() & " / "
        End If

        If mColPagoFraccionadoDN IsNot Nothing Then
            For Each pf As PagoFraccionadoDN In mColPagoFraccionadoDN
                cadena = cadena & pf.ToString() & "; "
            Next
        End If

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mColPagoFraccionadoDN Is Nothing Then
            pMensaje = "La colección de pagos fraccionados no puede ser nula"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mTipoFraccionamiento Is Nothing Then
            pMensaje = "El tipo de fraccionamiento no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mColPagoFraccionadoDN.Count <> mTipoFraccionamiento.NumeroPagos Then
            pMensaje = "El número de pagos debe coincidir con el tipo de fraccionamiento"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class


<Serializable()> _
Public Class ColGrupoPagosFraccionadosDN
    Inherits ArrayListValidable(Of GrupoPagosFraccionadosDN)

    ''' <summary>
    ''' Devuelve todos los valores de la colección ordenados en función del
    ''' número de pagos
    ''' </summary>
    Public ReadOnly Property ListaOrdenada() As List(Of GrupoPagosFraccionadosDN)
        Get
            Dim lista As New List(Of GrupoPagosFraccionadosDN)()
            Dim sl As New SortedList()
            For Each gp As GrupoPagosFraccionadosDN In Me
                sl.Add(gp.TipoFraccionamiento.NumeroPagos, gp)
            Next
            Dim e As IDictionaryEnumerator = sl.GetEnumerator
            While e.MoveNext
                lista.Add(e.Value)
            End While
            Return lista
        End Get
    End Property

    Public Function RecuperaGruposPagosFracSuperanLimite(ByVal colLimites As ColLimiteMinFraccionamientoDN, ByVal importeTarifa As Double) As ColGrupoPagosFraccionadosDN
        Dim colGPF As New ColGrupoPagosFraccionadosDN()

        If colLimites Is Nothing Then
            Return Me
        End If

        For Each gpf As FN.GestionPagos.DN.GrupoPagosFraccionadosDN In Me
            Dim limiteAlcanzado As Boolean = True
            For Each lim As FN.GestionPagos.DN.LimiteMinFraccionamientoDN In colLimites
                If lim.Fraccionamiento.GUID = gpf.TipoFraccionamiento.GUID AndAlso importeTarifa < lim.ValorMinimoFrac Then
                    limiteAlcanzado = False
                    Exit For
                End If
            Next

            If limiteAlcanzado Then
                colGPF.Add(gpf)
            End If

        Next

        Return colGPF

    End Function

End Class


Public Class GrupoPagosFraccionadosXML
    Implements Framework.DatosNegocio.IXMLAdaptador

    Public TipoFraccionamiento As New FraccionamientoXML()
    Public ColPagoFraccionado As New List(Of PagoFraccionadoXML)


    Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim entidad As GrupoPagosFraccionadosDN
        entidad = pEntidad

        TipoFraccionamiento.ObjetoToXMLAdaptador(entidad.TipoFraccionamiento)
        entidad.ColPagoFraccionadoDN.ToListIXMLAdaptador(New PagoFraccionadoXML(), ColPagoFraccionado)

    End Sub

    Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) Implements Framework.DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto
        Dim entidad As GrupoPagosFraccionadosDN
        entidad = pEntidad

        Dim tf As New FraccionamientoDN()
        TipoFraccionamiento.XMLAdaptadorToObjeto(tf)

        entidad.TipoFraccionamiento = tf

        For Each pfxml As PagoFraccionadoXML In ColPagoFraccionado
            Dim pf As New PagoFraccionadoDN()
            pfxml.XMLAdaptadorToObjeto(pf)
            entidad.ColPagoFraccionadoDN.Add(pf)
        Next

    End Sub

End Class