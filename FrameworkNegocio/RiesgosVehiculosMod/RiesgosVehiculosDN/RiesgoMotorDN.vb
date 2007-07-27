Imports Framework.DatosNegocio

<Serializable()> _
Public Class RiesgoMotorDN
    Inherits EntidadDN
    Implements FN.Seguros.Polizas.DN.IRiesgoDN


#Region "Atributos"

    Protected mMatricula As MatriculaDN
    Protected mCilindrada As Integer
    Protected mFechaMatriculacion As Date
    Protected mNumeroBastidor As String
    Protected mModelo As ModeloDN
    Protected mMatriculado As Boolean
    Protected mEstadoConsistentePoliza As Boolean
    Protected mValorMatricula As String ' valor pensado para chachear el valor de la matricula
    'Protected mValorNumeroBastidor As String ' valor pensado para chachear el valor del bastidor
    Protected mModeloDatos As ModeloDatosDN
#End Region

#Region "Propiedades"

    ''' <summary>
    ''' si es true el riesgo moto debe tener una matricula con valor para la matricula si quere formar parte de la poliza
    ''' a nivel de dar precio es posible que se conteste que esta matriculado pero no dar el numero de la matrícula
    ''' 
    ''' debe de ser true si la matriculaDN dispone de valor para el campo matricula
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Matriculado() As Boolean
        Get
            Return mMatriculado
        End Get
        Set(ByVal value As Boolean)

            If MatriculaMatriculada() Then
                value = True
            End If


            Me.CambiarValorVal(Of Boolean)(value, mMatriculado)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColEmpresas")> _
    Public Property Matricula() As MatriculaDN
        Get
            Return mMatricula
        End Get
        Set(ByVal value As MatriculaDN)
            CambiarValorRef(Of MatriculaDN)(value, mMatricula)
        End Set
    End Property

    Public Property Cilindrada() As Integer
        Get
            Return mCilindrada
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mCilindrada)
        End Set
    End Property

    Public Property FechaMatriculacion() As Date
        Get
            Return mFechaMatriculacion
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaMatriculacion)
        End Set
    End Property

    Public Property NumeroBastidor() As String
        Get
            Return mNumeroBastidor
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNumeroBastidor)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColEmpresas")> _
    Public Property Modelo() As ModeloDN
        Get
            Return mModelo
        End Get
        Set(ByVal value As ModeloDN)
            CambiarValorRef(Of ModeloDN)(value, mModelo)
        End Set
    End Property

    Public ReadOnly Property EstadoConsistentePoliza() As Boolean Implements Seguros.Polizas.DN.IRiesgoDN.EstadoConsistentePoliza
        Get
            mEstadoConsistentePoliza = Me.ComprobarEstadoParaPoliza()
            Return mEstadoConsistentePoliza
        End Get
    End Property

#End Region

#Region "Métodos"






    <RelacionPropCampoAtribute("mModeloDatos")> _
    Public Property ModeloDatos() As ModeloDatosDN
        Get
            Return mModeloDatos
        End Get
        Set(ByVal value As ModeloDatosDN)
            CambiarValorRef(Of ModeloDatosDN)(value, mModeloDatos)
        End Set
    End Property





    Public Function RiesgoValidoPoliza() As Boolean Implements Seguros.Polizas.DN.IRiesgoDN.RiesgoValidoPoliza
        If mMatricula Is Nothing Then
            Return False
        Else
            Return mMatricula.MatriculaTipoCorrecta
        End If

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        'If Me.mMatriculado AndAlso Me.mMatricula Is Nothing Then
        '    pMensaje = "un riesgo motor matriculado debe contar con número de matrícula "
        '    Return EstadoIntegridadDN.Inconsistente
        'End If

        'If Not Me.mMatriculado AndAlso String.IsNullOrEmpty(Me.mNumeroBastidor) Then
        '    pMensaje = "un riesgo motor debe no matriculado debe contar con número de  bastidor"
        '    Return EstadoIntegridadDN.Inconsistente
        'End If



        If mModeloDatos Is Nothing Then
            pMensaje = "El modelo datos no puede ser nulo para un riesgo motor"
            Return EstadoIntegridadDN.Inconsistente
        End If



        If mModeloDatos.Modelo.GUID <> Me.mModelo.GUID Then
            pMensaje = "El modelo del modelodatos y el riesgo motor no coinciden"
            Return EstadoIntegridadDN.Inconsistente
        End If


        'TODO: habría que revisar el estado de consistencia de la póliza
        mEstadoConsistentePoliza = Me.ComprobarEstadoParaPoliza()

        If Me.mMatriculado AndAlso mMatricula IsNot Nothing Then
            mValorMatricula = Me.mMatricula.ValorMatricula
        End If




        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If mModelo IsNot Nothing Then
            cadena = mModelo.ToString() & " - "
        End If

        cadena = cadena & mCilindrada & " cc"

        If mMatricula IsNot Nothing Then
            cadena = cadena & " - " & mMatricula.ValorMatricula
        Else
            cadena = cadena & " - " & mNumeroBastidor
        End If

        cadena = cadena & " - " & mFechaMatriculacion.ToShortDateString()

        Return cadena
    End Function

    'Public Function ClonarRiesgo() As Seguros.Polizas.DN.IRiesgoDN Implements Seguros.Polizas.DN.IRiesgoDN.ClonarRiesgo
    '    Dim riesgoClon As RiesgoMotorDN

    '    riesgoClon = Me.CloneSuperficialSinIdentidad()
    '    riesgoClon.mMatricula = Nothing
    '    riesgoClon.mCilindrada = 0
    '    riesgoClon.mFechaMatriculacion = Date.MinValue
    '    riesgoClon.mNumeroBastidor = String.Empty
    '    riesgoClon.mEstadoConsistentePoliza = False
    '    riesgoClon.mValorMatricula = String.Empty

    '    Return riesgoClon

    'End Function


    Private Sub RiesgoMotorDN_CambioEstadoDatos(ByVal sender As Object) Handles Me.CambioEstadoDatos

        If sender Is Me.mMatricula Then
            If MatriculaMatriculada() Then
                Me.Matriculado = True
            End If
        End If

    End Sub

    Private Function MatriculaMatriculada() As Boolean
        If mMatricula Is Nothing Then

            Return False
        Else

            Return Me.mMatricula.TipoMatricula <> TipoMatricula.LibreTMK 'AndAlso Not String.IsNullOrEmpty(Me.mMatricula.ValorMatricula)
        End If
    End Function

    Private Function ComprobarEstadoParaPoliza() As Boolean
        If (mMatriculado AndAlso mMatricula Is Nothing) OrElse (Not mMatriculado AndAlso String.IsNullOrEmpty(mNumeroBastidor)) Then
            Return False
        Else
            Return True
        End If
    End Function

#End Region



End Class

<Serializable()> _
Public Class RiesgoMotorFuturoDN
    Inherits EntidadDN
    Implements FN.Seguros.Polizas.DN.IRiesgoDN



#Region "Atributos"

    Protected mMatricula As MatriculaDN
    Protected mCilindrada As Integer
    Protected mFechaMatriculacion As Date
    Protected mNumeroBastidor As String
    Protected mModelo As ModeloDN
    Protected mMatriculado As Boolean
    Protected mEstadoConsistentePoliza As Boolean
    Protected mValorMatricula As String ' valor pensado para chachear el valor de la matricula
    Protected mRiesgoMotor As RiesgoMotorDN
#End Region

#Region "Propiedades"








    <RelacionPropCampoAtribute("mRiesgoMotor")> _
    Public Property RiesgoMotor() As RiesgoMotorDN

        Get
            Return mRiesgoMotor
        End Get

        Set(ByVal value As RiesgoMotorDN)
            CambiarValorRef(Of RiesgoMotorDN)(value, mRiesgoMotor)

        End Set
    End Property



    Public ReadOnly Property Iguales() As Boolean
        Get

            If Me.mRiesgoMotor Is Nothing Then


                Return False
            Else
                If Me.mRiesgoMotor.Matricula.ValorMatricula = Me.mValorMatricula Then
                    Return True
                Else
                    Return False
                End If
            End If
        End Get
    End Property


    ''' <summary>
    ''' si es true el riesgo moto debe tener una matricula con valor para la matricula si quere formar parte de la poliza
    ''' a nivel de dar precio es posible que se conteste que esta matriculado pero no dar el numero de la matrícula
    ''' 
    ''' debe de ser true si la matriculaDN dispone de valor para el campo matricula
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Matriculado() As Boolean
        Get
            Return mMatriculado
        End Get
        Set(ByVal value As Boolean)

            If MatriculaMatriculada() Then
                value = True
            End If


            Me.CambiarValorVal(Of Boolean)(value, mMatriculado)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColEmpresas")> _
    Public Property Matricula() As MatriculaDN
        Get
            Return mMatricula
        End Get
        Set(ByVal value As MatriculaDN)
            CambiarValorRef(Of MatriculaDN)(value, mMatricula)

        End Set
    End Property

    Public Property Cilindrada() As Integer
        Get
            Return mCilindrada
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mCilindrada)
        End Set
    End Property

    Public Property FechaMatriculacion() As Date
        Get
            Return mFechaMatriculacion
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaMatriculacion)
        End Set
    End Property

    Public Property NumeroBastidor() As String
        Get
            Return mNumeroBastidor
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNumeroBastidor)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColEmpresas")> _
    Public Property Modelo() As ModeloDN
        Get
            Return mModelo
        End Get
        Set(ByVal value As ModeloDN)
            CambiarValorRef(Of ModeloDN)(value, mModelo)
        End Set
    End Property

    Public ReadOnly Property EstadoConsistentePoliza() As Boolean Implements Seguros.Polizas.DN.IRiesgoDN.EstadoConsistentePoliza
        Get
            mEstadoConsistentePoliza = Me.ComprobarEstadoParaPoliza()
            Return mEstadoConsistentePoliza
        End Get
    End Property

