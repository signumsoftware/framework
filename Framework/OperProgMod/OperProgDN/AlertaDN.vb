Imports Framework.DatosNegocio
<Serializable()> _
Public Class AlertaDN

    Inherits Framework.Notas.NotasDN.NotaDN


#Region "Atributos"

    Protected mFEjecProgramada As DateTime
    Protected mOperacion As Framework.Procesos.ProcesosDN.OperacionDN
    Protected mDebenEjecutarseTodas As Boolean
    Protected mDebenEjecutaEnMismaTransaccion As Boolean
    Protected mAtendida As Boolean
    ' Protected mNota As Framework.Notas.NotasDN.NotaDN
#End Region


#Region "Constructores"


    'Public Sub New()
    '    CambiarValorRef(Of Framework.Notas.NotasDN.NotaDN)(New Framework.Notas.NotasDN.NotaDN, mNota)
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub


#End Region

#Region "Propiedades"








    '<RelacionPropCampoAtribute("mNota")> _
    'Public Property Nota() As Framework.Notas.NotasDN.NotaDN

    '    Get
    '        Return mNota
    '    End Get

    '    Set(ByVal value As Framework.Notas.NotasDN.NotaDN)
    '        CambiarValorRef(Of Framework.Notas.NotasDN.NotaDN)(value, mNota)

    '    End Set
    'End Property








    Public Property Atendida() As Boolean

        Get
            Return mAtendida
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mAtendida)
            If mAtendida = True Then
                Me.FF = Now
            End If
        End Set
    End Property


    Public Property Operacion() As Framework.Procesos.ProcesosDN.OperacionDN
        Get
            Return Me.mOperacion
        End Get
        Set(ByVal value As Framework.Procesos.ProcesosDN.OperacionDN)
            Me.CambiarValorRef(Of Framework.Procesos.ProcesosDN.OperacionDN)(value, Me.mOperacion)

        End Set
    End Property

    Public Property DebenEjecutarSeTodas() As Boolean
        Get
            Return Me.mDebenEjecutarseTodas
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mDebenEjecutarseTodas)

        End Set
    End Property

    Public Property DebenEjecutaEnMismaTransaccion() As Boolean
        Get
            Return Me.mDebenEjecutaEnMismaTransaccion
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mDebenEjecutaEnMismaTransaccion)

        End Set
    End Property

    Public Property FEjecProgramada() As DateTime
        Get
            Return Me.mFEjecProgramada
        End Get
        Set(ByVal value As DateTime)
            Me.CambiarValorVal(Of Date)(value, Me.mFEjecProgramada)
        End Set
    End Property

    'Public Property ColIHEntidad() As Framework.DatosNegocio.ColHEDN
    '    Get
    '        Return Me.mNota.ColIHEntidad
    '    End Get
    '    Set(ByVal value As Framework.DatosNegocio.ColHEDN)
    '        Me.mNota.ColIHEntidad = value
    '    End Set
    'End Property
#End Region

#Region "Metodos"


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        ' si hay una operacion las huellas a las dn deben de ser del tipo de la que acepta la operación asociada


        If Not ValOperacionDns(pMensaje, Me.mOperacion, Me.mColHEntidad) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If
        If Me.mFEjecProgramada = Date.MinValue Then
            pMensaje = "Una alerta debe de tenr su fecha de ejecucion programada establecida"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


    Public Function ValOperacionDns(ByRef mensaje As String, ByVal pOperacion As Framework.Procesos.ProcesosDN.OperacionDN, ByVal pDns As Framework.DatosNegocio.ColHEDN) As Boolean


        ' si hay una operacion las huellas a las dn deben de ser del tipo de la que acepta la operación asociada
        If pOperacion Is Nothing Then
            Return True
        End If
        For Each hdn As Framework.DatosNegocio.HEDN In Me.ColIHEntidad

            If Not pOperacion.ColDNAceptadas.ContieneTipo(hdn.TipoEntidadReferida) Then
                mensaje = "El tipo " & hdn.TipoEntidadReferida.FullName & " No está aceptado por la operacion " & pOperacion.Nombre
                Return False
            End If

        Next

        Return True

    End Function



#End Region





End Class


Public Class ColAlertaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of AlertaDN)
End Class