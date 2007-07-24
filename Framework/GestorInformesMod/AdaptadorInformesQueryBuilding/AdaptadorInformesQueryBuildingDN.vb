Imports Framework.DatosNegocio
Imports System.Data

<Serializable()> _
Public Class AdaptadorInformesQueryBuildingDN
    Inherits EntidadDN

    Public Sub New()

    End Sub

    Protected mTokenTipo As String
    Protected mPlantilla As ContenedorPlantilla.DN.ContenedorPlantillaDN
    Protected mTablasPrincipales As ColTablaPrincipalAIQB

#Region "propiedades"
    Public Property TokenTipo() As System.Type
        Get
            Dim tipo As System.Type = Nothing
            Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(Me.mTokenTipo, Nothing, tipo)
            Return tipo
        End Get
        Set(ByVal value As System.Type)
            Me.CambiarValorVal(Of String)(Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.TipoToString(value), Me.mTokenTipo)
        End Set
    End Property

    Public Property Plantilla() As ContenedorPlantilla.DN.ContenedorPlantillaDN
        Get
            Return Me.mPlantilla
        End Get
        Set(ByVal value As ContenedorPlantilla.DN.ContenedorPlantillaDN)
            Me.CambiarValorRef(Of ContenedorPlantilla.DN.ContenedorPlantillaDN)(value, Me.mPlantilla)
        End Set
    End Property

    Public Property TablasPrincipales() As ColTablaPrincipalAIQB
        Get
            Return Me.mTablasPrincipales
        End Get
        Set(ByVal value As ColTablaPrincipalAIQB)
            Me.CambiarValorCol(Of ColTablaPrincipalAIQB)(value, Me.mTablasPrincipales)
        End Set
    End Property
#End Region

End Class

<Serializable()> _
Public Class TablaPrincipalAIQB
    Inherits EntidadDN
    Implements ITabla

    Protected mNombreTabla As String
    Protected mNombreTablaBD As String

    Private mSQLDefinicion As String
    Private mParametros As List(Of System.Data.IDataParameter)
    Private mfkTabla As String

    'Protected mFiltroSeleccion As MotorBusquedaDN.FiltroDN
    Protected mTablasRelacionadas As ColTablaRelacionadaAIQB

#Region "propiedades"
    Public Property NombreTabla() As String Implements ITabla.NombreTabla
        Get
            Return Me.mNombreTabla
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreTabla)
        End Set
    End Property

    Public Sub CargarDatosSelect(ByVal fkTabla As String, ByVal sql As String, ByVal parametros As System.Data.IDataParameter)
        Me.mSQLDefinicion = sql
        Me.mParametros = parametros
        Me.mfkTabla = fkTabla
    End Sub

    Public Sub GenerarSQL(ByRef SQL As String, ByRef ColParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) Implements ITabla.GenerarSQL
        If String.IsNullOrEmpty(Me.mNombreTablaBD) Then
            Throw New ApplicationExceptionDN("El NombreTablaBD de la Tabla Principal AIQB no se ha definido")
        End If

        'puede estar vacío si se está invocando para generar el EsquemaXML
        If String.IsNullOrEmpty(Me.mfkTabla) AndAlso String.IsNullOrEmpty(Me.mSQLDefinicion) Then
            SQL = "SELECT * FROM " & Me.mNombreTablaBD
            ColParametros = Nothing
            Exit Sub
        End If

        'genera el Join en tiempo real
        If String.IsNullOrEmpty(Me.mfkTabla) Then
            Throw New ApplicationExceptionDN("El Foreign Key de la tabla con la consulta de selección de la Tabla Principal AIQB no se ha definido (debe invocarse el método CargarDatosSelect antes de ejecutar la generación de la consulta)")
        End If
        If String.IsNullOrEmpty(Me.mSQLDefinicion) Then
            Throw New ApplicationExceptionDN("La consulta de selección de la Tabla Principal AIQB no se ha definido (debe invocarse el método CargarDatosSelect antes de ejecutar la generación de la consulta)")
        End If

        Dim misql As New System.Text.StringBuilder()
        misql.Append("SELECT * FROM ")
        misql.Append(Me.mNombreTablaBD)
        misql.Append(" WHERE ")
        misql.Append("(")
        misql.Append(Me.mfkTabla)
        misql.Append(" IN (")
        misql.Append(Me.mSQLDefinicion)
        misql.Append("))")
        SQL = misql.ToString()
        ColParametros = Me.mParametros
    End Sub

    Public Property NombreTablaBD() As String Implements ITabla.NombreTablaBD
        Get
            Return Me.mNombreTablaBD
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreTablaBD)
        End Set
    End Property

    'Public Property FiltrosSeleccion() As MotorBusquedaDN.FiltroDN Implements ITabla.FiltrosSeleccion
    '    Get
    '        Return Me.mFiltroSeleccion
    '    End Get
    '    Set(ByVal value As MotorBusquedaDN.FiltroDN)
    '        Me.CambiarValorRef(Of MotorBusquedaDN.FiltroDN)(value, Me.mFiltroSeleccion)
    '    End Set
    'End Property

    Public Property TablasRelacionadas() As ColTablaRelacionadaAIQB Implements ITabla.TablasRelacionadas
        Get
            Return Me.mTablasRelacionadas
        End Get
        Set(ByVal value As ColTablaRelacionadaAIQB)
            Me.CambiarValorCol(Of ColTablaRelacionadaAIQB)(value, Me.mTablasRelacionadas)
            If Not Me.mTablasRelacionadas Is Nothing Then
                For Each t As TablaRelacionadaAIQB In Me.mTablasRelacionadas
                    t.TablaPadre = Me
                Next
            End If
        End Set
    End Property
