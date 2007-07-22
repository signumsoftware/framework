Imports Framework.DatosNegocio

''' <summary>
''' Clase provisional para la carga de datos de AMV a la espera de realizar el módulo para el departamento comercial
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class DatosColaboradorDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mRetencionIRPF As Integer
    Protected mVerComision As Boolean
    Protected mTRSinPeritacionNuevo As Boolean
    Protected mTRSinPeritacionOcasion As Boolean
    Protected mSuper As Boolean
    Protected mPerSuper As Boolean
    Protected mFechaCreacion As Date
    Protected mFechaAlta As Date
    Protected mFechaBaja As Date
    Protected mMotivoBaja As String
    Protected mGerente As String
    Protected mVendedor1 As String
    Protected mVendedor2 As String
    Protected mVendedor3 As String
    Protected mPersonaConfianza As String
    Protected mHorario As String
    Protected mMarcas As String
    Protected mModelos As String
    Protected mCompetencia As String
    Protected mAseguradoras As String
    Protected mMediaVenta As String
    Protected mOcasion As String
    Protected mFormaCobro As String

#End Region

#Region "Propiedades"

    Public Property VerComision() As Boolean
        Get
            Return mVerComision
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mVerComision)
        End Set
    End Property
    
    Public Property TRSinPeritacionNuevo() As Boolean
        Get
            Return mTRSinPeritacionNuevo
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mTRSinPeritacionNuevo)
        End Set
    End Property

    Public Property TRSinPeritacionOcasion() As Boolean
        Get
            Return mTRSinPeritacionOcasion
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mTRSinPeritacionOcasion)
        End Set
    End Property

    Public Property Super() As Boolean
        Get
            Return mSuper
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mSuper)
        End Set
    End Property

    Public Property PerSuper() As Boolean
        Get
            Return mPerSuper
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mPerSuper)
        End Set
    End Property

    Public Property FechaCreacion() As Date
        Get
            Return mFechaCreacion
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaCreacion)
        End Set
    End Property

    Public Property FechaAlta() As Date
        Get
            Return mFechaAlta
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaAlta)
        End Set
    End Property

    Public Property FechaBaja() As Date
        Get
            Return mFechaBaja
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaBaja)
        End Set
    End Property

    Public Property MotivoBaja() As String
        Get
            Return mMotivoBaja
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mMotivoBaja)
        End Set
    End Property

    Public Property Gerente() As String
        Get
            Return mGerente
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGerente)
        End Set
    End Property

    Public Property Vendedor1() As String
        Get
            Return mVendedor1
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mVendedor1)
        End Set
    End Property

    Public Property Vendedor2() As String
        Get
            Return mVendedor2
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mVendedor2)
        End Set
    End Property

    Public Property Vendedor3() As String
        Get
            Return mVendedor3
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mVendedor3)
        End Set
    End Property

    Public Property PersonaConfianza() As String
        Get
            Return mPersonaConfianza
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mPersonaConfianza)
        End Set
    End Property

    Public Property Horario() As String
        Get
            Return mHorario
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mHorario)
        End Set
    End Property

    Public Property Marcas() As String
        Get
            Return mMarcas
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mMarcas)
        End Set
    End Property

    Public Property Modelos() As String
        Get
            Return mModelos
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mModelos)
        End Set
    End Property

    Public Property Competencia() As String
        Get
            Return mCompetencia
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mCompetencia)
        End Set
    End Property

    Public Property Aseguradoras() As String
        Get
            Return mAseguradoras
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mAseguradoras)
        End Set
    End Property

    Public Property MediaVenta() As String
        Get
            Return mMediaVenta
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mMediaVenta)
        End Set
    End Property

    Public Property Ocasion() As String
        Get
            Return mOcasion
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mOcasion)
        End Set
    End Property

    Public Property FormaCobro() As String
        Get
            Return mFormaCobro
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mFormaCobro)
        End Set
    End Property


#End Region

End Class
