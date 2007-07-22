
Imports Framework.DatosNegocio




''' <summary>
''' representa un movimiento de dinero entre dos entiedades fiscales, que puede estar en los estados
''' (planificado, emitido, liquidado) ( activo, anulado) 
''' 
'''  y podrá estar compesado por otros pagos 
''' 
''' es un origen de importe debido y su valor será el saldo de los importes debidos que refiere por su campo mColIImporteDebidoOrigenes
''' 
''' 
''' mapeado de busqueda de la aplicación de pagos: /vwPagosxTipoEntOrigenxARxPrincipal/vwPagosSinDestinatarios*Donde*idPrincipal=#Principal.ID
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class PagoDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IOrigenIImporteDebidoDN


#Region "Atributos"

    Protected mOrigen As OrigenDN

    Protected mImportePago As Double


    ' TODO: el campo destinatario debiera eliminarse si no hay vista que lo use

    ''' <summary>
    ''' en un pago normal debe de coincidir con el deudor del importe debido een un pago norma
    ''' en un pago compensado debe coincidor con el acreedor
    ''' </summary>
    ''' <remarks></remarks>
    Protected mDeudor As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    ''' <summary>
    ''' en un pago normal debe de coincidir con el acreedor del importe debido een un pago norma
    ''' en un pago compensado debe coincidor con el deudor
    ''' </summary>
    ''' <remarks></remarks>
    Protected mDestinatario As FN.Localizaciones.DN.EntidadFiscalGenericaDN

    ''' <summary>
    ''' su entidad fiscal debe ser el deudor
    ''' </summary>
    ''' <remarks></remarks>
    Protected mCuentaOrigenPago As Financiero.DN.CuentaBancariaDN
    Protected mTalon As TalonDN
    Protected mTransferencia As TransferenciaDN

    ''' <summary>
    ''' la fecha a la que se programa la emisión de una orden de cobro de un pago
    ''' </summary>
    ''' <remarks></remarks>
    Protected mFechaProgramadaEmision As Date

    ''' <summary>
    ''' fecha en la que se emite la orden de cobro de un pago
    ''' </summary>
    ''' <remarks></remarks>
    Protected mFechaEmision As Date

    ''' <summary>
    ''' es la fecha la que el pago debiera haberse realizado por porte del cliente
    ''' </summary>
    ''' <remarks></remarks>
    Protected mFechaEfectoEsperada As Date

    ''' <summary>
    ''' es la fecha a la que el cliente realizó el pago a las cuentas de prestadora de servicios
    ''' </summary>
    ''' <remarks></remarks>
    Protected mFechaEfecto As Date
    Protected mCentroCostesDepartamento As CentroCostesDepartamentoDN
    Protected mColNotificacionPago As ColNotificacionPagoDN

    Protected mIdFicheroTransferencia As String
    Protected mFechaAnulacion As Date


    ' Protected mIImporteDebidoDN As IImporteDebidoDN

    ''' <summary>
    ''' es el importe debido causa del pago
    ''' </summary>
    ''' <remarks></remarks>
    Protected mApunteImpDOrigen As ApunteImpDDN

    Protected mGUIDIImporteDebidoOrigen As String



    ''' <summary>
    ''' la coleccion de importes debidos que compesan la diferencia en tre el importe del pago
    ''' y so importe debido produccto de ser un origen de importe debido
    ''' 
    ''' </summary>
    ''' <remarks></remarks>
    Protected mColApunteImpDCompensantes As ColApunteImpDDN
    Protected mPagoCompensado As PagoDN

    ''' <summary>
    ''' es el importe debido que genera por ser un origen de importe debido
    ''' 
    ''' su valor debe ser igual al del pago salbo que existan importes debidos compensantes en la col  mColIImporteDebidoCompensantes
    ''' </summary>
    ''' <remarks></remarks>
    Protected mApunteImpDProducto As ApunteImpDDN


    ''' <summary>
    ''' la fecha en la que es creada el pago
    ''' </summary>
    ''' <remarks></remarks>
    Protected mFechaCreacion As DateTime

    ' determina la posicion del pago en una seria que cubren el mismo importe debido
    Protected mPosicionPago As PosicionPago
