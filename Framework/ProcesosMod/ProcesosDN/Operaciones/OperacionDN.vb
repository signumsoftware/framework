Imports Framework.DatosNegocio
Imports Framework.TiposYReflexion.DN

<Serializable()> Public Class OperacionDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IOperacionDN


    Protected mVerboOperacion As VerboDN
    Protected mRutaIcono As String
    Protected mComentario As String
    Protected mTrazarOperacion As Boolean
    Protected mColDNAceptadas As ColVinculoClaseDN
    Protected mObjetoIndirectoNoModificable As Boolean

    ' Protected mEjecutaMetodo As Boolean

    Public Sub New()
        ' CambiarValorVal(Of Boolean)(True, mEjecutaMetodo)

        Me.CambiarValorRef(Of ColVinculoClaseDN)(New ColVinculoClaseDN, mColDNAceptadas)
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub






    'Public Property EjecutaMetodo() As Boolean

    '    Get
    '        Return mEjecutaMetodo
    '    End Get

    '    Set(ByVal value As Boolean)
    '        CambiarValorVal(Of Boolean)(value, mEjecutaMetodo)

    '    End Set
    'End Property





    ''' <summary>
    ''' lso tipo de dn a los que se puede aplicar esta operacion
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ColDNAceptadas() As ColVinculoClaseDN
        Get
            Return mColDNAceptadas
        End Get
        Set(ByVal value As ColVinculoClaseDN)
            Me.CambiarValorRef(Of ColVinculoClaseDN)(value, mColDNAceptadas)
        End Set
    End Property

    Public Property VerboOperacion() As VerboDN Implements IOperacionDN.VerboOperacion
        Get
            Return mVerboOperacion
        End Get
        Set(ByVal value As VerboDN)
            CambiarValorRef(Of VerboDN)(value, mVerboOperacion)

            If mVerboOperacion IsNot Nothing AndAlso String.IsNullOrEmpty(Me.mNombre) Then
                Me.mNombre = Me.mVerboOperacion.Nombre
            End If

        End Set
    End Property

    Public Property RutaIcono() As String
        Get
            Return mRutaIcono
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mRutaIcono)
        End Set
    End Property

    Public Property Comentario() As String
        Get
            Return mComentario
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mComentario)
        End Set
    End Property

    Public Property TrazarOperacion() As Boolean
        Get
            Return mTrazarOperacion
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mTrazarOperacion)
        End Set
    End Property

    Public Property ObjetoIndirectoNoModificable() As Boolean Implements IOperacionDN.ObjetoIndirectoNoModificable
        Get
            Return mObjetoIndirectoNoModificable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mObjetoIndirectoNoModificable)
        End Set
    End Property



#Region "Validaciones"

    Protected Overridable Function ValVerboOperacion(ByRef mensaje As String, ByVal verbo As VerboDN) As Boolean
        If verbo Is Nothing Then
            mensaje = "El verbo de la operación de negocio no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValVerboOperacion(pMensaje, mVerboOperacion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If


        If String.IsNullOrEmpty(Me.mNombre) Then
            Me.mNombre = Me.mVerboOperacion.Nombre
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region


End Class

<Serializable()> Public Class ColOperacionDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of OperacionDN)

#Region "Métodos"

    Public Function RecuperarxNombreVerbo(ByVal pNombreVerbo As String) As OperacionDN
        For Each op As OperacionDN In Me
            If op.VerboOperacion.Nombre.ToLower = pNombreVerbo.ToLower Then
                Return op
            End If
        Next
        Return Nothing
    End Function

    Public Function RecuperarColxTipoEntidadDN(ByVal pTipoDN As System.Type) As ColOperacionDN

        Dim colop As New ColOperacionDN


        For Each op As OperacionDN In Me
            If op.ColDNAceptadas.ContieneTipo(pTipoDN) Then
                colop.Add(op)
            End If
        Next

        Return colop
    End Function

#End Region




End Class


Public Class OperacionDNMapXml
    Implements Framework.DatosNegocio.IXMLAdaptador

    Public VinculoMetodo As Framework.TiposYReflexion.DN.VinculoMetodoDNMapXml


    Public VerboOperacion As New VerboDNMapXml
    Public RutaIcono As String
    Public Comentario As String
    Public Nombre As String


    Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
        Dim mientidad As OperacionDN = pEntidad
        VerboOperacion.ObjetoToXMLAdaptador(mientidad.VerboOperacion)
        Nombre = mientidad.Nombre
        Comentario = mientidad.Comentario
        RutaIcono = mientidad.RutaIcono

    End Sub

    Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto

        Dim mientidad As OperacionDN = pEntidad
        Dim v As New VerboDN
        VerboOperacion.XMLAdaptadorToObjeto(v)
        mientidad.VerboOperacion = v
        mientidad.Nombre = Nombre
        mientidad.Comentario = Comentario
        mientidad.RutaIcono = RutaIcono


    End Sub
End Class
