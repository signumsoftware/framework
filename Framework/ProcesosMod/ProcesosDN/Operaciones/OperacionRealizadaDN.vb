Imports Framework.DatosNegocio
<Serializable()> Public Class OperacionRealizadaDN
    Inherits EntidadTemporalDN
    Implements IOperacionRealizadaDN





#Region "Atributos"
    ' atributos para controlar las operaciones posibles
    Protected mColSubTransiciones As ColTransicionDN  ' representa las transiciones que pueden realizarse
    Protected mColSubTRIniciadas As ColTransicionRealizadaDN ' representa los caminos que se iniciaron de entre los posibles, ya que en un flujo concurrente no siempretiene que iniciarse todos los caminos
    Protected mColOPRFinalizadasoEnCurso As ColOperacionRealizadaDN ' referencia a las sub operaciones que se generan en flujos concurrentes de las cuales me es la operacion padre
    Protected mOperacionRealizadaPadre As OperacionRealizadaDN  ' este atributo solo debe ser referido mientras la operacion esté en curso es decir  fecha FF<>min value
    '    Protected mGUIDDNenProceso As String ' es el guid de la DN que se esta procesando por esta instancia de flujo o operacion

    Protected mHuellaOI As HEDN ' esta huella debe ser guardada con persistencia contenida


    Protected mOperacion As OperacionDN
    Protected mFechaOperacion As DateTime
    Protected mSujetoOperacion As IEjecutorOperacionDN
    Protected mObjetoIndirectoOperacion As IEntidadDN
    Protected mObjetoDirectoOperacion As Framework.DatosNegocio.IEntidadDN
    Protected mEstadoIOperacionRealizadaDN As EstadoIOperacionRealizadaDN
    Protected mRutaSubordinada As String

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        mFechaOperacion = Now()

        Me.CambiarValorRef(Of ColTransicionDN)(New ColTransicionDN, mColSubTransiciones)
        Me.CambiarValorRef(Of ColTransicionRealizadaDN)(New ColTransicionRealizadaDN, mColSubTRIniciadas)
        Me.CambiarValorRef(Of ColOperacionRealizadaDN)(New ColOperacionRealizadaDN, mColOPRFinalizadasoEnCurso)
        modificarEstado = EstadoDatosDN.Inconsistente

    End Sub

    Public Sub New(ByVal pColSubTransiciones As ColTransicionDN)
        MyBase.New()
        mFechaOperacion = Now()
        Me.CambiarValorRef(Of ColTransicionDN)(pColSubTransiciones, mColSubTransiciones)
        Me.CambiarValorRef(Of ColTransicionRealizadaDN)(New ColTransicionRealizadaDN, mColSubTRIniciadas)
        Me.CambiarValorRef(Of ColOperacionRealizadaDN)(New ColOperacionRealizadaDN, mColOPRFinalizadasoEnCurso)
        modificarEstado = EstadoDatosDN.Inconsistente
    End Sub
#End Region

#Region "Propiedades"


    Public Property ColSubTransiciones() As ColTransicionDN
        Get
            Return mColSubTransiciones
        End Get
        Set(ByVal value As ColTransicionDN)
            CambiarValorCol(Of ColTransicionDN)(value, mColSubTransiciones)
        End Set
    End Property


    Public Property ColOPRFinalizadasoEnCurso() As ColOperacionRealizadaDN
        Get
            Return mColOPRFinalizadasoEnCurso
        End Get
        Set(ByVal value As ColOperacionRealizadaDN)
            CambiarValorCol(Of ColOperacionRealizadaDN)(value, mColOPRFinalizadasoEnCurso)
        End Set
    End Property

#End Region