#End Region

#Region "Métodos"

    Public Function RiesgoValidoPoliza() As Boolean Implements Seguros.Polizas.DN.IRiesgoDN.RiesgoValidoPoliza
        Return mMatricula.MatriculaTipoCorrecta
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        'If Me.mMatriculado AndAlso Me.mMatricula Is Nothing Then
        '    pMensaje = "un riesgo motor matriculado debe contar con número de matrícula "
        '    Return EstadoIntegridadDN.Inconsistente
        'End If

        'If Not Me.mMatriculado AndAlso String.IsNullOrEmpty(Me.mNumeroBastidor) Then
        '    pMensaje = "un riesgo motor debe no matriculado debe contar con número de  bastidor"
        '    Return EstadoIntegridadDN.Inconsistente
        'End If

        'TODO: habría que revisar el estado de consistencia de la póliza
        mEstadoConsistentePoliza = Me.ComprobarEstadoParaPoliza()

        If Me.mMatriculado AndAlso mMatricula IsNot Nothing Then
            mValorMatricula = Me.mMatricula.ValorMatricula
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If mModelo IsNot Nothing Then
            cadena = mModelo.ToString() & " - "
        End If

        cadena = cadena & mCilindrada & " cc"

        If mMatricula IsNot Nothing Then
            cadena = cadena & " - " & mMatricula.ValorMatricula
        Else
            cadena = cadena & " - " & mNumeroBastidor
        End If

        cadena = cadena & " - " & mFechaMatriculacion.ToShortDateString()

        Return cadena
    End Function

    'Public Function ClonarRiesgo() As Seguros.Polizas.DN.IRiesgoDN Implements Seguros.Polizas.DN.IRiesgoDN.ClonarRiesgo
    '    Dim riesgoClon As RiesgoMotorDN

    '    riesgoClon = Me.CloneSuperficialSinIdentidad()
    '    riesgoClon.mMatricula = Nothing
    '    riesgoClon.mCilindrada = 0
    '    riesgoClon.mFechaMatriculacion = Date.MinValue
    '    riesgoClon.mNumeroBastidor = String.Empty
    '    riesgoClon.mEstadoConsistentePoliza = False
    '    riesgoClon.mValorMatricula = String.Empty

    '    Return riesgoClon

    'End Function


    Private Sub RiesgoMotorDN_CambioEstadoDatos(ByVal sender As Object) Handles Me.CambioEstadoDatos

        If sender Is Me.mMatricula Then
            If MatriculaMatriculada() Then
                Me.Matriculado = True
            End If
        End If

    End Sub

    Private Function MatriculaMatriculada() As Boolean
        If mMatricula Is Nothing Then

            Return False
        Else

            Return Me.mMatricula.TipoMatricula <> TipoMatricula.LibreTMK 'AndAlso Not String.IsNullOrEmpty(Me.mMatricula.ValorMatricula)
        End If
    End Function

    Private Function ComprobarEstadoParaPoliza() As Boolean
        If (mMatriculado AndAlso mMatricula Is Nothing) OrElse (Not mMatriculado AndAlso String.IsNullOrEmpty(mNumeroBastidor)) Then
            Return False
        Else
            Return True
        End If
    End Function

#End Region



    'Public Function ClonarRiesgo() As Seguros.Polizas.DN.IRiesgoDN Implements Seguros.Polizas.DN.IRiesgoDN.ClonarRiesgo

    'End Function
End Class
