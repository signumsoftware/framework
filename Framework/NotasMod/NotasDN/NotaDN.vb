Imports Framework.Ficheros.FicherosDN
Imports Framework.DatosNegocio

<Serializable()> _
 Public Class NotaDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN



#Region "Atributos"
    ' poddiamos terner un atributo que diga el creador de la nota pero tambien podia venir por una operación
    Protected mCreador As Framework.Usuarios.DN.UsuarioDN
    Protected mComentario As String
    Protected mColHEntidad As Framework.DatosNegocio.ColHEDN
    Protected mModificable As Boolean
    Protected mFechaCreacion As Date = Now
    Protected mprioridad As Double
#End Region


#Region "Constructores"
    Public Sub New()
        Me.CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(New ColHEDN, mColHEntidad)
        ' Me.CambiarValorRef(Of Framework.Ficheros.FicherosDN.ColHuellaFicheroAlmacenadoIODN)(New ColHuellaFicheroAlmacenadoIODN, mColHFicheroAlm)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pnombre As String, ByVal pcomentario As String, ByVal pcreador As Usuarios.DN.UsuarioDN)
        Me.mNombre = pnombre
        mComentario = pcomentario
        Me.CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(New ColHEDN, mColHEntidad)
        '    Me.CambiarValorRef(Of Framework.Ficheros.FicherosDN.ColHuellaFicheroAlmacenadoIODN)(New ColHuellaFicheroAlmacenadoIODN, mColHFicheroAlm)

        Me.CambiarValorRef(Of Framework.Usuarios.DN.UsuarioDN)(pcreador, mCreador)

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"
    <RelacionPropCampoAtribute("mCreador")> _
    Public ReadOnly Property Creador() As Framework.Usuarios.DN.UsuarioDN
        Get
            Return Me.mCreador
        End Get
    End Property
    Public Overrides Property Nombre() As String
        Get
            Return MyBase.Nombre
        End Get
        Set(ByVal value As String)
            If esModificable() Then
                MyBase.Nombre = value
            End If
        End Set
    End Property

    Public ReadOnly Property FechaCreacion() As Date
        Get
            Return Me.mFechaCreacion
        End Get
    End Property

    Public Property Modificable() As Boolean
        Get
            Return Me.mModificable
        End Get
        Set(ByVal value As Boolean)
            If esModificable() Then
                Me.CambiarValorVal(Of Boolean)(value, Me.mModificable)
            End If
        End Set
    End Property
    <RelacionPropCampoAtribute("mColHEntidad")> _
    Public Property ColIHEntidad() As Framework.DatosNegocio.ColHEDN
        Get
            Return Me.mColHEntidad
        End Get
        Set(ByVal value As Framework.DatosNegocio.ColHEDN)
            If esModificable() Then
                Me.CambiarValorRef(Of ColHEDN)(value, Me.mColHEntidad)
            End If
        End Set
    End Property

    'Public Property ColHFicheroAlm() As ColHuellaFicheroAlmacenadoIODN
    '    Get
    '        '  Return Me.mColHFicheroAlm
    '    End Get
    '    Set(ByVal value As ColHuellaFicheroAlmacenadoIODN)
    '        '    Me.CambiarValorRef(Of ColHuellaFicheroAlmacenadoIODN)(value, Me.mColHFicheroAlm)
    '    End Set
    'End Property

    Public Property comentario() As String
        Get
            Return Me.mComentario
        End Get
        Set(ByVal value As String)
            If esModificable() Then
                Me.CambiarValorVal(Of String)(value, Me.mComentario)
            End If

        End Set
    End Property

    Public Property Prioridad() As Double
        Get
            Return Me.mprioridad
        End Get
        Set(ByVal value As Double)
            If esModificable() Then
                Me.CambiarValorVal(Of Double)(value, Me.mprioridad)
            End If


        End Set
    End Property


#End Region

#Region "Metodos"


    Private Function esModificable() As Boolean
        Return Me.mModificable OrElse String.IsNullOrEmpty(Me.mID)
    End Function

    Public Sub AsignarCreador(ByVal pCreador As Framework.Usuarios.DN.UsuarioDN)
        Me.CambiarValorRef(Of Usuarios.DN.UsuarioDN)(pCreador, Me.mCreador)

    End Sub

#End Region

End Class
