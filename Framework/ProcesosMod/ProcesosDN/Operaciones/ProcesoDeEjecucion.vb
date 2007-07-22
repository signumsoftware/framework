<Serializable()> Public Class ProcesoDeEjecucionDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "atributos"
    Private mDataset As DataSet
    Private mTransicionARealizar As TransicionDN
    Private mPorcentajeCompletado As Single
    Private mCompletado As Boolean
    Private mTipoObjeto As System.Type
    Private mPrincipal As Object
#End Region

#Region "constructor"
    Public Sub New()

    End Sub

    Public Sub New(ByVal pPrincipal As Object, ByVal pDatatable As DataTable, ByVal pTransicionARealizar As TransicionDN, ByVal pTipoObjeto As Type)
        Dim mids As New DataSet
        mids.Tables.Add(pDatatable)
        Me.CambiarValorRef(mids, Me.mDataset)
        Me.CambiarValorRef(pTransicionARealizar, Me.mTransicionARealizar)
        Me.CambiarValorVal(DateTime.Now, Me.mPeriodo.FInicio)
        Me.CambiarValorRef(pPrincipal, Me.mPrincipal)
        Me.CambiarValorRef(pTipoObjeto, Me.mTipoObjeto)
        Me.modificarEstado = DatosNegocio.EstadoDatosDN.Modificado
    End Sub
#End Region

#Region "propiedades"
    ''' <summary>
    ''' El Principal que autoriza la ejecución de estas operaciones
    ''' </summary>
    Public Property Principal() As Object
        Get
            Return Me.mPrincipal
        End Get
        Set(ByVal value As Object)
            Me.CambiarValorRef(value, Me.mPrincipal)
        End Set
    End Property

    ''' <summary>
    ''' Accede directamente al datatable del dataset
    ''' </summary>
    Public ReadOnly Property Datatable() As DataTable
        Get
            If Not Me.mDataset Is Nothing AndAlso Me.mDataset.Tables.Count <> 0 Then
                Return Me.mDataset.Tables(0)
            End If
            Return Nothing
        End Get
    End Property

    Public Property Dataset() As DataSet
        Get
            Return Me.mDataset
        End Get
        Set(ByVal value As DataSet)
            Me.CambiarValorRef(value, Me.mDataset)
        End Set
    End Property

    Public Property TransicionARealizar() As TransicionDN
        Get
            Return Me.mTransicionARealizar
        End Get
        Set(ByVal value As TransicionDN)
            Me.CambiarValorRef(value, Me.mTransicionARealizar)
        End Set
    End Property

    Public Property PorcentajeCompletado() As Single
        Get
            Return Me.mPorcentajeCompletado
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mPorcentajeCompletado)
        End Set
    End Property

    Public Property Completado() As Boolean
        Get
            Return Me.mCompletado
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(value, Me.mCompletado)
        End Set
    End Property

    Public ReadOnly Property Ticket() As String
        Get
            Return Me.GUID
        End Get
    End Property
#End Region

#Region "metodos"
    Private Sub PrepararColumnas(ByRef pDatatable As DataTable)
        If Not pDatatable Is Nothing Then
            If (Not pDatatable.Columns.Contains("Operación Ejecutada")) AndAlso (Not pDatatable.Columns("Operación Ejecutada").DataType Is GetType(Boolean)) Then
                pDatatable.Columns.Add(New DataColumn("Operación Ejecutada", GetType(System.Boolean)))
            End If

            If (Not pDatatable.Columns.Contains("Resultado Operación")) AndAlso (Not pDatatable.Columns("Resultado Operación").DataType Is GetType(ResultadoOperacion)) Then
                pDatatable.Columns.Add(New DataColumn("Resultado Operación", GetType(ResultadoOperacion)))
            End If

            If (Not pDatatable.Columns.Contains("Comentario Operación")) AndAlso (Not pDatatable.Columns("Comentario Operación").DataType Is GetType(String)) Then
                pDatatable.Columns.Add(New DataColumn("Comentario Operación", GetType(System.String)))
            End If
        End If
    End Sub

    ''' <summary>
    ''' Recalcula el porcentaje de completado del proceso
    ''' y si está completo lo apunta tb en el atibuto correspondiente
    ''' </summary>
    '''<returns>el valor de la propiedad "completado"</returns>
    ''' <remarks></remarks>
    Public Function ActualizarPorcentajes() As Boolean
        Dim numeroregistros As Integer = Me.mDataset.Tables(0).Rows.Count

        Dim completados As Integer = 0

        For Each mir As DataRow In Me.mDataset.Tables(0).Rows
            If mir("Operación Ejecutada") = True Then
                completados += 1
            End If
        Next

        Me.mPorcentajeCompletado = (numeroregistros * 100) / completados

        If Me.mPorcentajeCompletado = 100 Then
            Me.Completado = True
            Me.Periodo.FF = DateTime.Now
        Else
            Me.Completado = False
        End If

        Return Me.Completado
    End Function

    ''' <summary>
    ''' Devuelve el ID del siguienteobjeto a procesar de la tabla
    ''' </summary>
    Public Function SiguienteIDAProcesar() As String
        For Each mir As DataRow In Me.mDataset.Tables(0).Rows
            If mir("Operación Ejecutada") = False Then
                Return mir(0)
            End If
        Next
        'no quedan operaciones por ejecutar
        Return String.Empty
    End Function

    ''' <summary>
    ''' Introduce en la tabla el resultado de la ejecución de la operación para ese objeto
    ''' </summary>
    ''' <param name="pIDObjeto">el id del objeto sobre el que se ha ejecutado la operación</param>
    ''' <param name="pResultado">el resultado de la ejecución</param>
    ''' <param name="pComentarioOperacion">el comentario que se quiere introducir en la tabla</param>
    Public Sub OperacionEjecutada(ByVal pIDObjeto As String, ByVal pResultado As ResultadoOperacion, ByVal pComentarioOperacion As String)
        For Each mir As DataRow In Me.mDataset.Tables(0).Rows
            If mir(0) = pIDObjeto Then
                mir("Operación Ejecutada") = True
                mir("Resultado Operación") = pResultado
                mir("Comentario Operación") = pComentarioOperacion
            End If
        Next
    End Sub

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        Return MyBase.EstadoIntegridad(pMensaje)
    End Function
#End Region

End Class

Public Enum ResultadoOperacion As Integer
    correcta = 0
    incorrecta = 1
End Enum