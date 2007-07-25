#Region "Importaciones"

Imports System.Collections.Generic

#End Region

''' <summary>Esta clase representa un gestor de transacciones distribuidas.</summary>
''' <remarks>El gestor se encarga de iniciar, confirmar y cancelar todas las transacciones sobre un recurso determinado.</remarks>
Public Class GTDRGenericoBDAD
    Implements IGTDRLN

#Region "Atributos"
    'Hashtable global donde guardamos los diferentes gestores que hemos instanciado
    Private Shared mHTGestores As Dictionary(Of String, GTDRGenericoBDAD)

    'Transacciones que maneja el gestor de transacciones distribuidas
    Protected mHTTransacciones As Dictionary(Of String, ITransaccionRecursoLN)

    'Recurso sobre el que se realizan las transacciones
    Protected mRecurso As IRecursoLN
#End Region

#Region "Constructores Estaticos"
    ''' <summary>Constructor estatico.</summary>
    ''' <remarks>Crea la Hashtable global de gestores.</remarks>
    Shared Sub New()
        mHTGestores = New Dictionary(Of String, GTDRGenericoBDAD)
    End Sub
#End Region

#Region "Constructores"
    ''' <summary>Constructor por parametros.</summary>
    ''' <param name="pRecurso" type="IRecurso">
    ''' Recurso del que vamos a controlar las transacciones.
    ''' </param>
    Private Sub New(ByVal pRecurso As IRecursoLN)
        mHTTransacciones = New Dictionary(Of String, ITransaccionRecursoLN)
        Me.mRecurso = pRecurso
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene el recurso sobre el que trabaja el gestor.</summary>
    Public ReadOnly Property Recurso() As IRecursoLN Implements IGTDRLN.Recurso
        Get
            Return mRecurso
        End Get
    End Property
#End Region

