''' <summary>
''' Esta clase guarda la informacion sobre una transaccion logica, sus datos asociados, su recurso y el gestor encargado de
''' controlarlos a todos.
''' </summary>
Public Class TransaccionRecursoBDAD
    Implements ITransaccionRecursoLN

#Region "Atributos"
    'Contador global de ids
    Private Shared sID As String 'TODO: ESTO NO DEBERIA SER UN INT???

    'ID del objeto
    Protected mID As String

    'Transaccion logica que contenemos
    Protected mTL As ITransaccionLogicaLN

    'Datos asociados a la transaccion
    Private mDatosTransaccion As IDatosTransaccionGenericoBDAD

    'Recurso sobre el que se desarrolla la transaccion
    Protected mRecurso As IRecursoLN

    'Gestor que controla la transaccion sobre este recurso
    Protected mGTDR As GTDRGenericoBDAD
#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto.</summary>
    ''' <remarks>El constructor asigna un ID unico a cada objeto.</remarks>
    Public Sub New()
        sID += 1
        mID = sID
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene o asigna los datos asociados a la transaccion.</summary>
    Public Property DatosTransaccion() As IEntidadesTransaccRecursoLN Implements ITransaccionRecursoLN.DatosTransaccion
        Get
            Return mDatosTransaccion
        End Get
        Set(ByVal Value As IEntidadesTransaccRecursoLN)
            mDatosTransaccion = Value
        End Set
    End Property

    ''' <summary>Obtiene o asigna el gestor que controla nuestro recurso asociado.</summary>
    Public Property Gestor() As IGTDRLN Implements ITransaccionRecursoLN.Gestor
        Get
            Return mGTDR
        End Get
        Set(ByVal Value As IGTDRLN)
            mGTDR = Value
        End Set
    End Property

    ''' <summary>Obtiene o asigna el ID de la entidad.</summary>
    Public Property ID() As String Implements ITransaccionRecursoLN.ID
        Get
            Return Me.mID
        End Get
        Set(ByVal Value As String)
            Me.mID = Value
        End Set
    End Property

    ''' <summary>Obtiene o asigna la transaccion logica que almacenamos.</summary>
    Public Property TransaccionLogica() As ITransaccionLogicaLN Implements ITransaccionRecursoLN.TransaccionLogica
        Get
            Return mTL
        End Get
        Set(ByVal Value As ITransaccionLogicaLN)
            mTL = Value
        End Set
    End Property

    ''' <summary>Obtiene o asigna el recurso que almacenamos.</summary>
    Public Property Recurso() As IRecursoLN Implements ITransaccionRecursoLN.Recurso
        Get
            Return mRecurso
        End Get
        Set(ByVal Value As IRecursoLN)
            mRecurso = Value
        End Set
    End Property
#End Region

End Class
