Imports Framework.DatosNegocio

<Serializable()> _
Public Class CajonDocumentoDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mTipoDocumento As TipoFicheroDN
    Protected mDocumento As HuellaFicheroAlmacenadoIODN
    Protected mHuellasEntidadesReferidas As ColHuellaEntidadReferidaCajonDocumentoDN
    Protected mAlerta As Framework.OperProg.OperProgDN.AlertaDN
    Protected mFechaVerificacon As Date
    ' Protected mHuellasEntidadesReferidas As Framework.DatosNegocio.ColHEDN
    Protected mIdentificacionDocumento As IdentificacionDocumentoDN

    Public Sub New()
        'CambiarValorRef(Of Framework.OperProg.OperProgDN.AlertaDN)(New Framework.OperProg.OperProgDN.AlertaDN, mAlerta)
        '  mAlerta.FEjecProgramada = Now
        Me.CambiarValorCol(Of ColHuellaEntidadReferidaCajonDocumentoDN)(New ColHuellaEntidadReferidaCajonDocumentoDN(), Me.mHuellasEntidadesReferidas)
        'CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(New Framework.DatosNegocio.ColHEDN, mHuellasEntidadesReferidas)
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub










    <RelacionPropCampoAtribute("mIdentificacionDocumento")> _
    Public Property IdentificacionDocumento() As IdentificacionDocumentoDN

        Get
            Return mIdentificacionDocumento
        End Get

        Set(ByVal value As IdentificacionDocumentoDN)
            CambiarValorRef(Of IdentificacionDocumentoDN)(value, mIdentificacionDocumento)

        End Set
    End Property





    Public Sub CrearAlerta(ByVal pPrioriad As Double, ByVal comentario As String)

        Dim miAlerta As New Framework.OperProg.OperProgDN.AlertaDN

        For Each h As HuellaEntidadReferidaCajonDocumentoDN In mHuellasEntidadesReferidas
            Dim he As New HEDN(h.TipoEntidadReferida, h.IdEntidadReferida, h.GUIDReferida)
            miAlerta.ColIHEntidad.AddUnico(he)
        Next

        miAlerta.Prioridad = pPrioriad
        miAlerta.FEjecProgramada = Now
        miAlerta.comentario = comentario
        CambiarValorRef(Of Framework.OperProg.OperProgDN.AlertaDN)(miAlerta, mAlerta)
    End Sub


    Public Sub VerificarDocumentoEnlazado()
        Dim pMensaje As String
        If Not Me.mIdentificacionDocumento Is Nothing AndAlso Me.mIdentificacionDocumento.TipoFichero.GUID <> Me.mTipoDocumento.GUID Then
            pMensaje = "El tipo de documento de mIdentificacionDocumento no coincide"
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(pMensaje)
        End If


        If Not Me.mDocumento Is Nothing AndAlso Not Me.mDocumento.Colidentificaciones.Contiene(Me.mIdentificacionDocumento, CoincidenciaBusquedaEntidadDN.Todos) Then
            pMensaje = "IdentificacionDocumento referido no esta contenido en la coleccion de identidades del fichero"
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(pMensaje)
        End If


        ' marcar la alerta como atendida se actualiza en estado de integridad
        If (Me.mAlerta IsNot Nothing) Then
            mAlerta.Atendida = True
        End If



        CambiarValorVal(Of Date)(Now, mFechaVerificacon)
    End Sub


    <RelacionPropCampoAtribute("mFechaVerificacon")> _
    Public ReadOnly Property FechaVerificacon() As Date

        Get
            Return mFechaVerificacon
        End Get

 
    End Property

    <RelacionPropCampoAtribute("mAlerta")> _
    Public Property Alerta() As Framework.OperProg.OperProgDN.AlertaDN

        Get
            Return mAlerta
        End Get

        Set(ByVal value As Framework.OperProg.OperProgDN.AlertaDN)
            CambiarValorRef(Of Framework.OperProg.OperProgDN.AlertaDN)(value, mAlerta)

        End Set
    End Property

    <RelacionPropCampoAtribute("mTipoDocumento")> _
    Public Property TipoDocumento() As TipoFicheroDN
        Get
            Return Me.mTipoDocumento
        End Get
        Set(ByVal value As TipoFicheroDN)
            Me.CambiarValorRef(Of TipoFicheroDN)(value, Me.mTipoDocumento)
        End Set
    End Property

    <RelacionPropCampoAtribute("mDocumento")> _
    Public Property Documento() As HuellaFicheroAlmacenadoIODN
        Get
            Return Me.mDocumento
        End Get
        Set(ByVal value As HuellaFicheroAlmacenadoIODN)
            Me.CambiarValorRef(Of HuellaFicheroAlmacenadoIODN)(value, Me.mDocumento)

        End Set
    End Property

    <RelacionPropCampoAtribute("mHuellasEntidadesReferidas")> _
    Public Property HuellasEntidadesReferidas() As ColHuellaEntidadReferidaCajonDocumentoDN
        Get
            Return Me.mHuellasEntidadesReferidas
        End Get
        Set(ByVal value As ColHuellaEntidadReferidaCajonDocumentoDN)
            Me.CambiarValorCol(Of ColHuellaEntidadReferidaCajonDocumentoDN)(value, Me.mHuellasEntidadesReferidas)
        End Set
    End Property


    '<RelacionPropCampoAtribute("mHuellasEntidadesReferidas")> _
    'Public Property HuellasEntidadesReferidas() As Framework.DatosNegocio.ColHEDN

    '    Get
    '        Return mHuellasEntidadesReferidas
    '    End Get

    '    Set(ByVal value As Framework.DatosNegocio.ColHEDN)
    '        CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(value, mHuellasEntidadesReferidas)

    '    End Set
    'End Property



    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN

        ' si se vincula un documento y ademas este está verificado en ese caso la alaerta estara atendida
        ' y podria ser borrada o pasada a historico
        If Not mAlerta Is Nothing Then
            Me.mAlerta.Atendida = Me.mDocumento IsNot Nothing AndAlso Me.mFechaVerificacon > Date.MinValue
        End If

        If Me.mTipoDocumento Is Nothing Then
            pMensaje = "El tipo de documento no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not Me.mIdentificacionDocumento Is Nothing AndAlso Me.mIdentificacionDocumento.TipoFichero.GUID <> Me.mTipoDocumento.GUID Then
            pMensaje = "El tipo de documento de mIdentificacionDocumento no coincide"
            Return EstadoIntegridadDN.Inconsistente
        End If


        If Not Me.mDocumento Is Nothing AndAlso Not Me.mDocumento.Colidentificaciones.Contiene(Me.mIdentificacionDocumento, CoincidenciaBusquedaEntidadDN.Todos) Then
            pMensaje = "IdentificacionDocumento referido no esta contenido en la coleccion de identidades del fichero"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

End Class

<Serializable()> _
 Public Class ColCajonDocumentoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of CajonDocumentoDN)

    Public Function PodarCol(ByVal pTipoDoc As TipoFicheroDN)


        Dim colpodados As New ColCajonDocumentoDN


        For Each cd As CajonDocumentoDN In Me
            If cd.TipoDocumento.GUID = pTipoDoc.GUID Then
                colpodados.Add(cd)
            End If
        Next


        Me.EliminarEntidadDN(colpodados, DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)

        Return colpodados

    End Function

End Class
