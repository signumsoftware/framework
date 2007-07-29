Imports Framework.DatosNegocio
Imports Framework.Ficheros.FicherosDN

<Serializable()> _
Public Class AntecedentesDN
    Inherits EntidadTemporalDN

    Protected mColTipoDocumento As ColTipoFicheroDN
    Protected mCategoria As CategoriaDN
    Protected mAnyosSinSiniestro As Integer
    Protected mNivelBonificacion As Integer

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorCol(Of ColTipoFicheroDN)(New ColTipoFicheroDN(), mColTipoDocumento)
    End Sub

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mColTipoDocRequerido")> _
    Public Property ColTipoDocRequerido() As ColTipoFicheroDN
        Get
            Return mColTipoDocumento
        End Get
        Set(ByVal value As ColTipoFicheroDN)
            CambiarValorCol(Of ColTipoFicheroDN)(value, mColTipoDocumento)
        End Set
    End Property

    <RelacionPropCampoAtribute("mCategoria")> _
    Public Property Categoria() As CategoriaDN
        Get
            Return mCategoria
        End Get
        Set(ByVal value As CategoriaDN)
            CambiarValorRef(Of CategoriaDN)(value, mCategoria)
        End Set
    End Property

    Public Property AnyosSinSiniestro() As Integer
        Get
            Return mAnyosSinSiniestro
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mAnyosSinSiniestro)
        End Set
    End Property

    Public Property NivelBonificacion() As Integer
        Get
            Return mNivelBonificacion
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mNivelBonificacion)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mCategoria Is Nothing Then
            pMensaje = "La categoría de no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

#End Region

End Class


<Serializable()> _
Public Class ColAntecedentesDN
    Inherits ArrayListValidableEntTemp(Of AntecedentesDN)

    Public Function RecuperarAnyosSinSiniestro(ByVal categoria As CategoriaDN, ByVal justificante As Justificantes, ByVal anyosSinSiniestro As Integer, ByVal fechaEfecto As Date) As ColAntecedentesDN
        Dim colAntTemp As ColAntecedentesDN = New ColAntecedentesDN()
        colAntTemp.AddRangeObject(Me.RecuperarContienenFecha(fechaEfecto))
        Dim colAntResp As ColAntecedentesDN = New ColAntecedentesDN()

        For Each ant As AntecedentesDN In colAntTemp
            If ant.Categoria.GUID = categoria.GUID AndAlso ant.ColTipoDocRequerido.Count = justificante AndAlso ant.AnyosSinSiniestro = anyosSinSiniestro Then
                colAntResp.AddUnico(ant)
            End If
        Next

        Return colAntResp

    End Function

End Class