#Region "Propiedades IOperacionNegocioDN"

    Public Property FechaOperacion() As Date Implements IOperacionRealizadaDN.FechaOperacion
        Get
            Return mFechaOperacion
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaOperacion)
        End Set
    End Property



    Public Property SujetoOperacion() As IEjecutorOperacionDN Implements IOperacionRealizadaDN.SujetoOperacion
        Get
            Return mSujetoOperacion
        End Get
        Set(ByVal value As IEjecutorOperacionDN)
            Dim mensaje As String
            mensaje = ""

            If Not ValSujetoOperacion(mensaje, value) Then
                Throw New ApplicationExceptionDN(mensaje)
            End If

            CambiarValorRef(Of IEntidadDN)(value, mSujetoOperacion)
        End Set
    End Property

    Public Property ObjetoIndirectoOperacion() As IEntidadDN Implements IOperacionRealizadaDN.ObjetoIndirectoOperacion
        Get
            Return mObjetoIndirectoOperacion
        End Get
        Set(ByVal value As IEntidadDN)
            Dim mensaje As String
            mensaje = ""

            If Not ValObjetoIndirecto(mensaje, value) Then
                Throw New ApplicationExceptionDN(mensaje)
            End If


            CambiarValorRef(Of IEntidadDN)(New HEDN(value, HuellaEntidadDNIntegridadRelacional.ninguna, Nothing), mHuellaOI)
            CambiarValorRef(Of IEntidadDN)(value, mObjetoIndirectoOperacion)
        End Set
    End Property

    Public Property ObjetoDirectoOperacion() As Framework.DatosNegocio.IEntidadDN Implements IOperacionRealizadaDN.ObjetoDirectoOperacion
        Get
            Return mObjetoDirectoOperacion
        End Get
        Set(ByVal value As Framework.DatosNegocio.IEntidadDN)
            CambiarValorRef(Of Framework.DatosNegocio.IEntidadDN)(value, mObjetoDirectoOperacion)

        End Set
    End Property


    Public Property ObjetoIndirectoNoModificable() As Boolean Implements IOperacionDN.ObjetoIndirectoNoModificable
        Get
            Return Me.mOperacion.ObjetoIndirectoNoModificable
        End Get
        Set(ByVal value As Boolean)
            Me.mOperacion.ObjetoIndirectoNoModificable = value
        End Set
    End Property

#End Region

#Region "Validaciones"

    'Protected Overrides Function ValVerboOperacion(ByRef mensaje As String, ByVal verbo As VerboDN) As Boolean


    '    If Not MyBase.ValVerboOperacion(mensaje, verbo) Then
    '        Return False
    '    End If


    '    'If Me.mSujetoOperacion IsNot Nothing Then
    '    '    Return ValSujetoOperacion(mensaje, mSujetoOperacion)
    '    'End If

    '    Return True

    'End Function

    Protected Overridable Function ValSujetoOperacion(ByRef mensaje As String, ByVal pSujeto As IEjecutorOperacionDN) As Boolean
        If pSujeto Is Nothing Then
            mensaje = "El sujeto de la operación de negocio no puede ser nulo"
            Return False
        End If



        If Not pSujeto.ColOperaciones.Contiene(Me.mOperacion, CoincidenciaBusquedaEntidadDN.Todos) Then
            mensaje = " el sujeto de esta operación no contiene el verbo entre sus autorizados"
            Return False
        End If


        Return True


    End Function

    Protected Overridable Function ValObjetoIndirecto(ByRef mensaje As String, ByVal objetoIndirecto As IEntidadDN) As Boolean
        If objetoIndirecto Is Nothing Then
            mensaje = "El objeto indirecto de la operación de negocio no puede ser nulo"
            Return False
        End If

        Return True

    End Function



#End Region

