
Imports Framework.DatosNegocio
<Serializable()> Public Class RelacionENFicheroDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

    '  Protected mEntidadNegocio As EntNegocioDN
    Protected mHuellaFichero As Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN
    Protected mEstadosRelacionENFichero As EstadosRelacionENFicheroDN

    'Protected mCajonDoc As Framework.Ficheros.FicherosDN.CajonDocumentoDN
    Protected mTipoEntNegoio As TipoEntNegoioDN
    'Protected mTipoFichero As Framework.Ficheros.FicherosDN.TipoFicheroDN
    'Protected mValorIdentidad As String

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        mEstadosRelacionENFichero = New EstadosRelacionENFicheroDN(AmvDocumentosDN.EstadosRelacionENFichero.Creada)

    End Sub

#End Region

    Public Sub FijarFF(ByVal pFF As Date)
        Me.CambiarValorVal(Of Date)(pFF, Me.mPeriodo.FFinal)
    End Sub







    'Public Property ValorIdentidad() As String

    '    Get
    '        Return mValorIdentidad
    '    End Get

    '    Set(ByVal value As String)
    '        CambiarValorVal(Of String)(value, mValorIdentidad)

    '    End Set
    'End Property





    '<RelacionPropCampoAtribute("mTipoFichero")> _
    'Public Property TipoFichero() As Framework.Ficheros.FicherosDN.TipoFicheroDN

    '    Get
    '        Return mTipoFichero
    '    End Get

    '    Set(ByVal value As Framework.Ficheros.FicherosDN.TipoFicheroDN)
    '        CambiarValorRef(Of Framework.Ficheros.FicherosDN.TipoFicheroDN)(value, mTipoFichero)

    '    End Set
    'End Property





    <RelacionPropCampoAtribute("mTipoEntNegoio")> _
    Public Property TipoEntNegoio() As TipoEntNegoioDN

        Get
            Return mTipoEntNegoio
        End Get

        Set(ByVal value As TipoEntNegoioDN)
            CambiarValorRef(Of TipoEntNegoioDN)(value, mTipoEntNegoio)

        End Set
    End Property







    '<RelacionPropCampoAtribute("mCajonDoc")> _
    'Public Property CajonDoc() As Framework.Ficheros.FicherosDN.CajonDocumentoDN

    '    Get
    '        Return mCajonDoc
    '    End Get

    '    Set(ByVal value As Framework.Ficheros.FicherosDN.CajonDocumentoDN)
    '        CambiarValorRef(Of Framework.Ficheros.FicherosDN.CajonDocumentoDN)(value, mCajonDoc)

    '    End Set
    'End Property





    Public ReadOnly Property EstadosRelacionENFichero() As EstadosRelacionENFicheroDN
        Get
            Return mEstadosRelacionENFichero
        End Get

    End Property

    'Public Property EntidadNegocio() As EntNegocioDN
    '    Get
    '        Return mEntidadNegocio
    '    End Get
    '    Set(ByVal value As EntNegocioDN)
    '        Me.CambiarValorRef(Of EntNegocioDN)(value, mEntidadNegocio)
    '    End Set
    'End Property

    Public Property HuellaFichero() As Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN
        Get
            Return mHuellaFichero
        End Get
        Set(ByVal value As Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)
            Me.CambiarValorRef(Of Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)(value, mHuellaFichero)
        End Set
    End Property
    Public Sub Cerrar()
        Dim mensaje As String
        If Me.mPeriodo.FFinal > Date.MinValue Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la relacion está cerrada")
        End If

        If Not Me.AlcanzaEstado(mensaje, AmvDocumentosDN.EstadosRelacionENFichero.Cerrado) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If

        If Me.mPeriodo.FFinal = Date.MinValue Then
        End If

        Me.mPeriodo.FFinal = Now
        Me.mEstadosRelacionENFichero = New EstadosRelacionENFicheroDN(AmvDocumentosDN.EstadosRelacionENFichero.Cerrado)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado
    End Sub


    Public Sub Crear()
        Dim mensaje As String

        'If mFF > Date.MinValue Then
        '    Throw New Framework.DatosNegocio.ApplicationExceptionDN("la relacion esta cerrada")
        'End If
        Me.mPeriodo.FFinal = Date.MinValue




        If Not Me.AlcanzaEstado(mensaje, AmvDocumentosDN.EstadosRelacionENFichero.Creada) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If

        Me.mEstadosRelacionENFichero = New EstadosRelacionENFicheroDN(AmvDocumentosDN.EstadosRelacionENFichero.Creada)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado

    End Sub

    Public Sub Clasificar()
        Dim mensaje As String

        If Me.mPeriodo.FFinal > Date.MinValue Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la relacion esta cerrada")
        End If

        If Not Me.AlcanzaEstado(mensaje, AmvDocumentosDN.EstadosRelacionENFichero.Clasificando) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If
        Me.mEstadosRelacionENFichero = New EstadosRelacionENFicheroDN(AmvDocumentosDN.EstadosRelacionENFichero.Clasificando)







        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado

    End Sub

    Public Sub Incidentar()
        Dim mensaje As String


        If Me.mPeriodo.FFinal > Date.MinValue Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la relacion esta cerrada")
        End If

        If Not Me.AlcanzaEstado(mensaje, AmvDocumentosDN.EstadosRelacionENFichero.Creada) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If
        Me.mEstadosRelacionENFichero = New EstadosRelacionENFicheroDN(AmvDocumentosDN.EstadosRelacionENFichero.Incidentado)

        ' mFF = Now
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado



    End Sub

    Public Sub Anular()
        Dim mensaje As String

        If Me.mPeriodo.FFinal > Date.MinValue Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la relacion esta cerrada")
        End If

        If Not Me.AlcanzaEstado(mensaje, AmvDocumentosDN.EstadosRelacionENFichero.Creada) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If


        Me.mEstadosRelacionENFichero = New EstadosRelacionENFicheroDN(AmvDocumentosDN.EstadosRelacionENFichero.Anulado)
        Me.mPeriodo.FFinal = Now
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado


    End Sub
    Public Function AlcanzaEstado(ByRef mensaje As String, ByVal pEstadosRelacionENFichero As EstadosRelacionENFichero) As Boolean

        Select Case pEstadosRelacionENFichero
            Case AmvDocumentosDN.EstadosRelacionENFichero.Cerrado
                ' debe tener una entidad de negocio con tipo e id y una huella de fichero
                'If Me.mEntidadNegocio.IdEntNeg Is Nothing OrElse Me.mEntidadNegocio.IdEntNeg = "" Then
                '    mensaje = "la entidad de negocio no dispone de un id asignado"
                '    Return False
                'End If
                'If Me.mEntidadNegocio.TipoEntNegocioReferidora Is Nothing OrElse Me.mEntidadNegocio.TipoEntNegocioReferidora.ID = "" Then
                '    mensaje = "la entidad de negocio no indica su tipo de entidad"
                '    Return False
                'End If
                If Me.mHuellaFichero.RutaAbsoluta Is Nothing OrElse Me.mHuellaFichero.RutaAbsoluta = "" Then
                    mensaje = "la huella de fichero  no expresa la ruta absoluta"
                    Return False
                End If


                If Me.mHuellaFichero.Colidentificaciones.CalcularGradoIdetificacion < Framework.Ficheros.FicherosDN.GradoIdetificacion.todosIdentificable Then
                    mensaje = "alguno de los elementos no esta identificado"
                    Return False
                End If

                'If Me.mTipoFichero Is Nothing Then
                '    mensaje = "debe de disponer de un tipo de fichero para su identificacion"
                '    Return False
                'End If

                'If String.IsNullOrEmpty(mValorIdentidad) Then
                '    mensaje = "debe de disponer de un ValorIdentidad de fichero para su identificacion"
                '    Return False
                'End If

                'If Me.mCajonDoc Is Nothing Then
                '    mensaje = "Debe estar relacioando con un cajon documento"
                '    Return False
                'End If

                'If Me.mCajonDoc Is Nothing OrElse Me.mCajonDoc.IdentificacionDocumento Is Nothing Then
                '    mensaje = "Debe estar relacioando con un cajon documento vinculado a una identificación de documento"
                '    Return False
                'End If

                'If Me.mCajonDoc IsNot Nothing AndAlso Me.mCajonDoc.Documento IsNot Nothing Then
                '    mensaje = "El cajon documento no refiere a la huella de documento a decuada"
                '    Return False
                'End If

                'If Me.mCajonDoc IsNot Nothing AndAlso Me.mCajonDoc.Documento IsNot Me.HuellaFichero Then
                '    mensaje = "El cajon documento no refiere a la huella de documento a decuada"
                '    Return False
                'End If

                Return True


            Case AmvDocumentosDN.EstadosRelacionENFichero.Clasificando

                If Me.mHuellaFichero.RutaAbsoluta Is Nothing OrElse Me.mHuellaFichero.RutaAbsoluta = "" Then
                    mensaje = "la huella de fichero  no expresa la ruta absoluta"
                    Return False
                End If


                ' debe de tener tipo de entidad de negocio selecioando

                'If Me.mEntidadNegocio.TipoEntNegocioReferidora Is Nothing Then
                '    mensaje = "La entidad de negocio no tine un Tipo de Entidad de Negocio seleccioanda"
                '    Return False

                'End If

                'If Me.mHuellaFichero.Colidentificaciones.Count > 0 Then
                '    mensaje = "almenos debe tener una identificacion de documento"
                '    Return False
                'End If

                'If Me.mCajonDoc Is Nothing Then
                '    mensaje = "Debe estar relacioando con un cajon documento"
                '    Return False

                'End If



                Return True


            Case Else
                Return True

        End Select






    End Function


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Not Me.AlcanzaEstado(pMensaje, Me.mEstadosRelacionENFichero.Valor) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(pMensaje)
        End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function


End Class

Public Enum EstadosRelacionENFichero
    Creada = 1
    Clasificando = 2
    Cerrado = 3
    Incidentado = 4
    Anulado = 5
End Enum

<Serializable()> _
Public Class EstadosRelacionENFicheroDN
    Inherits Framework.DatosNegocio.EntidadTipoDN(Of EstadosRelacionENFichero)


    Public Sub New()
    End Sub


    Public Sub New(ByVal valor As EstadosRelacionENFichero)
        MyBase.New(valor)
    End Sub

    Protected Overrides Function CrearInstancia(ByVal valor As EstadosRelacionENFichero) As Object
        Return New EstadosRelacionENFicheroDN(valor)
    End Function
End Class