Imports Framework.DatosNegocio

''' <summary>
''' permite guardar y recuperar un fichero atraves de una ruta que se compone usando el prefijo de ruta ofrecido por una ruta de almacenamiento
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class HuellaFicheroAlmacenadoIODN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mRutaAlmacenamiento As RutaAlmacenamientoFicherosDN
    Protected mRutaRelativa As String
    Protected mDatos As Byte()
    Protected mExtension As String
    Protected mNombreOriginalFichero As String
    ' Protected mTipoFichero As TipoFicheroDN

    Protected mColIdentificaciones As ColIdentificacionDocumentoDN
    Protected mColIdentificacionesIncorrectas As ColIdentificacionDocumentoDN
#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorRef(Of ColIdentificacionDocumentoDN)(New ColIdentificacionDocumentoDN, mColIdentificaciones)
        CambiarValorRef(Of ColIdentificacionDocumentoDN)(New ColIdentificacionDocumentoDN, mColIdentificacionesIncorrectas)

        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar

    End Sub
    Public Sub New(ByVal pFileInfo As IO.FileInfo)

        mNombreOriginalFichero = pFileInfo.Name
        Me.mNombre = pFileInfo.Name
        Me.mExtension = pFileInfo.Extension
        CambiarValorRef(Of ColIdentificacionDocumentoDN)(New ColIdentificacionDocumentoDN, mColIdentificaciones)
        CambiarValorRef(Of ColIdentificacionDocumentoDN)(New ColIdentificacionDocumentoDN, mColIdentificacionesIncorrectas)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar

    End Sub
#End Region

#Region "Propiedades"


    <RelacionPropCampoAtribute("mColidentificacionesIncorrectas")> _
    Public Property ColidentificacionesIncorrectas() As ColIdentificacionDocumentoDN

        Get
            Return mColIdentificacionesIncorrectas
        End Get

        Set(ByVal value As ColIdentificacionDocumentoDN)
            CambiarValorRef(Of ColIdentificacionDocumentoDN)(value, mColIdentificacionesIncorrectas)

        End Set
    End Property


    <RelacionPropCampoAtribute("mColidentificaciones")> _
    Public Property Colidentificaciones() As ColIdentificacionDocumentoDN

        Get
            Return mColIdentificaciones
        End Get

        Set(ByVal value As ColIdentificacionDocumentoDN)
            CambiarValorRef(Of ColIdentificacionDocumentoDN)(value, mColIdentificaciones)

        End Set
    End Property


    Public Property NombreOriginalFichero() As String
        Get
            Return Me.mNombreOriginalFichero
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mNombreOriginalFichero)
        End Set
    End Property

    Public Property Extension() As String
        Get
            Return Me.mExtension
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mExtension)
        End Set
    End Property

    Public ReadOnly Property NombreyExtension() As String
        Get
            Return Me.mNombre & mExtension
        End Get
    End Property

    Public Property RutaRelativa() As String
        Get
            If mRutaRelativa Is Nothing OrElse mRutaRelativa = "" Then
                mRutaRelativa = mRutaAlmacenamiento.GenerarRuta
            End If
            Return mRutaRelativa
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mRutaRelativa)
        End Set
    End Property

    Public ReadOnly Property RutaCarpetaContenedora() As String
        Get
            Return mRutaAlmacenamiento.RutaCarpeta & Me.RutaRelativa
        End Get
    End Property

    Public ReadOnly Property RutaAbsoluta() As String
        Get
            Return mRutaAlmacenamiento.RutaCarpeta & Me.RutaRelativa & "\" & Me.NombreyExtension
        End Get
    End Property

    Public Property Datos() As Byte()
        Get
            Return mDatos
        End Get
        Set(ByVal value As Byte())
            Me.CambiarValorVal(Of Byte())(value, mDatos)
        End Set
    End Property

    <RelacionPropCampoAtribute("mRutaAlmacenamiento")> _
    Public Property RutaAlmacenamiento() As RutaAlmacenamientoFicherosDN
        Get
            Return Me.mRutaAlmacenamiento
        End Get
        Set(ByVal value As RutaAlmacenamientoFicherosDN)

            If value Is Nothing Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("La asignacion de una ruta de almacenamiento no puede ser nozing")
            End If
            Me.CambiarValorRef(Of RutaAlmacenamientoFicherosDN)(value, mRutaAlmacenamiento)
        End Set
    End Property

    '<RelacionPropCampoAtribute("mColaboradorComercial")> _
    'Public Property TipoFichero() As TipoFicheroDN
    '    Get
    '        Return mTipoFichero
    '    End Get
    '    Set(ByVal value As TipoFicheroDN)
    '        CambiarValorRef(Of TipoFicheroDN)(value, mTipoFichero)
    '    End Set
    'End Property

#End Region


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN

        ' no puede una identificacion de documento estar contenida en la coleccion de correctos y de incorrectos


        If Me.mColIdentificacionesIncorrectas.ContieneAlguno(mColIdentificaciones, CoincidenciaBusquedaEntidadDN.Todos) Then
            pMensaje = "alguna identificacion esta contenida en la coleccion de identificaciones correctas y erroneas a la vez"
            Return EstadoIntegridadDN.Inconsistente
        End If



        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


    Public Function ReemplazarIndentificacion(ByVal pIdentificacionEntrante As IdentificacionDocumentoDN, ByVal pIdentificacionSaliente As IdentificacionDocumentoDN) As IdentificacionDocumentoDN



        Dim miid As IdentificacionDocumentoDN = Me.mColIdentificaciones.EliminarEntidadDN(pIdentificacionEntrante.GUID, CoincidenciaBusquedaEntidadDN.Todos)(0)

        If miid Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("no encuentra la identificacion de documento entre las identificaciones correctas")
        End If

        If pIdentificacionSaliente Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(" la identificacion a reemplazar no puede ser nula")
        End If


        Me.mColIdentificaciones.Add(pIdentificacionSaliente)

        Return miid


    End Function
    Public Function RechazarIndentificacion(ByVal pIdentificacionDocumento As IdentificacionDocumentoDN) As IdentificacionDocumentoDN



        Dim miid As IdentificacionDocumentoDN = Me.mColIdentificaciones.EliminarEntidadDN(pIdentificacionDocumento.GUID, CoincidenciaBusquedaEntidadDN.Todos)(0)

        If miid Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("no encuentra la identificacion de documento entre las identificaciones correctas")
        End If


        Me.mColIdentificacionesIncorrectas.Add(miid)

        Return miid


    End Function


End Class

<Serializable()> _
Public Class ColHuellaFicheroAlmacenadoIODN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of HuellaFicheroAlmacenadoIODN)
End Class