#End Region

#Region "Constructores"

    Public Sub New()

        MyBase.New()
        Me.mFechaCreacion = Now
        Me.CambiarValorCol(Of ColNotificacionPagoDN)(New ColNotificacionPagoDN, mColNotificacionPago)
        Me.CambiarValorCol(Of ColApunteImpDDN)(New ColApunteImpDDN, mColApunteImpDCompensantes)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente

    End Sub

#End Region

#Region "Propiedades"







    Public Property PosicionPago() As PosicionPago

        Get
            Return mPosicionPago
        End Get

        Set(ByVal value As PosicionPago)
            CambiarValorVal(Of PosicionPago)(value, mPosicionPago)

        End Set
    End Property










    Public ReadOnly Property FechaCreacion() As DateTime

        Get
            Return mFechaCreacion
        End Get


    End Property







    <RelacionPropCampoAtribute("mApunteImpDProducto")> _
    Public Property ApunteImpDProducto() As FN.GestionPagos.DN.ApunteImpDDN

        Get
            Return mApunteImpDProducto
        End Get

        Set(ByVal value As ApunteImpDDN)
            CambiarValorRef(Of ApunteImpDDN)(value, mApunteImpDProducto)

        End Set
    End Property





    <RelacionPropCampoAtribute("mColApunteImpDCompensantes")> _
    Public Property ColApunteImpDCompensantes() As ColApunteImpDDN

        Get
            Return mColApunteImpDCompensantes
        End Get

        Set(ByVal value As ColApunteImpDDN)
            CambiarValorRef(Of ColApunteImpDDN)(value, mColApunteImpDCompensantes)

        End Set
    End Property


    Property Deudor() As FN.Localizaciones.DN.EntidadFiscalGenericaDN
        Get
            Return Me.mDeudor
        End Get
        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            Me.CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mDeudor)

        End Set
    End Property

    Public ReadOnly Property FechaEmision() As Date
        Get
            Return Me.mFechaEmision
        End Get
    End Property

    Public ReadOnly Property FechaAnulacion() As Date
        Get
            Return Me.mFechaAnulacion
        End Get
    End Property


    Public Property PagoCompensado() As PagoDN
        Get
            Return Me.mPagoCompensado
        End Get
        Set(ByVal value As PagoDN)
            Me.CambiarValorRef(Of PagoDN)(value, mPagoCompensado)
        End Set
    End Property

    Public Property ColNotificacionPago() As ColNotificacionPagoDN
        Get
            Return Me.mColNotificacionPago
        End Get
        Set(ByVal value As ColNotificacionPagoDN)
            Me.CambiarValorCol(Of ColNotificacionPagoDN)(value, mColNotificacionPago)
        End Set
    End Property

    Public Property CentroCostesDepartamento() As CentroCostesDepartamentoDN
        Get
            Return mCentroCostesDepartamento
        End Get
        Set(ByVal value As CentroCostesDepartamentoDN)
            CambiarValorRef(Of CentroCostesDepartamentoDN)(value, mCentroCostesDepartamento)

        End Set
    End Property

    Public Property Origen() As OrigenDN
        Get
            Return mOrigen
        End Get
        Set(ByVal value As OrigenDN)
            CambiarValorRef(Of OrigenDN)(value, mOrigen)
        End Set
    End Property

    Public Property Importe() As Double
        Get
            Return mImportePago
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mImportePago)
        End Set
    End Property

    Public Property Destinatario() As FN.Localizaciones.DN.EntidadFiscalGenericaDN
        Get
            Return mDestinatario
        End Get
        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)


            CambiarValorRef(value, mDestinatario)
        End Set
    End Property

    Public Property Talon() As TalonDN
        Get
            Return Me.mTalon
        End Get
        Set(ByVal value As TalonDN)
            Me.CambiarValorRef(value, Me.mTalon)
        End Set
    End Property

    Public Property Transferencia() As TransferenciaDN
        Get
            Return Me.mTransferencia
        End Get
        Set(ByVal value As TransferenciaDN)
            Me.CambiarValorRef(value, Me.mTransferencia)
        End Set
    End Property

    Public Property CuentaOrigenPago() As Financiero.DN.CuentaBancariaDN
        Get
            Return Me.mCuentaOrigenPago
        End Get
        Set(ByVal value As Financiero.DN.CuentaBancariaDN)
            Me.CambiarValorRef(value, Me.mCuentaOrigenPago)
        End Set
    End Property
    Public Property FechaEfectoEsperada() As DateTime
        Get
            Return Me.mFechaEfectoEsperada
        End Get
        Set(ByVal value As DateTime)
            Me.CambiarValorVal(value, Me.mFechaEfectoEsperada)
        End Set
    End Property
    Public Property FechaEfecto() As DateTime
        Get
            Return Me.mFechaEfecto
        End Get
        Set(ByVal value As DateTime)
            Me.CambiarValorVal(value, Me.mFechaEfecto)
        End Set
    End Property

    Public Property FechaProgramadaEmision() As DateTime
        Get
            Return Me.mFechaProgramadaEmision
        End Get
        Set(ByVal value As DateTime)
            Me.CambiarValorVal(value, Me.mFechaProgramadaEmision)
        End Set
    End Property

    Public Property IdFicheroTransferencia() As String
        Get
            Return mIdFicheroTransferencia
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mIdFicheroTransferencia)
        End Set
    End Property

    Public Property ApunteImpDOrigen() As FN.GestionPagos.DN.ApunteImpDDN
        Get
            Return Me.mApunteImpDOrigen
        End Get
        Set(ByVal value As FN.GestionPagos.DN.ApunteImpDDN)


            Me.CambiarValorRef(Of FN.GestionPagos.DN.ApunteImpDDN)(value, Me.mApunteImpDOrigen)
            mGUIDIImporteDebidoOrigen = mApunteImpDOrigen.GUID
            Me.Destinatario = Me.mApunteImpDOrigen.Acreedora
            Me.Deudor = Me.mApunteImpDOrigen.Deudora

        End Set
    End Property