#End Region



End Class

<Serializable()> _
Public Class ColTablaPrincipalAIQB
    Inherits ArrayListValidable(Of TablaPrincipalAIQB)
End Class

<Serializable()> _
Public Class TablaRelacionadaAIQB
    Inherits EntidadDN
    Implements ITabla

    Protected mNombreRelacion As String
    Protected mNombreTabla As String
    Protected mTablaPadre As ITabla
    Protected mfkPadre As String
    Protected mfkPropio As String
    Protected mNombreTablaBD As String
    'Protected mFiltroSeleccion As MotorBusquedaDN.FiltroDN
    Protected mTablasRelacionadas As ColTablaRelacionadaAIQB

#Region "propiedades"
#Region "implementación de ITabla"
    Public Property NombreTabla() As String Implements ITabla.NombreTabla
        Get
            Return Me.mNombreTabla
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreTabla)
        End Set
    End Property

    'Public Property FiltrosSeleccion() As MotorBusquedaDN.FiltroDN Implements ITabla.FiltrosSeleccion
    '    Get
    '        Return Me.mFiltroSeleccion
    '    End Get
    '    Set(ByVal value As MotorBusquedaDN.FiltroDN)
    '        Me.CambiarValorRef(Of MotorBusquedaDN.FiltroDN)(value, Me.mFiltroSeleccion)
    '    End Set
    'End Property

    Public Property TablasRelacionadas() As ColTablaRelacionadaAIQB Implements ITabla.TablasRelacionadas
        Get
            Return Me.mTablasRelacionadas
        End Get
        Set(ByVal value As ColTablaRelacionadaAIQB)
            Me.CambiarValorCol(Of ColTablaRelacionadaAIQB)(value, Me.mTablasRelacionadas)
        End Set
    End Property

    Public Sub GenerarSQL(ByRef SQL As String, ByRef ColParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) Implements ITabla.GenerarSQL
        Dim misql As New System.Text.StringBuilder()
        misql.Append("SELECT * FROM ")
        misql.Append(Me.mNombreTablaBD)
        misql.Append(" WHERE ")
        misql.Append("(")
        misql.Append(Me.mfkPropio)
        misql.Append(" IN (SELECT ")
        misql.Append(Me.mfkPadre)
        misql.Append(" FROM (")

        Dim sqlPadre As String = String.Empty
        Me.mTablaPadre.GenerarSQL(sqlPadre, ColParametros)

        misql.Append(sqlPadre)

        misql.Append(") AS derivedtbl_1))")

        SQL = misql.ToString()
    End Sub

    Public Property NombreTablaBD() As String Implements ITabla.NombreTablaBD
        Get
            Return Me.mNombreTablaBD
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreTablaBD)
        End Set
    End Property
#End Region

    Public Property TablaPadre() As ITabla
        Get
            Return Me.mTablaPadre
        End Get
        Set(ByVal value As ITabla)
            Me.CambiarValorRef(value, Me.mTablaPadre)
            If Not Me.mTablaPadre.TablasRelacionadas.Contains(Me) Then
                Me.mTablaPadre.TablasRelacionadas.Add(Me)
            End If
        End Set
    End Property

    Public Property NombreRelacion() As String
        Get
            Return Me.mNombreRelacion
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mNombreRelacion)
        End Set
    End Property

    Public Property fkPadre() As String
        Get
            Return Me.mfkPadre
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mfkPadre)
        End Set
    End Property

    Public Property fkPropio() As String
        Get
            Return Me.mfkPropio
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mfkPropio)
        End Set
    End Property
#End Region



End Class

<Serializable()> _
Public Class ColTablaRelacionadaAIQB
    Inherits ArrayListValidable(Of TablaRelacionadaAIQB)
End Class



Public Interface ITabla
    Property NombreTabla() As String
    Property NombreTablaBD() As String
    Sub GenerarSQL(ByRef SQL As String, ByRef ColParametros As List(Of System.Data.IDataParameter))
    'Property FiltrosSeleccion() As MotorBusquedaDN.FiltroDN
    Property TablasRelacionadas() As ColTablaRelacionadaAIQB
End Interface