#Region "Métodos"
    Protected Sub ActualizarRutaSubordinada(ByVal pmOperacionRealizadaPadre As OperacionRealizadaDN)

        If pmOperacionRealizadaPadre Is Nothing Then
            Me.CambiarValorVal(Of String)(Me.mGUID, mRutaSubordinada)

        Else
            Me.CambiarValorVal(Of String)(pmOperacionRealizadaPadre.RutaSubordinada & "/" & Me.mGUID, mRutaSubordinada)

        End If


    End Sub

    Public Function ActualizarEstadoColOPRFinalizadasoEnCurso(ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN)


        If pTransicionRealizada Is Nothing Then
            Throw New ApplicationExceptionDN("la trnasicion realizada no pudede ser nothin")
        End If

        If pTransicionRealizada.OperacionRealizadaOrigen Is Nothing Then
            Throw New ApplicationExceptionDN("OperacionRealizadaOrigen realizada no pudede ser nothin")
        End If


        ' dentificar de que tipo de transicion se trata Inicio , Normal Finalizacion


        ' DE INICIO
        If pTransicionRealizada.Transicion.TipoTransicion = TipoTransicionDN.Inicio OrElse pTransicionRealizada.Transicion.TipoTransicion = TipoTransicionDN.InicioDesde OrElse pTransicionRealizada.Transicion.TipoTransicion = TipoTransicionDN.InicioObjCreado OrElse pTransicionRealizada.Transicion.TipoTransicion = TipoTransicionDN.Subordianda Then
            ' yo debo ser el padre de la destino y debo ser la oepracion de origen
            ' y debo añadir el destino

            If pTransicionRealizada.OperacionRealizadaOrigen IsNot Me Then
                Throw New ApplicationExceptionDN("OperacionRealizadaOrigen debiera de ser yo")
            End If

            If pTransicionRealizada.OperacionRealizadaDestino.OperacionPadre IsNot Me Then
                Throw New ApplicationExceptionDN("ptr.OperacionRealizadaDestino.OperacionPadre debiera de ser yo")
            End If

            If Not Me.mColSubTRIniciadas.Contiene(pTransicionRealizada, CoincidenciaBusquedaEntidadDN.Todos) Then
                Me.mColSubTRIniciadas.Add(pTransicionRealizada)
                Me.mColOPRFinalizadasoEnCurso.Add(pTransicionRealizada.OperacionRealizadaDestino)
            Else
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("solo puede exitri una presencia a este nivel")
            End If

            Exit Function
        End If




        If pTransicionRealizada.EsFinalizacion Then

            ' el padre y el destino debiera ser yo
            ' y debo quitarme el origen


            If pTransicionRealizada.OperacionRealizadaDestino IsNot Me Then
                Throw New ApplicationExceptionDN("en una operacion de finalizacion OperacionRealizadaDestino debiera de ser yo")
            End If


            If pTransicionRealizada.OperacionRealizadaOrigen.OperacionPadre IsNot Me Then
                Throw New ApplicationExceptionDN("en una operacion de finalizacion  OperacionRealizadaOrigen.OperacionPadre debiera de ser yo")
            End If



            If Not pTransicionRealizada.OperacionRealizadaOrigen.EstadoActividad = ProcesosDN.EstadoActividad.Cerrada Then
                Throw New ApplicationExceptionDN("en una operacion de finalizacion  ptr.OperacionRealizadaOrigen.EstadoActividad debe ser cerada")
            End If

            Dim encontrados As Int64 = Me.mColOPRFinalizadasoEnCurso.EliminarEntidadDN(pTransicionRealizada.OperacionRealizadaOrigen, CoincidenciaBusquedaEntidadDN.Todos).Count


            Select Case encontrados

                Case Is = 0
                    Throw New ApplicationExceptionDN("se debió encontrar una OperacionRealizadaOrigen")

                Case Is > 1
                    Throw New ApplicationExceptionDN("se encontrarons " & encontrados & "  OperacionRealizadaOrigen, solo debia haber una")

                Case Is = 1
                    'no debo haver nada en este caso
            End Select
            Exit Function



        Else


            ' se trata de una transicion normal donde el origen y el destino son subordiandas mias

            ' luego el padre de ambas debo  ser yo
            If pTransicionRealizada.OperacionRealizadaDestino.OperacionPadre IsNot Me Then
                Throw New ApplicationExceptionDN("en una transicion normal  el padre de OperacionRealizadaDestino debiera de ser yo")
            End If

            If pTransicionRealizada.OperacionRealizadaOrigen.OperacionPadre IsNot Me Then
                Throw New ApplicationExceptionDN("en una transicion normal  el padre de OperacionRealizadaOrigen debiera de ser yo")
            End If


            ' me queito el origen que debe de haber uno solo
            Dim encontrados As Int64 = Me.mColOPRFinalizadasoEnCurso.EliminarEntidadDN(pTransicionRealizada.OperacionRealizadaOrigen, CoincidenciaBusquedaEntidadDN.Todos).Count


            Select Case encontrados

                Case Is = 0
                    Throw New ApplicationExceptionDN("se debió encontrar una OperacionRealizadaOrigen")

                Case Is > 1
                    Throw New ApplicationExceptionDN("se encontrarons " & encontrados & "  OperacionRealizadaOrigen, solo debia haber una")

                Case Is = 1
                    ' el caso correcto, añado el destino
                    Me.mColOPRFinalizadasoEnCurso.Add(pTransicionRealizada.OperacionRealizadaDestino)


            End Select





        End If















    End Function
    Public Function PreparadaParaEjecutatr() As Boolean

        ' se podrá iniciar si no hay operaciones en curso y si todas las transiciones requeirdas estan en las iniciadas



        If Not Me.mColOPRFinalizadasoEnCurso.Count = 0 Then
            Return False
        End If

        For Each tran As ProcesosDN.TransicionDN In mColSubTransiciones

            If tran.SubordinadaRequerida Then
                If Not Me.mColSubTRIniciadas.ContieneTransicion(tran) Then
                    Return False
                End If

            End If

        Next



        Return True
    End Function


    Public Function TerminarOPR() As Boolean


        ' condiciones para la finalizacion
        If mColOPRFinalizadasoEnCurso.Count > 0 Then
            Return False
        End If

        For Each tran As ProcesosDN.TransicionDN In Me.mColSubTransiciones
            If tran.Automatica AndAlso Not Me.mColSubTRIniciadas.Contiene(tran, CoincidenciaBusquedaEntidadDN.Todos) Then
                Return False
            End If
        Next

        Me.mPeriodo.FFinal = Now
        Me.EstadoIOperacionRealizada = EstadoIOperacionRealizadaDN.Terminada

        '' si mi destino solo es mi padre, solicito a mi padre que se termine
        'If Not mOperacionRealizadaPadre Is Nothing AndAlso UnicoDestinoOPPadre() Then
        '    mOperacionRealizadaPadre.FinalizarOPR(Me)
        'End If


        Return True
    End Function

    Public Function UnicoDestinoMiOprPadre(ByVal pcoltran As ColTransicionDN) As Boolean



        If pcoltran Is Nothing Then
            Return False
        End If

        If pcoltran.Count <> 1 Then
            Return False
        End If

        Dim tran As TransicionDN = pcoltran(0)
        If tran.OperacionOrigen.GUID = Me.Operacion.GUID AndAlso tran.OperacionDestino.GUID = Me.OperacionPadre.Operacion.GUID Then
            Return True
        Else
            Return False
        End If


        'For Each tran As TransicionDN In pcoltran


        '    If tran.OperacionOrigen.GUID = Me.mGUID AndAlso tran.OperacionDestino.GUID = Me.OperacionPadre.Operacion.GUID Then
        '    Else
        '        Return False
        '    End If

        'Next

        ' Return True




    End Function




    ''' <summary>
    ''' metodo invocado en el padre de la operacion cuando una operacion subordinmada tiene una transicion de finalizacion
    ''' </summary>
    ''' <param name="pSubOperacionSolicitante"></param>
    ''' <remarks></remarks>
    Protected Sub FinalizarOPR(ByVal pSubOperacionSolicitante As OperacionRealizadaDN)

        If Not pSubOperacionSolicitante.OperacionPadre Is Me Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("")
        End If

        ' la  transicion de finalizacionde finalizacio
        If Not mColOPRFinalizadasoEnCurso.Remove(pSubOperacionSolicitante) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("")
        End If

        '  If mColOPRFinalizadasoEnCurso.Count = 0 Then
        TerminarOPR()
        '   End If
    End Sub

    Public Sub FinalizarOPRPadre()
        If mOperacionRealizadaPadre Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("Esta operacion no es subordinada, por lo que no puede finalizar a su padre")
        End If
        Me.mOperacionRealizadaPadre.FinalizarOPR(Me)
    End Sub



    ''' <summary>
    ''' recupera la coleccion de operaciones en curso incluyendo la de sus hijas,
    ''' que serviran para poder obtener las operaciones posibles a realizar recorriendo su transiciones autorizadas
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarColRecursivaUltimasOperacionRealizada() As ColOperacionRealizadaDN

        Dim col As New ColOperacionRealizadaDN

        For Each opr As OperacionRealizadaDN In mColOPRFinalizadasoEnCurso





            If opr.EstadoActividad = ProcesosDN.EstadoActividad.Cerrada AndAlso opr.GUID <> Me.GUID Then
                col.Add(opr)
            End If


            ' si la operacion esta en estado iniciada   ella no es una operacion posible pero si sus  sub operaciones

            If opr.EstadoActividad = ProcesosDN.EstadoActividad.Iniciada Then
                col.Add(opr)
                col.AddRange(opr.RecuperarColRecursivaUltimasOperacionRealizada)

            End If


            'If opr.EstadoIOperacionRealizada = EstadoIOperacionRealizadaDN.Terminada AndAlso opr.GUID <> Me.GUID Then
            '    col.Add(opr)
            'End If


            '' si la operacion esta en estado iniciada   ella no es una operacion posible pero si sus  sub operaciones

            'If opr.EstadoIOperacionRealizada = EstadoIOperacionRealizadaDN.Iniciada Then
            '    col.AddRange(opr.RecuperarColRecursivaUltimasOperacionRealizada)
            'End If

        Next

        Return col


    End Function






    Public Function RecuperarColRecursivaUltimasOperacionRealizadaNoIniciadas() As ColOperacionRealizadaDN

        Dim col As New ColOperacionRealizadaDN

        For Each opr As OperacionRealizadaDN In mColOPRFinalizadasoEnCurso





            If opr.EstadoActividad = ProcesosDN.EstadoActividad.Cerrada AndAlso opr.GUID <> Me.GUID Then
                col.Add(opr)
            End If


            ' si la operacion esta en estado iniciada   ella no es una operacion posible pero si sus  sub operaciones

            If opr.EstadoActividad = ProcesosDN.EstadoActividad.Iniciada Then
                col.Add(opr)

                ' recuperar las subordinadas las subordinadas
                Dim colOPr As ProcesosDN.ColOperacionRealizadaDN = opr.RecuperarColRecursivaUltimasOperacionRealizada

                ' descontar las subordinadas iniciadas
                For Each TransicionRealizadaSubordinada As TransicionRealizadaDN In opr.ColTRIniciadas
                    colOPr.EliminarEntidadDNxGUID(TransicionRealizadaSubordinada.OperacionRealizadaDestino.GUID)
                Next


                col.AddRange(opr.RecuperarColRecursivaUltimasOperacionRealizadaNoIniciadas)

            End If


            'If opr.EstadoIOperacionRealizada = EstadoIOperacionRealizadaDN.Terminada AndAlso opr.GUID <> Me.GUID Then
            '    col.Add(opr)
            'End If


            '' si la operacion esta en estado iniciada   ella no es una operacion posible pero si sus  sub operaciones

            'If opr.EstadoIOperacionRealizada = EstadoIOperacionRealizadaDN.Iniciada Then
            '    col.AddRange(opr.RecuperarColRecursivaUltimasOperacionRealizada)
            'End If

        Next

        Return col


    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN
        'If Not ValVerboOperacion(pMensaje, mVerboOperacion) Then
        '    Return EstadoIntegridadDN.Inconsistente
        'End If

        If Not ValSujetoOperacion(pMensaje, mSujetoOperacion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValObjetoIndirecto(pMensaje, mObjetoIndirectoOperacion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.ActualizarRutaSubordinada(Me.mOperacionRealizadaPadre)

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

#End Region

#Region "Propiedades"

    Public ReadOnly Property ColRecursivaUltimasOperacionRealizada() As ColOperacionRealizadaDN
        Get
            Return mColOPRFinalizadasoEnCurso
        End Get
    End Property

    Public ReadOnly Property IniciadasTRSubordinadas() As Int16
        Get
            Return mColSubTRIniciadas.Count
        End Get
    End Property

    Public ReadOnly Property ColTRIniciadas() As ColTransicionRealizadaDN
        Get
            Return mColSubTRIniciadas
        End Get
    End Property

    Public Property OperacionPadre() As OperacionRealizadaDN
        Get
            Return Me.mOperacionRealizadaPadre
        End Get
        Set(ByVal value As OperacionRealizadaDN)

            Me.CambiarValorRef(Of OperacionRealizadaDN)(value, mOperacionRealizadaPadre)
            ActualizarRutaSubordinada(mOperacionRealizadaPadre)
        End Set
    End Property





    Public Property Operacion() As OperacionDN
        Get
            Return Me.mOperacion
        End Get
        Set(ByVal value As OperacionDN)
            CambiarValorRef(Of OperacionDN)(value, mOperacion)

        End Set
    End Property

    Public Property VerboOperacion() As VerboDN Implements IOperacionDN.VerboOperacion
        Get
            Return mOperacion.VerboOperacion
        End Get
        Set(ByVal value As VerboDN)

        End Set
    End Property

    Public Property EstadoIOperacionRealizada() As EstadoIOperacionRealizadaDN Implements IOperacionRealizadaDN.EstadoIOperacionRealizada
        Get
            Return mEstadoIOperacionRealizadaDN
        End Get
        Set(ByVal value As EstadoIOperacionRealizadaDN)
            'todo: poner la logica  de validacion en la signacion del estado
            Me.CambiarValorVal(Of EstadoIOperacionRealizadaDN)(value, mEstadoIOperacionRealizadaDN)
        End Set
    End Property


    Public ReadOnly Property EstadoActividad() As EstadoActividad
        Get

            If Me.FF = Me.FI AndAlso Me.FI = Date.MinValue Then
                Return ProcesosDN.EstadoActividad.Creada
            End If

            If Me.FI > Me.FF Then
                Return ProcesosDN.EstadoActividad.Iniciada
            End If

            If Me.FF > Me.FI Then
                Return ProcesosDN.EstadoActividad.Cerrada
            End If
        End Get
    End Property


    Public ReadOnly Property RutaSubordinada() As String Implements IOperacionRealizadaDN.RutaSubordinada
        Get
            Return mRutaSubordinada
        End Get
    End Property
#End Region


    Public Function RecuperarPadreGrafo() As IOperacionRealizadaDN
        If Me.OperacionPadre Is Nothing Then
            Return Me
        Else
            Return Me.OperacionPadre.RecuperarPadreGrafo()
        End If
    End Function


    Public Sub AsignarOIenGrafo(ByVal oi As Object) Implements IOperacionRealizadaDN.AsignarOIenGrafo
        Dim opp As OperacionRealizadaDN = RecuperarPadreGrafo()
        opp.AsignarOIenGrafoDescendente(oi)

  

    End Sub

    Public Sub AsignarOIenGrafoDescendente(ByVal oi As Object)
        If Me.ObjetoIndirectoOperacion Is oi Then
            Exit Sub
        End If

        Me.ObjetoIndirectoOperacion = oi

        For Each tran As TransicionRealizadaDN In Me.ColTRIniciadas

            tran.OperacionRealizadaDestino.AsignarOIenGrafoDescendente(oi)
            tran.OperacionRealizadaOrigen.AsignarOIenGrafoDescendente(oi)


        Next

        'For Each tran As TransicionRealizadaDN In Me.ColSubTransiciones

        '    tran.OperacionRealizadaDestino.AsignarOIenGrafoDescendente(oi)
        '    tran.OperacionRealizadaOrigen.AsignarOIenGrafoDescendente(oi)


        'Next
        For Each op As OperacionRealizadaDN In Me.ColRecursivaUltimasOperacionRealizada
            op.AsignarOIenGrafoDescendente(oi)
        Next
        For Each op As OperacionRealizadaDN In Me.ColOPRFinalizadasoEnCurso
            op.AsignarOIenGrafoDescendente(oi)
        Next


    End Sub
End Class






<Serializable()> Public Class ColOperacionRealizadaDN
    Inherits ArrayListValidable(Of OperacionRealizadaDN)

    'TODO: Retorna una colección de operaciones realizadas en lugar de una colección
    'de operaciones
    Public Function RecuperarColUltimasOperacionRealizada() As ColOperacionRealizadaDN
        Dim colOpR As New ColOperacionRealizadaDN

        For Each opr As OperacionRealizadaDN In Me
            colOpR.AddRange(opr.RecuperarColRecursivaUltimasOperacionRealizada())
        Next

        Return colOpR

    End Function
    Public Function RecuperarColRecursivaUltimasOperacionRealizadaNoIniciadas() As ColOperacionRealizadaDN
        Dim colOpR As New ColOperacionRealizadaDN

        For Each opr As OperacionRealizadaDN In Me
            colOpR.AddRange(opr.RecuperarColRecursivaUltimasOperacionRealizadaNoIniciadas())
        Next

        Return colOpR

    End Function
End Class



'Public Class pruebaop
'    Inherits Framework.DatosNegocio.EntidadDN
'End Class


'Public Class OperacionRealizadaDNPrueba
'    Inherits OperacionRealizadaDN(Of pruebaop)
'End Class