#Region "Metodos"
    ''' <summary>Metodo que obtiene un gestor para un recurso determinado.</summary>
    ''' <remarks>
    ''' Se comprueba si ya existia un gestor para este recurso, en caso afirmativo se devuelve la misma instancia. En caso
    ''' contrario se crea un nuevo gestor.
    ''' </remarks>
    ''' <param name="pRecurso" type="IRecurso">
    ''' Recurso del que queremos obtener un gestor.
    ''' </param>
    ''' <returns>Gestor sobre el recurso indicado.</returns>
    Public Shared Function CrearInstancia(ByVal pRecurso As IRecursoLN) As GTDRGenericoBDAD
        Dim gr As GTDRGenericoBDAD

        'Si se contine el gestor para ese recurso se devuelve
        If (Not pRecurso Is Nothing AndAlso mHTGestores.ContainsKey(pRecurso.ID)) Then
            Return mHTGestores.Item(pRecurso.ID)

            'Si no se crea el gestor del recurso
        Else
            gr = New GTDRGenericoBDAD(pRecurso)
            mHTGestores.Add(pRecurso.ID, gr)
            Return gr
        End If
    End Function

    ''' <summary>Metodo que cancela una transaccion sobre un recurso.</summary>
    ''' <param name="pTR" type="ITransaccionRecurso">
    ''' Objeto que contiene los datos del recurso y la transaccion que queremos cancelar.
    ''' </param>
    Public Sub CancelarTransaccion(ByVal pTR As ITransaccionRecursoLN) Implements IGTDRLN.CancelarTransaccion
        Dim dt As DatosTransaccionGenericoBDAD = Nothing



        'Si el objeto no es nothing y somos el gestor encargado de manejar esta transaccion la cancelamos
        If (Not pTR Is Nothing) Then
            If (Me.mHTTransacciones.ContainsKey(pTR.ID)) Then
                Try
                    dt = pTR.DatosTransaccion()
                    'Debug.WriteLine("Transaccion Cancelando:" & dt.GetHashCode)

                    dt.Transaccion.Rollback()
                    'Debug.WriteLine("Transaccion cancelada:" & dt.GetHashCode)

                Catch ex As Exception
                    'Debug.WriteLine("Transaccion Cancelando error:" & dt.GetHashCode)

                    Throw ex

                Finally
                    CerrarConexion(dt)

                End Try
            End If
        End If
    End Sub



    Public Sub CerrarConexion(ByVal dt As DatosTransaccionGenericoBDAD)

        If dt Is Nothing Then
            Throw New ApplicationExceptionAD("DatosTransaccionGenericoBDAD no puede ser nulo")
        End If


        Dim conexion As Object

        Try

            conexion = dt.Conexion


            '  If dt IsNot Nothing AndAlso dt.Conexion IsNot Nothing AndAlso dt.Conexion.State <> ConnectionState.Closed Then
            If dt.Conexion IsNot Nothing AndAlso dt.Conexion.State <> ConnectionState.Closed Then

                'Debug.WriteLine("Conexión Cerrando:" & dt.GetHashCode & " conexion: " & conexion.GetHashCode)
                dt.Conexion.Close()
                dt.Conexion.Dispose()
                dt.Conexion = Nothing
                'Debug.WriteLine("Conexión cerrada:" & dt.GetHashCode & " conexion: " & conexion.GetHashCode)

            Else

                'Debug.WriteLine("Conexión no Cerrada:" & dt.GetHashCode & " conexion: " & conexion.GetHashCode)



            End If
        Catch ex As Exception

        End Try

    End Sub

    ''' <summary>Metodo que confirma una transaccion sobre un recurso.</summary>
    ''' <param name="pTR" type="ITransaccionRecurso">
    ''' Objeto que contiene los datos del recurso y la transaccion que queremos confirmar.
    ''' </param>
    Public Sub ConfirmarTransaccion(ByVal pTR As ITransaccionRecursoLN) Implements IGTDRLN.ConfirmarTransaccion
        Dim dt As DatosTransaccionGenericoBDAD = Nothing

        'Si el objeto no es nothing y somos el gestor encargado de manejar esta transaccion la confirmamos
        If (Not pTR Is Nothing) Then
            If (Me.mHTTransacciones.ContainsKey(pTR.ID)) Then
                Try
                    dt = pTR.DatosTransaccion()

                    'Debug.WriteLine("transaccion confirmando:" & dt.GetHashCode)
                    dt.Transaccion.Commit()
                    'Debug.WriteLine("transaccion confirmada:" & dt.GetHashCode)

                Catch ex As Exception
                    'Debug.WriteLine("transaccion confirmando error:" & dt.GetHashCode)
                    Dim nex As ApplicationException = New ApplicationExceptionAD("Posible error en la gestión de transacciones para un meto LN " & ex.Message, ex)

                    Throw nex
                Finally
                    CerrarConexion(dt)
                End Try
            End If
        End If
    End Sub

    ''' <summary>Metodo que inicia una transaccion sobre un recurso.</summary>
    ''' <param name="pTLPadre" type="ITransaccionLogica">
    ''' El padre de la transaccion que queremos inicializar
    ''' </param>
    ''' <returns>
    ''' La transaccion que hemos inicializado y el recurso al que va asociada en forma de un objeto ITransaccionRecurso.
    ''' </returns>
    Private Function IniciarTransaccion(ByVal pTLPadre As ITransaccionLogicaLN) As ITransaccionRecursoLN Implements IGTDRLN.IniciarTransaccion
        Dim fac As IFactoriaAD
        Dim tr As ITransaccionRecursoLN
        Dim trbd As TransaccionRecursoBDAD
        Dim dtr As DatosTransaccionGenericoBDAD
        Dim cn As IDbConnection = Nothing
        Dim trb As IDbTransaction = Nothing

        If (Not pTLPadre Is Nothing) Then
            'Buscamos si en la transaccion logica padre exite una transaccion para ese recurso. En caso de existir la devolvemos
            tr = pTLPadre.RecuperarTransacRecurso(Me.Recurso)
            If (Not tr Is Nothing) Then
                Return tr

                'De no existir se crea una, se asocia a la transaccion logica y se devuelve
            Else
                Try
                    trbd = New TransaccionRecursoBDAD
                    trbd.TransaccionLogica = pTLPadre
                    trbd.Gestor = Me
                    trbd.Recurso = Me.Recurso

                    'Obtenemos una conexion
                    fac = New FactoriaAD
                    cn = fac.GetConexion(Me.mRecurso)

                    'Comenzamos una transaccion y la asociamos
                    cn.Open()
                    trb = cn.BeginTransaction

                    dtr = New DatosTransaccionGenericoBDAD
                    dtr.Transaccion = trb

                    dtr.Conexion = cn
                    trbd.DatosTransaccion = dtr

                    'Apuntamos la nueva transaccion
                    pTLPadre.TransacionesRecurso.Add(trbd.ID, trbd)
                    mHTTransacciones.Add(trbd.ID, trbd)

                    Dim conexion As Object = cn
                    'Debug.WriteLine("Conexión abierta:" & dtr.GetHashCode & " conexion: " & conexion.GetHashCode)


                    Return trbd

                Catch ex As Exception
                    If (trb IsNot Nothing) Then
                        trb.Rollback()
                    End If

                    If (cn IsNot Nothing) AndAlso cn.State = ConnectionState.Open Then
                        cn.Close()
                        Dim conexion As Object = cn
                        'Debug.WriteLine("Conexión Cerrando:" & conexion.GetHashCode)

                    End If

                    Throw (ex)
                End Try
            End If
        End If

        Return Nothing
    End Function

    ''' <summary>Metodo que obtiene la conexion de una transaccion logica.</summary>
    ''' <remarks>Se devuelve o bien una conexion verdadera o un proxy.</remarks>
    ''' <param name="pTL" type="ITransaccionLogica">
    ''' Transaccion logica de la que queremos obtener su conexion.
    ''' </param>
    ''' <returns>Conexion sobre la que se ejecuta la transaccion logica.</returns>
    Public Function GetConexion(ByVal pTL As ITransaccionLogicaLN) As IDbConnection
        'aqui se devuelve bien una conexion verdadera o un proxi
        '  en este punto se debira llamar a la factoria generica para que de un objeto u otro en funcion del recurso
        Dim tr As ITransaccionRecursoLN
        Dim cnProxy As ConexionProxyAD
        Dim fac As IFactoriaAD

        'Si pTL es nothing se devulve una conexion normal porque no se esta en dentro de una transaccion
        If (pTL Is Nothing) Then
            fac = New FactoriaAD

            Return fac.GetConexion(Me.mRecurso)

        Else
            'Si esta en una transaccion logica se llama al metodo que inica una transaccion y se devulve un proxy
            tr = Me.IniciarTransaccion(pTL)
            cnProxy = New ConexionProxyAD(tr)

            Return cnProxy
        End If
    End Function

    ''' <summary>Metodo que obtiene la transaccion de una transaccion logica.</summary>
    ''' <remarks>
    ''' No esta implementado.
    ''' Aqui se devuelve bien una transaccion verdadera o un proxy. En este punto se deberia llamar a la factoria generica
    ''' para que de un objeto u otro en funcion del recurso.
    ''' </remarks>
    ''' <param name="pTL" type="ITransaccionLogica">
    ''' Transaccion logica de la que queremos obtener su transaccion.
    ''' </param>
    ''' <returns>Transaccion que contiene a la transaccion logica.</returns>
    Public Function GetTransaccion(ByVal pTL As ITransaccionLogicaLN) As IDbTransaction
        Throw New NotImplementedException
    End Function
#End Region

End Class
