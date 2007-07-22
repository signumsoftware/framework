#Region "Importacione"
Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales
#End Region

<Serializable()> _
Public Class ResponsableAgrupacionDeEmpresasDN
    Inherits EntidadDN
    Implements IResponsableDN

#Region "Atributos"
    Protected mEntidadResponsable As IEntidadDN
    Protected mAgrupacionEmpresasACargoDN As AgrupacionDeEmpresasDN
    Protected mPeriodo As IntervaloFechasDN
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    'Public Sub New(ByVal pResponsable As EmpleadoDN)
    '    Dim mensaje As String = ""
    '    If ValidarDatosResponsable(mensaje, pResponsable) Then
    '        Me.CambiarValorRef(Of IEntidadDN)(pResponsable, mEntidadResponsable)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

    Public Sub New(ByVal pEntidadResponsable As IEntidadDN, ByVal pAgrupacionEmpresasACargoDN As AgrupacionDeEmpresasDN, ByVal pPeriodo As IntervaloFechasDN)
        Dim mensaje As String = ""
        If ValidarDatosResponsable(mensaje, pEntidadResponsable) Then
            Me.CambiarValorRef(Of IEntidadDN)(pEntidadResponsable, mEntidadResponsable)
        Else
            Throw New Exception(mensaje)
        End If

        If ValidarAgrupacionEmpresasACargo(mensaje, pAgrupacionEmpresasACargoDN) Then
            Me.CambiarValorRef(Of AgrupacionDeEmpresasDN)(pAgrupacionEmpresasACargoDN, mAgrupacionEmpresasACargoDN)
        Else
            Throw New Exception(mensaje)
        End If
        Me.CambiarValorRef(Of IntervaloFechasDN)(pPeriodo, mPeriodo)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"
    Protected Overrides Property BajaPersistente() As Boolean
        Get
            Return MyBase.BajaPersistente
        End Get
        Set(ByVal value As Boolean)

            If value Then
                Me.mPeriodo.FFinal = Now
            Else
                Throw New ApplicationException("el proceso de baja no es reversible")
            End If
            MyBase.BajaPersistente = value
        End Set
    End Property

    Public Overridable Property EntidadResponsableDN() As Framework.DatosNegocio.IEntidadDN Implements IResponsableDN.EntidadResponsableDN
        Get
            Return mEntidadResponsable
        End Get
        Set(ByVal value As Framework.DatosNegocio.IEntidadDN)
            Dim mensaje As String = ""
            If ValidarDatosResponsable(mensaje, value) Then
                Me.CambiarValorRef(Of IEntidadDN)(value, mEntidadResponsable)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public Property AgrupacionEmpresasACargoDN() As AgrupacionDeEmpresasDN
        Get
            Return mAgrupacionEmpresasACargoDN
        End Get
        Set(ByVal value As AgrupacionDeEmpresasDN)
            Dim mensaje As String = ""
            If ValidarAgrupacionEmpresasACargo(mensaje, value) Then
                Me.CambiarValorRef(Of AgrupacionDeEmpresasDN)(value, mAgrupacionEmpresasACargoDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public ReadOnly Property ClonColEntidadesACargoDN() As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadDN) Implements IResponsableDN.ClonColEntidadesACargoDN
        Get
            If mAgrupacionEmpresasACargoDN Is Nothing OrElse mAgrupacionEmpresasACargoDN.ColEmpresasDN Is Nothing Then
                Return Nothing
            Else
                Return New List(Of Framework.DatosNegocio.IEntidadDN)(mAgrupacionEmpresasACargoDN.ColEmpresasDN.ToArray())
            End If
        End Get
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarDatosResponsable(ByRef mensaje As String, ByVal pEntidadResponsable As Framework.DatosNegocio.IEntidadDN) As Boolean Implements IResponsableDN.ValidarDatosResponsable
        If pEntidadResponsable Is Nothing Then
            mensaje = "Todo responsable de una agrupación debe contener una entidad"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarAgrupacionEmpresasACargo(ByRef mensaje As String, ByVal pAgrupacionEmpresasACargo As AgrupacionDeEmpresasDN) As Boolean
        If pAgrupacionEmpresasACargo Is Nothing Then
            mensaje = "Todo responsable debe tener una agrupación de empresas"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarEntidadesACargo(ByRef mensaje As String, ByVal pColEntidadesACargoDN As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadDN)) As Boolean Implements IResponsableDN.ValidarEntidadesACargo
        If pColEntidadesACargoDN Is Nothing OrElse pColEntidadesACargoDN.Count < 1 Then
            mensaje = "Todo responsable de una agrupación debe tener una colección de empresas"
            Return False
        End If
        Return True
    End Function

#End Region

End Class