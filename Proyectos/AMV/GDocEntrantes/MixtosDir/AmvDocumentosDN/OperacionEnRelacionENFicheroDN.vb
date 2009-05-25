<Serializable()> _
Public Class OperacionEnRelacionENFicheroDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

    Protected mRelacionENFichero As RelacionENFicheroDN
    Protected mOperador As OperadorDN
    Protected mTipoOperacionREnFDN As TipoOperacionREnFDN
    Protected mCancelada As Boolean
    Protected mComentario As String
    Protected mValorClasificacionCanalEntrada As String
    Protected mDatosOperacion As String
    Protected mTipoCanal As TipoCanalDN
    Public Sub New()
        Me.mPeriodo.FInicio = Now
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pOperador As OperadorDN, ByVal pRelacionENFichero As RelacionENFicheroDN, ByVal pTipoOperacionREnFDN As TipoOperacionREnFDN)
        Me.mPeriodo.FInicio = Now
        Me.CambiarValorRef(Of OperadorDN)(pOperador, mOperador)
        Me.CambiarValorRef(Of RelacionENFicheroDN)(pRelacionENFichero, mRelacionENFichero)
        Me.CambiarValorRef(Of TipoOperacionREnFDN)(pTipoOperacionREnFDN, mTipoOperacionREnFDN)

        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub
    Public Property TipoCanal() As TipoCanalDN
        Get
            Return mTipoCanal
        End Get
        Set(ByVal value As TipoCanalDN)
            Me.CambiarValorRef(Of TipoCanalDN)(value, mTipoCanal)
        End Set
    End Property
    Public Property DatosOperacion() As String
        Get
            Return mDatosOperacion
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mDatosOperacion)
        End Set
    End Property
    Public Property ComentarioOperacion() As String
        Get
            Return mComentario
        End Get
        Set(ByVal value As String)
            Me.CambiarValorPropiedadVal(value, mComentario)
        End Set
    End Property

    Public Property TipoOperacionREnF() As TipoOperacionREnFDN
        Get
            Return Me.mTipoOperacionREnFDN
        End Get
        Set(ByVal value As TipoOperacionREnFDN)
            Me.CambiarValorRef(Of TipoOperacionREnFDN)(value, Me.mTipoOperacionREnFDN)
        End Set
    End Property

    Public Property RelacionENFichero() As RelacionENFicheroDN
        Get
            Return mRelacionENFichero
        End Get
        Set(ByVal value As RelacionENFicheroDN)
            Me.CambiarValorRef(Of RelacionENFicheroDN)(value, mRelacionENFichero)

        End Set
    End Property

    Public Property Operador() As OperadorDN
        Get
            Return mOperador
        End Get
        Set(ByVal value As OperadorDN)
            Me.CambiarValorRef(Of OperadorDN)(value, mOperador)

        End Set
    End Property

    Public Property Cancelada() As Boolean
        Get
            Return mCancelada
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mCancelada)
        End Set
    End Property

    ' si la operacion esta cancelada no se permite su cierre
    Public Sub EjecutarOperacionEndatos()


        If Not mCancelada Then
            'If mFF = Date.MinValue Then
            Me.mPeriodo.FFinal = Now
            ' End If

            Select Case Me.mTipoOperacionREnFDN.Valor

                Case AmvDocumentosDN.TipoOperacionREnF.Anular
                    Me.mRelacionENFichero.Anular()

                Case AmvDocumentosDN.TipoOperacionREnF.Incidentar
                    Me.mRelacionENFichero.Incidentar()

                Case AmvDocumentosDN.TipoOperacionREnF.Rechazar
                    ' no cambia el estado

                Case AmvDocumentosDN.TipoOperacionREnF.Crear
                    'Me.mRelacionENFichero.Cerrar()
                    'mFF = Now

                Case AmvDocumentosDN.TipoOperacionREnF.Clasificar
                    Me.mRelacionENFichero.Clasificar()

                Case AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar
                    Me.mRelacionENFichero.Cerrar()

           

            End Select






        End If
    End Sub

    Public Sub FijarEstadoRelacion(ByVal estadoRel As EstadosRelacionENFichero)


        If Me.mTipoOperacionREnFDN.Valor = AmvDocumentosDN.TipoOperacionREnF.FijarEstado Then

            Me.mRelacionENFichero.FijarFF(Date.MinValue)
            Select Case estadoRel

                Case EstadosRelacionENFichero.Anulado
                    Me.mRelacionENFichero.Anular()
                Case EstadosRelacionENFichero.Incidentado
                    Me.mRelacionENFichero.Incidentar()

                Case EstadosRelacionENFichero.Cerrado
                    Me.mRelacionENFichero.Cerrar()

                Case EstadosRelacionENFichero.Clasificando
                    Me.mRelacionENFichero.Clasificar()

                Case EstadosRelacionENFichero.Creada
                    Me.mRelacionENFichero.Crear()
            End Select

        Else
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("Estado de operacion incorrecto debiera de ser  " & AmvDocumentosDN.TipoOperacionREnF.FijarEstado)
        End If


    End Sub

End Class

<Serializable()> _
Public Class ColOperacionEnRelacionENFicheroDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of OperacionEnRelacionENFicheroDN)
End Class