#End Region



#Region "Métodos"

    ''' <summary>
    ''' un pago tine como causa un importe debido o un pago, que asu vez puede tener los mismos origenes
    ''' si un pago tine como origen un importe debido diremos que es un pago original
    ''' si un pago tine como origen otro pago dieremos que el segundo es un pago compensador del primero 
    ''' esta función devulve el importe debido original de la cadena de pagos
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarImportedebidoBase() As FN.GestionPagos.DN.IImporteDebidoDN
        Return RecuperarImportedebidoBasep(Me)
    End Function


    Private Function RecuperarImportedebidoBasep(ByVal pPago As DN.PagoDN) As FN.GestionPagos.DN.IImporteDebidoDN


        If pPago.ApunteImpDOrigen Is Nothing Then
            Return RecuperarImportedebidoBasep(pPago.PagoCompensado)
        Else
            Return pPago.ApunteImpDOrigen
        End If

    End Function


    Public Function GenerarImporteDebidoProducto() As ApunteImpDDN



        If Me.mFechaEfecto = Date.MinValue Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la fecha de efecto debe estar establecida para llamr a este metodo")
        End If

        Dim ap As New ApunteImpDDN

        ap.Acreedora = Me.mDeudor
        ap.Deudora = Me.mDestinatario
        ap.FCreación = Now
        ap.FEfecto = Me.mFechaEfecto
        ap.HuellaIOrigenImpDebDN = New HuellaIOrigenImpDebDN(Me)

        Dim aid As ApunteImpDDN = Me.mColApunteImpDCompensantes.SaldarCol

        If aid Is Nothing Then
            ap.Importe = Me.mImportePago
        Else

            If aid.Deudora.GUID = Me.mDeudor.GUID Then
                ' sentido positivo
                Throw New ApplicationException("no se admiten importes positivos para el saldo")

                'ap.Importe = Me.mImportePago + aid.Importe
            Else
                ' sentido negativo

                ap.Importe = Me.mImportePago + aid.Importe

            End If
        End If

        Me.ApunteImpDProducto = ap

        Return ap
    End Function


    Public Function RegistrarFechaEmisionPagoYaEmitido(ByVal pfecha As Date, ByVal mensaje As String) As Boolean
        ' elapgo no se puede emitir si esta anulado o si ya esta emitido o si ya esta efectuado

        If Me.mFechaAnulacion <> Date.MinValue Then
            mensaje = "El pago esta anulado y no peude emitirse"
            Return False
        End If

        If Me.FechaEfecto <> Date.MinValue Then
            mensaje = "El pago esta efectuado y no puede emitirse"
            Return False
        End If

        If Me.FechaEmision <> Date.MinValue Then
            mensaje = "El pago esta emitido y no peude emitirse de nuevo"
            Return False
        End If

        Me.CambiarValorVal(Of Date)(pfecha, mFechaEmision)
        ' Me.mFechaEmision = pfecha
        Return True
    End Function

    Public Function EmitirPago(ByVal mensaje As String) As Boolean
        ' elapgo no se puede emitir si esta anulado o si ya esta emitido o si ya esta efectuado

        If Me.mFechaAnulacion <> Date.MinValue Then
            mensaje = "El pago esta anulado y no peude emitirse"
            Return False
        End If

        If Me.FechaEfecto <> Date.MinValue Then
            mensaje = "El pago esta efectuado y no puede emitirse"
            Return False
        End If

        If Me.FechaEmision <> Date.MinValue Then
            mensaje = "El pago esta emitido y no peude emitirse de nuevo"
            Return False
        End If

        Me.CambiarValorVal(Of Date)(Now, mFechaEmision)

        Return True
    End Function
    Public Function CrearPagoCompensador() As PagoDN
        Dim pago As New PagoDN()

        pago.PagoCompensado = Me
        pago.ApunteImpDOrigen = Me.ApunteImpDOrigen
        pago.Destinatario = Me.mDeudor
        pago.mDeudor = Me.Destinatario
        pago.Importe = Me.mImportePago

        Return pago

    End Function

    Public Function Anulable(ByRef pMensaje As String) As Boolean Implements IOrigenIImporteDebidoDN.Anulable
        If mFechaEmision <> Date.MinValue Then
            pMensaje = "el pago ha sido emitido y no puede anularse y por ello no es anulable"
            Return False
        End If

        If mFechaEfecto <> Date.MinValue Or Not String.IsNullOrEmpty(mIdFicheroTransferencia) Then
            pMensaje = "el pago ha sido efectuado y no puede anularse"

            Return False
        End If

        Return True

    End Function

    Public Function Efectuable(ByRef pMensaje As String) As Boolean


        If mFechaEfecto <> Date.MinValue Then
            pMensaje = "el pago ha sido efectuado y no volver a efectuarse"
            Return False
        End If
        If Me.mFechaAnulacion <> Date.MinValue Then
            pMensaje = "el pago ha sido anulado y no puede efectuarse"
            Return False
        End If

        Return True

    End Function
    Public Function AnularPago(ByRef pMensaje As String, ByVal pfechaAnulacion As Date) As Boolean
        If Anulable(pMensaje) Then
            Me.mFechaAnulacion = pfechaAnulacion
            'Me.mIImporteDebidoProducto.FAnulacion = pMensaje
            Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado
            Return True
        End If
        Return False
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN


        '' modificacion para que un pago compensado pueda apuntar al importe debido del pago que compensa

        '' Verificar los origennes del importe debido
        If Me.mApunteImpDOrigen Is Nothing Then
            pMensaje = "El importe debido  del pago   no puede ser nulo "
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Me.mApunteImpDOrigen Is Nothing AndAlso Me.mPagoCompensado Is Nothing Then
            pMensaje = "El importe debido  del pago y el pago compesado  no pueden ser nulos "
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not String.IsNullOrEmpty(Me.mApunteImpDOrigen.GUIDAgrupacion) Then
            pMensaje = "El importe debido  del pago  no puede estar referido por una agrupacion "
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If



        If mPagoCompensado IsNot Nothing Then
            ' es un pago compensador
            ' y por lo tanto debe tener un pago que se haya efectuado como pago a compensar
            Dim pagoAcompensar As PagoDN = Me.mPagoCompensado
            If pagoAcompensar.mFechaEfecto = Date.MinValue Then
                Throw New ApplicationException("No se puede compensar un pago que no se ha efectuado")
            End If

        Else
            mGUIDIImporteDebidoOrigen = mApunteImpDOrigen.GUID
        End If






        'si el pago esta pagado debiera de tener un importe debido producto y una fecha de emision menor a ella
        ' If Me.mFechaEfecto <> Date.MinValue AndAlso (Me.mFechaEmision =Date.MinValue OrElse Me.mFechaEfecto < Me.mFechaEmision) Then
        If Me.mFechaEfecto <> Date.MinValue AndAlso (Me.mFechaEfecto < Me.mFechaEmision) Then
            Throw New ApplicationException("Un pago efectuado debe tener una fecha de misión adecuada a su fecha de efecto")

        End If

        If (Me.mFechaEfecto <> Date.MinValue AndAlso Me.mApunteImpDProducto Is Nothing) OrElse (Me.mApunteImpDProducto IsNot Nothing AndAlso Me.mFechaEfecto = Date.MinValue) Then
            Throw New ApplicationException("Un pago efectuado debe tener su importe debido producto")
        End If


        ' si el pago esta naulado su importe debido tambien debiera estar anulado

        If Not mApunteImpDProducto Is Nothing Then

            If Me.mFechaAnulacion <> mApunteImpDProducto.FAnulacion Then
                pMensaje = "las fechas de anulación entre un importe debido y su origen deben ser identicas"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If

        End If
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''


        ' verificar el importe del pago

        If Me.mImportePago = 0 Then
            pMensaje = "El importe del pago no es correcto, es 0"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If
        If Me.mImportePago < 0 Then
            pMensaje = "El importe del pago no es correcto, es <0"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Dim aid As ApunteImpDDN = Me.mColApunteImpDCompensantes.SaldarCol


        If aid Is Nothing Then
            ' no tine importes a compesar
            If mApunteImpDProducto Is Nothing Then
                ' no tine importe debido luego su estado no puede ser poagado


            Else
                ' tiene importe debido luego debe estar pagado

                If Not Me.mImportePago = Me.mApunteImpDProducto.Importe Then
                    Throw New ApplicationException("El importe del pago debe ser igual al valor de su importe debido producto")
                End If

            End If


        Else
            If aid.Deudora.GUID = Me.mDeudor.GUID Then
                ' sentido positivo
                Throw New ApplicationException("El saldo de los importes debidos solo pueden reducir el importe del pago")

            Else

                If mApunteImpDProducto IsNot Nothing Then
                    If Not Me.mImportePago + aid.Importe = Me.mApunteImpDProducto.Importe Then
                        Throw New ApplicationException("El saldo de los importes debidos compensados ")
                    End If
                End If


                ' sentido negativo
            End If
        End If




        'Dim saldoCompensado As Double

        'If Not Me.mIImporteDebidoProducto.Importe = Me.mImportePago - saldoCompensado Then
        '    pMensaje = "El importe del pago no es correcto, debiera cumplir Me.mIImporteDebidoProducto.Importe = Me.mImportePago - saldoCompensado"
        '    Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

        'End If


        ''''''''''''''''''''''''''''''''''''''''''''''''''''''




        If Not Me.mTalon Is Nothing AndAlso Not Me.mTransferencia Is Nothing Then
            pMensaje = "No puede haber al mismo tiempo una Transferencia y un Talón para el mismo Pago"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Me.mDestinatario Is Nothing Then
            pMensaje = "El destinatario no puede ser nulo"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Me.mDeudor Is Nothing Then
            pMensaje = "El deudor no puede ser nulo"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If


        If mPagoCompensado IsNot Nothing Then
            ' pago compensado

            If Not Me.mDestinatario.GUID = Me.mPagoCompensado.Deudor.GUID Then
                pMensaje = "El Destinatario del Pago no coincide con el deudor del pago compenado"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If
            If Not Me.mDeudor.GUID = Me.mPagoCompensado.Destinatario.GUID Then
                pMensaje = "El Destinatario del Pago no coincide con el deudor del importe debido"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If
            If Not Me.mPagoCompensado.FechaEfecto <> Date.MinValue Then
                pMensaje = "No se puede compensar un pago que no ha tenido efecto"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

            End If


        Else
            ' pago normal

            If Me.mApunteImpDOrigen.FAnulacion <> Date.MinValue Then
                pMensaje = "un pago activo no puede estar relacionado con un importe anulado"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If

            If Not Me.mDestinatario.GUID = Me.mApunteImpDOrigen.Acreedora.GUID Then
                pMensaje = "El Destinatario del Pago no coincide con el acreedor del importe debido"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If

            If Not Me.mDeudor.GUID = Me.mApunteImpDOrigen.Deudora.GUID Then
                pMensaje = "El Destinatario del Pago no coincide con el deudor del importe debido"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If

        End If



        'If Me.Destinatario Is Nothing Then
        '    pMensaje = "El Destinatario del Pago no puede estar vacío"
        '    Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        'End If
        'If Me.mOrigen Is Nothing Then
        '    pMensaje = "El origen del pago no puede ser nulo"
        '    Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        'End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

#End Region



#Region "IorigenImporteDebido"

    Public Property ColHEDN() As Framework.DatosNegocio.ColHEDN Implements IOrigenIImporteDebidoDN.ColHEDN
        Get
            Dim al As New Framework.DatosNegocio.ColHEDN
            al.Add(New Framework.DatosNegocio.HEDN(Me.mApunteImpDOrigen))
            Return al
        End Get
        Set(ByVal value As Framework.DatosNegocio.ColHEDN)
        End Set
    End Property

    Public Property FAnulacion() As Date Implements IOrigenIImporteDebidoDN.FAnulacion
        Get
            Return Me.mFechaAnulacion
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(Of Date)(value, Me.mFechaAnulacion)
        End Set
    End Property

    Private Property IImporteDebidoDN1() As IImporteDebidoDN Implements IOrigenIImporteDebidoDN.IImporteDebidoDN
        Get
            Return Me.ApunteImpDOrigen
        End Get
        Set(ByVal value As IImporteDebidoDN)
            ApunteImpDOrigen = value
        End Set
    End Property

#End Region



   

    Public Function Anular(ByVal fAnulacion As Date) As Object Implements IOrigenIImporteDebidoDN.Anular

        Dim mensaje As String
        Return Me.AnularPago(mensaje, fAnulacion)

    End Function
End Class

<Serializable()> Public Class ColPagoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of PagoDN)

#Region "Métodos"

    ''' <summary>
    ''' Método que devuelve una lista de colecciones de pago
    ''' en función de la cuenta de origen del pago
    ''' </summary>
    ''' <returns>Lista con las colecciones de pago separadas por cuenta de origen</returns>
    ''' <remarks></remarks>
    Public Function RecuperarListaColPagosxCuentaOrigen() As List(Of ColPagoDN)
        Dim listaColPagos As New List(Of ColPagoDN)()

        For Each pago As PagoDN In Me
            Dim encontrado As Boolean = False

            For Each colP As ColPagoDN In listaColPagos
                If pago.CuentaOrigenPago.EsIgualRapido(colP.Item(0)) Then
                    colP.Add(pago)
                    encontrado = True
                    Exit For
                End If
            Next

            If Not encontrado Then
                Dim colAux As New ColPagoDN()
                colAux.Add(pago)
                listaColPagos.Add(colAux)
            End If
        Next

        Return listaColPagos

    End Function

    Public Function ImporteTotal() As Double
        For Each mipago As PagoDN In Me
            ImporteTotal += mipago.Importe
        Next
    End Function
    Public Function ImporteDescontadoCompensaciones(ByVal acrredor As FN.Localizaciones.DN.EntidadFiscalGenericaDN) As Double

        Dim saldoAcrredor, SaldoDeudor As Double


        For Each mipago As PagoDN In Me

            If acrredor.GUID = mipago.Destinatario.GUID Then
                saldoAcrredor += mipago.Importe
            Else
                SaldoDeudor += mipago.Importe
            End If

        Next

        Return saldoAcrredor - SaldoDeudor
    End Function

    Public Function SoloDosEntidadesFiscales() As Boolean


        Dim ief1, ief2 As FN.Localizaciones.DN.EntidadFiscalGenericaDN
        ief1 = Me.Item(0).Deudor
        ief2 = Me.Item(0).Destinatario


        For Each ie As PagoDN In Me

            If Not ((ie.Deudor.GUID = ief1.GUID OrElse ie.Deudor.GUID = ief2.GUID) AndAlso (ie.Destinatario.GUID = ief1.GUID OrElse ie.Destinatario.GUID = ief2.GUID)) Then
                Return False
            End If

        Next

        Return True

    End Function



    Public Function SoloDosEntidadesFiscalesMismoSentido() As Boolean


        Dim ief1, ief2 As FN.Localizaciones.DN.EntidadFiscalGenericaDN
        ief1 = Me.Item(0).Deudor
        ief2 = Me.Item(0).Destinatario


        For Each ie As PagoDN In Me

            If Not (((ie.Deudor.GUID = ief1.GUID) OrElse (ie.Deudor.GUID = ief2.GUID)) AndAlso ((ie.Destinatario.GUID = ief1.GUID) OrElse (ie.Destinatario.GUID = ief2.GUID))) Then
                Return False
            End If

        Next

        Return True

    End Function





    Public Function RecuperarColPagos(ByVal pAnulados As FiltroPago, ByVal pEmitidos As FiltroPago, ByVal pEfectuado As FiltroPago) As ColPagoDN



        Dim col As New ColPagoDN
        Dim aceptado As Boolean = True

        For Each pago As PagoDN In Me
            aceptado = True

            If Not pAnulados = FiltroPago.Todos Then

                If (pAnulados = FiltroPago.No) = (pago.FechaAnulacion > Date.MinValue) Then
                    aceptado = False
                End If
            End If
            If Not pEmitidos = FiltroPago.Todos Then
                If aceptado AndAlso (pEmitidos = FiltroPago.No) = (pago.FechaEmision > Date.MinValue) Then
                    aceptado = False
                End If
            End If

            If Not pEfectuado = FiltroPago.Todos Then
                If aceptado AndAlso (pEfectuado = FiltroPago.No) = (pago.FechaEfecto > Date.MinValue) Then
                    aceptado = False
                End If
            End If

            If aceptado Then
                col.Add(pago)
            End If

        Next


        Return col




    End Function



#End Region

End Class


Public Enum FiltroPago
    Todos
    Si
    No
End Enum

Public Enum PosicionPago
    Primero
    Intermedio
    Ultimo
End Enum