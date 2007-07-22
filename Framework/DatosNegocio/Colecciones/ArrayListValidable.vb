#Region "Importaciones"

Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary

#End Region

''' <summary>Esta clase representa un ArrayList tipado.</summary>
''' <remarks>El control de los tipos se realiza en tiempo de ejecucion.</remarks>

<Serializable()> _
Public Class ArrayListValidable
    Inherits ArrayList
    Implements IValidable
    Implements IColEventos

    Implements IColDn









    'Implements IDatoPersistenteDN

#Region "Atributos"
    'Validador del objeto
    Private mValidador As IValidador

    'Indica si los eventos de modificacion estan activos (por defecto lo estan)
    Private mEventosActivos As Boolean = True
#End Region

#Region "Constructores"

    'TODO: Creado para poder guardar en BD, hay que repasarlo
    Public Sub New()
        MyBase.New()
    End Sub

    ''' <summary>Constructor por defecto.</summary>
    ''' <param name="pValidador" type="IValidador">
    ''' El validador que vamos a usar para validar el arraylist.
    ''' </param>
    Public Sub New(ByVal pValidador As IValidador)
        MyBase.New()
        mValidador = pValidador
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene el validador que valida el ArrayListValidable.</summary>
    Public ReadOnly Property Validador() As IValidador Implements IValidable.Validador
        Get
            Return mValidador
        End Get
    End Property

    ''' <summary>Indica si los eventos de modificacion se emiten o no.</summary>
    Public Property EventosActivos() As Boolean Implements IColEventos.EventosActivos
        Get
            Return mEventosActivos
        End Get
        Set(ByVal Value As Boolean)
            mEventosActivos = Value
        End Set
    End Property

    '''' <summary>Obtiene o modifica si el arraylist esta dado de baja o no.</summary>
    '''' <remarks>No esta implementado</remarks>
    'Public Property Baja() As Boolean Implements IDatoPersistenteDN.Baja
    '    Get
    '        Return False
    '    End Get
    '    Set(ByVal Value As Boolean)
    '        Throw New NotImplementedException
    '    End Set
    'End Property

    '''' <summary>Obtiene o modifica el estado de modificacion del arraylist.</summary>
    '''' <remarks>No esta implementado</remarks>
    'Public Property EstadoDatos() As EstadoDatosDN Implements IDatoPersistenteDN.EstadoDatos
    '    Get
    '        Throw New NotImplementedException
    '    End Get
    '    Set(ByVal Value As EstadoDatosDN)
    '        Throw New NotImplementedException
    '    End Set
    'End Property

    '''' <summary>Obtiene o modifica la fecha de modificacion del arraylist.</summary>
    '''' <remarks>No esta implementado</remarks>
    'Public Property FechaModificacion() As Date Implements IDatoPersistenteDN.FechaModificacion
    '    Get
    '        Throw New NotImplementedException
    '    End Get
    '    Set(ByVal Value As Date)
    '        Throw New NotImplementedException
    '    End Set
    'End Property
#End Region

#Region "Metodos"
    Public Function Contiene(ByVal pEntidadDN As IEntidadBaseDN, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean

        Dim Ent As IEntidadBaseDN

        For Each Ent In Me

            If pEntidadDN.GUID.ToLower = Ent.GUID.ToLower Then
                Contiene = True

                Select Case pCoincidencia
                    Case CoincidenciaBusquedaEntidadDN.Clones
                        If Not pEntidadDN Is Ent Then
                            Return True
                        End If
                    Case CoincidenciaBusquedaEntidadDN.MismaRef
                        If pEntidadDN Is Ent Then
                            Return True
                        End If
                    Case CoincidenciaBusquedaEntidadDN.Todos
                        Return True
                End Select




            End If

        Next


        Return False

    End Function
    Public Function Contiene(ByVal pEntidadDN As IEntidadBaseDN, ByRef mismaref As Boolean) As Boolean

        'Dim Ent As IEntidadDN
        'For Each Ent In Me

        '    If pEntidadDN Is Ent Then ' lo pongo primero para el caso de que fuera un nuevo ojeto , que como sabemos su ide es "" o nothing al ser reconocida como la misma isntacia se sepa que es el mismo
        '        mismaref = True
        '        Return True
        '    End If

        '    If Not pEntidadDN.ID Is Nothing AndAlso pEntidadDN.ID <> "" AndAlso pEntidadDN.ID = Ent.ID Then

        '        If pEntidadDN Is Ent Then
        '            mismaref = True
        '        Else
        '            mismaref = False
        '        End If
        '        Return True

        '    End If

        'Next

        'mismaref = False
        'Return False


        Dim Ent As IEntidadDN
        mismaref = True
        Contiene = False

        For Each Ent In Me

            If Not pEntidadDN.ID Is Nothing AndAlso pEntidadDN.ID <> "" AndAlso pEntidadDN.ID = Ent.ID Then
                Contiene = True
                If pEntidadDN Is Ent Then
                    'mismaref = True
                    ' debemos de continuar por si encontrasemos un clon
                Else
                    ' ya sabemos que almenos hay una isntacia repetida que ademas es un clon luego podemos avandonar el bucle
                    mismaref = False
                    Return True
                End If


            End If

        Next


        Return Contiene
    End Function




    Public Function Contiene(ByVal pIdEntidadDN As String) As Boolean

        Dim Ent As IEntidadDN
        For Each Ent In Me

            If Ent.ID.ToLower = pIdEntidadDN.ToLower Then
                Return True
            End If

        Next

        Return False

    End Function
    Public Function RecuperarxID(ByVal idEntidadDN As String) As IEntidadDN

        Dim Ent As IEntidadDN

        For Each Ent In Me
            If Ent.ID.ToLower = idEntidadDN.ToLower Then
                Return Ent
            End If
        Next

        Return Nothing

    End Function

    Private Function ReferenciasAEntidadOcopiasDeEntidadDN(ByVal pEntidadDN As IEntidadDN) As ArrayList
        Dim ent As IEntidadDN
        Dim ReferenciasAEntidadOcopiasDeEntidad As ArrayList



        ReferenciasAEntidadOcopiasDeEntidad = New ArrayList

        For Each ent In Me

            If ent.ID.ToLower = pEntidadDN.ID.ToLower Then
                If ent.ID Is Nothing OrElse ent.ID = "" Then
                    ' en este caso se trata de intantacias a dar de alta es decir do intancias distiantas
                    ' y solo deben ser eliminadas si se trata de la misma intancia en referencia de memoria
                    If ent Is pEntidadDN Then
                        ReferenciasAEntidadOcopiasDeEntidad.Add(ent)
                    End If
                Else
                    ReferenciasAEntidadOcopiasDeEntidad.Add(ent)
                End If
            End If

        Next

        Return ReferenciasAEntidadOcopiasDeEntidad
    End Function


    Public Function EliminarEntidadDN(ByVal pEntidadDN As IEntidadDN, ByVal entidadesAEliminar As ArrayList) As ArrayList

        ' elimina todas las visitas del mimo id
        Dim ent As IEntidadDN
        Dim vistasaEliminar As ArrayList

        If entidadesAEliminar Is Nothing Then
            vistasaEliminar = ReferenciasAEntidadOcopiasDeEntidadDN(pEntidadDN)
        Else
            vistasaEliminar = entidadesAEliminar
        End If




        For Each ent In vistasaEliminar
            Me.Remove(ent)
        Next

        Return vistasaEliminar

    End Function
    Public Function EliminarEntidadDN(ByVal idEntidadDN As String) As ArrayList

        ' elimina todas las visitas del mimo id
        Dim ent As IEntidadDN
        Dim vistasaEliminar As ArrayList

        If idEntidadDN = "" Then
            Return Nothing
        End If


        vistasaEliminar = New ArrayList

        For Each ent In Me

            If ent.ID.ToLower = idEntidadDN.ToLower Then
                vistasaEliminar.Add(ent)
            End If

        Next

        For Each ent In vistasaEliminar
            Me.Remove(ent)
        Next

        Return vistasaEliminar

    End Function

    Public Function AddUnico(ByVal pEntidadDN As IEntidadDN) As ArrayList
        ' devuelve la lsita de vistas que fueron eliminadas

        Dim alReferenciasAEntidadOcopiasDeEntidadDN As ArrayList
        alReferenciasAEntidadOcopiasDeEntidadDN = ReferenciasAEntidadOcopiasDeEntidadDN(pEntidadDN)

        If alReferenciasAEntidadOcopiasDeEntidadDN.Count > 1 Then
            AddUnico = EliminarEntidadDN(pEntidadDN, alReferenciasAEntidadOcopiasDeEntidadDN)
            Me.Add(pEntidadDN)
        ElseIf alReferenciasAEntidadOcopiasDeEntidadDN.Count = 1 AndAlso Not Me.Contains(pEntidadDN) Then
            AddUnico = EliminarEntidadDN(pEntidadDN, alReferenciasAEntidadOcopiasDeEntidadDN)
            Me.Add(pEntidadDN)
        ElseIf alReferenciasAEntidadOcopiasDeEntidadDN.Count = 0 Then
            Me.Add(pEntidadDN)
        End If

        Return alReferenciasAEntidadOcopiasDeEntidadDN
    End Function

    ''' <summary>Añade un elemento al ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir la validacion y los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pElemento" type="Object">
    ''' El objeto que añadimos al ArrayListValidable
    ''' </param>
    ''' <returns>El indice en el que se inserto el elemento</returns>
    Public Overrides Function Add(ByVal pElemento As Object) As Integer
        If (Not ValTipoDeDatos(pElemento)) Then
            Throw New ApplicationException("Error: se ha intentado añadir un tipo incorrecto al arraylist.")
        End If
        Dim permitido As Boolean
        permitido = True
        Me.OnElementoaAñadir(Me, pElemento, permitido)
        If Not permitido Then
            Exit Function
        End If
        MyBase.Add(pElemento)
        OnElementoAñadido(Me, pElemento)
    End Function

    ''' <summary>Añade una coleccion al ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir la validacion y los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pColeccion" type="ICollection">
    ''' La coleccion que añadimos al ArrayListValidable
    ''' </param>
    Public Overrides Sub AddRange(ByVal pColeccion As ICollection)
        Dim eColeccion As Object

        If Not ValidarRango(pColeccion) Then
            Throw New ApplicationException("Error: se ha intentado añadir una coleccion con algun tipo incorrecto al arraylist.")
        End If

        For Each eColeccion In pColeccion
            If TypeOf eColeccion Is ICollection Then
                MyBase.AddRange(eColeccion)
            Else
                MyBase.Add(eColeccion)
            End If
            OnElementoAñadido(Me, eColeccion)
        Next

    End Sub

    ''' <summary>Elimina un elemento del ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pElemento" type="Object">
    ''' El elemento que eliminamos del ArrayListValidable
    ''' </param>
    Public Overrides Sub Remove(ByVal pElemento As Object)

        Dim permitido As Boolean
        permitido = True
        Me.OnElementoaEliminar(Me, pElemento, permitido)
        If Not permitido Then
            Exit Sub
        End If

        MyBase.Remove(pElemento)
        OnElementoEliminado(Me, pElemento)
    End Sub

    ''' <summary>Elimina el elemento en la posicion indicada del ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pIndice" type="Integer">
    ''' El indice del elemento que vamos a eliminar del ArrayListValidable
    ''' </param>
    Public Overrides Sub RemoveAt(ByVal pIndice As Integer)
        Dim objeto As Object

        'Guardamos el objeto para utilizarlo en el evento de eliminacion
        objeto = Me.Item(pIndice)

        MyBase.RemoveAt(pIndice)
        OnElementoEliminado(Me, objeto)
    End Sub

    ''' <summary>Elimina un rango de elementos del ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pIndice" type="Integer">
    ''' El indice del primer elemento que vamos a eliminar del ArrayListValidable
    ''' </param>
    ''' <param name="pNumero" type="Integer">
    ''' El numero de elementos que vamos a eliminar
    ''' </param>
    Public Overrides Sub RemoveRange(ByVal pIndice As Integer, ByVal pNumero As Integer)
        Dim objetos(pNumero - 1) As Object

        'Guardamos los objetos eliminados para utilizarlos en el evento de eliminacion
        Me.CopyTo(pIndice, objetos, 0, pNumero)

        MyBase.RemoveRange(pIndice, pNumero)
        RaiseEvent ElementoEliminado(Me, pNumero)
    End Sub

    ''' <summary>Obtiene una copia en profundidad del ArrayListValidable</summary>
    ''' <returns>La copia del ArrayListValidable</returns>
    Public Overrides Function Clone() As Object
        Dim formateador As BinaryFormatter
        Dim memoria As MemoryStream

        formateador = New BinaryFormatter
        memoria = New MemoryStream

        'Nos serializamos y volvemos a poner el puntero de lectura/escritura al principio
        formateador.Serialize(memoria, Me)
        memoria.Seek(0, IO.SeekOrigin.Begin)

        'Nos desserializamos para conseguir la copia
        Return formateador.Deserialize(memoria)
    End Function

    ''' <summary>
    ''' Indica si la validacion que se realiza sobre el ArrayListValidable es la misma que la del validador
    ''' pasado por parametro.
    ''' </summary>
    ''' <param name="pValidador" type="IValidador">
    ''' El validador contra el que vamos a comparar.
    ''' </param>
    ''' <returns>Si la validacion que realizan los dos validadores es la misma o no.</returns>
    Public Function ValidacionIdentica(ByVal pValidador As IValidador) As Boolean Implements IValidable.ValidacionIdentica
        If (Me.mValidador.Formula = pValidador.Formula) Then
            Return True

        Else
            Return False
        End If
    End Function

    ''''' <summary>Obtiene informacion sobre el estado de consistencia del objeto.</summary>
    ''''' <remarks>No esta implementado</remarks>
    ''''' <param name="pMensaje" type="String">
    ''''' Mensaje informativo sobre el estado del objeto.
    ''''' </param>
    ''''' <returns>El estado de consistencia del objeto</returns>
    ''Public Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN Implements IDatoPersistenteDN.EstadoIntegridad
    ''    Throw New NotImplementedException
    ''End Function

    '''' <summary>Metodo que registra a todas las partes de una entidad.</summary>
    '''' <remarks>No esta implementado</remarks>
    'Public Sub RegistrarParteTodas() Implements IDatoPersistenteDN.RegistrarParteTodas
    '    Throw New NotImplementedException
    'End Sub
#End Region

#Region "Metodos Auxiliares"
    ''' <summary>Indica si un objeto es valido para este ArrayListValidable o no.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' El objeto que queremos validar.
    ''' </param>
    ''' <returns>Si el objeto es valido o no.</returns>
    Private Function ValTipoDeDatos(ByVal pObjeto As Object) As Boolean
        Dim mensaje As String = String.Empty
        If (Not mValidador Is Nothing) Then
            Return Me.mValidador.Validacion(mensaje, pObjeto)
        End If

        Return True
    End Function

    ''' <summary>Indica si una coleccion es valida para este ArrayListValidable o no.</summary>
    ''' <param name="pColeccion" type="ICollection">
    ''' La coleccion que queremos validar.
    ''' </param>
    ''' <returns>Si la coleccion es valida o no.</returns>
    Private Function ValidarRango(ByVal pColeccion As ICollection) As Boolean
        Dim enumerador As IEnumerator

        'Si es una coleccion validable y la formula de validacion es la misma que la de mi objeto de validacion
        'se asume que podemos  aceptar la coleccion

        If (TypeOf pColeccion Is IValidable) AndAlso Not CType(pColeccion, IValidable).Validador Is Nothing Then
            If (CType(pColeccion, IValidable).Validador.Formula = Me.mValidador.Formula) Then
                Return True
            End If
        End If

        'Si por contra no se cumple la condicion validamos cada uno de los objetos
        enumerador = pColeccion.GetEnumerator

        Do While (enumerador.MoveNext)
            If Not ValTipoDeDatos(enumerador.Current()) Then
                Return False
            End If
        Loop

        Return True
    End Function
#End Region

#Region "Eventos"
    ''' <summary>Evento que indica que se ha añadido un elemento al arraylist.</summary>
    Public Event ElementoAñadido(ByVal sender As Object, ByVal elemento As Object) Implements IColEventos.ElementoAñadido

    ''' <summary>Evento que indica que se ha eliminado un elemento del arraylist.</summary>
    Public Event ElementoEliminado(ByVal sender As Object, ByVal elemento As Object) Implements IColEventos.ElementoEliminado

    Public Event ElementoaAñadir(ByVal sender As Object, ByVal elemento As Object, ByRef permitir As Boolean) Implements IColEventos.ElementoaAñadir

    Public Event ElementoaEliminar(ByVal sender As Object, ByVal elemento As Object, ByRef permitir As Boolean) Implements IColEventos.ElementoaEliminar
#End Region

#Region "Manejadores Eventos"
    ''' <summary>Emite un evento de elemento añadido si los eventos estan activos.</summary>
    ''' <param name="pSender" type="Object">
    ''' Objeto que emite el evento.
    ''' </param>
    ''' <param name="pElemento" type="Object">
    ''' El elemento del evento.
    ''' </param>
    Protected Overridable Sub OnElementoAñadido(ByVal pSender As Object, ByVal pElemento As Object)
        If (Me.mEventosActivos = True) Then
            RaiseEvent ElementoAñadido(pSender, pElemento)
        End If
    End Sub

    ''' <summary>Emite un evento de elemento eliminado si los eventos estan activos.</summary>
    ''' <param name="pSender" type="Object">
    ''' Objeto que emite el evento.
    ''' </param>
    ''' <param name="pElemento" type="Object">
    ''' El elemento del evento.
    ''' </param>
    Protected Overridable Sub OnElementoEliminado(ByVal pSender As Object, ByVal pElemento As Object)
        If (Me.mEventosActivos = True) Then
            RaiseEvent ElementoEliminado(pSender, pElemento)
        End If
    End Sub

    Protected Overridable Sub OnElementoaAñadir(ByVal pSender As Object, ByVal pElemento As Object, ByRef permitir As Boolean)
        If (Me.mEventosActivos = True) Then
            RaiseEvent ElementoaAñadir(pSender, pElemento, permitir)
        End If
    End Sub
    Protected Overridable Sub OnElementoaEliminar(ByVal pSender As Object, ByVal pElemento As Object, ByRef permitir As Boolean)
        If (Me.mEventosActivos = True) Then
            RaiseEvent ElementoaEliminar(pSender, pElemento, permitir)
        End If
    End Sub
#End Region



    Public Function RecuperarItemxHuellaTextual(ByVal huellaTextual As String) As Object Implements IColDn.RecuperarItemxHuellaTextual
        Dim entidaddn As IEntidadBaseDN


        Dim tfulname, id, guid As String
        Dim o As Object

        Dim matrizSeparadores(0) As String
        matrizSeparadores(0) = "//"
        Dim matriztexto() As String

        matriztexto = huellaTextual.Split(matrizSeparadores, System.StringSplitOptions.None)
        If matriztexto.Length = 1 Then
            guid = matriztexto(0)
        Else
            If matriztexto.Length = 3 Then
                tfulname = matriztexto(0)
                id = matriztexto(1)
                guid = matriztexto(2)
            Else

                Throw New ArgumentException("Error en el formateado")
            End If

        End If

        For Each entidaddn In Me
            o = entidaddn

            If entidaddn.GUID = guid Then
                Return entidaddn
            End If

        Next

        Return Nothing

    End Function
    Public Function EliminarItemxHuellaTextual(ByVal huellaTextual As String) As Object Implements IColDn.EliminarItemxHuellaTextual
        Dim objeto As Object
        objeto = Me.RecuperarItemxHuellaTextual(huellaTextual)
        If Not objeto Is Nothing Then
            Me.Remove(objeto)

        End If
        Return objeto
    End Function

    Private Function AddUnico1(ByVal pEntidadDN As IEntidadDN) As System.Collections.ArrayList Implements IColDn.AddUnico
        Return Me.AddUnico(pEntidadDN)
    End Function

    Private Sub AddRange1(ByVal pColeccion As System.Collections.ICollection) Implements IColDn.AddRange
        Me.AddRange(pColeccion)
    End Sub

    Private Sub Sort1() Implements IColDn.Sort
        Me.Sort()
    End Sub

    Public ReadOnly Property ModificadosElemtosCol() As Boolean Implements IColEventos.ModificadosElemtosCol
        Get


            For Each elemtoIEntidadBaseDN As IEntidadBaseDN In Me
                If elemtoIEntidadBaseDN.Estado = EstadoDatosDN.SinModificar Then
                    If String.IsNullOrEmpty(elemtoIEntidadBaseDN.ID) OrElse elemtoIEntidadBaseDN.ID = "0" Then
                        Return True
                    End If
                Else
                    Return True
                End If
            Next

            Return False


        End Get
    End Property

    Public Function RecuperarLsitaGUID() As System.Collections.Generic.List(Of String) Implements IColDn.RecuperarLsitaGUID

        Dim ls As New Generic.List(Of String)

        For Each midn As IEntidadBaseDN In Me
            ls.Add(midn.GUID)
        Next




        Return ls





    End Function
End Class






'<Serializable()> _
'Public Class ArrayListValidable(Of T)
'    Inherits ArrayList
'    Implements IValidable
'    Implements IColEventos
'    ' Implements IDatoPersistenteDN

'#Region "Atributos"
'    'Validador del objeto
'    Private mValidador As IValidador

'    'Indica si los eventos de modificacion estan activos (por defecto lo estan)
'    Private mEventosActivos As Boolean = True
'#End Region

'#Region "Constructores"
'    ''' <summary>Constructor por defecto.</summary>
'    ''' <param name="pValidador" type="IValidador">
'    ''' El validador que vamos a usar para validar el arraylist.
'    ''' </param>
'    Public Sub New(ByVal pValidador As IValidador)
'        MyBase.New()
'        mValidador = pValidador
'    End Sub
'    Public Sub New()
'        MyBase.New()
'    End Sub
'#End Region

'#Region "Propiedades"
'    ''' <summary>Obtiene el validador que valida el ArrayListValidable.</summary>
'    Public ReadOnly Property Validador() As IValidador Implements IValidable.Validador
'        Get
'            Return mValidador
'        End Get
'    End Property

'    ''' <summary>Indica si los eventos de modificacion se emiten o no.</summary>
'    Public Property EventosActivos() As Boolean Implements IColEventos.EventosActivos
'        Get
'            Return mEventosActivos
'        End Get
'        Set(ByVal Value As Boolean)
'            mEventosActivos = Value
'        End Set
'    End Property

'    '''' <summary>Obtiene o modifica si el arraylist esta dado de baja o no.</summary>
'    '''' <remarks>No esta implementado</remarks>
'    'Public Property Baja() As Boolean Implements IDatoPersistenteDN.Baja
'    '    Get
'    '        Throw New NotImplementedException
'    '    End Get
'    '    Set(ByVal Value As Boolean)
'    '        Throw New NotImplementedException
'    '    End Set
'    'End Property

'    '''' <summary>Obtiene o modifica el estado de modificacion del arraylist.</summary>
'    '''' <remarks>No esta implementado</remarks>
'    'Public Property EstadoDatos() As EstadoDatosDN Implements IDatoPersistenteDN.EstadoDatos
'    '    Get
'    '        Throw New NotImplementedException
'    '    End Get
'    '    Set(ByVal Value As EstadoDatosDN)
'    '        Throw New NotImplementedException
'    '    End Set
'    'End Property

'    '''' <summary>Obtiene o modifica la fecha de modificacion del arraylist.</summary>
'    '''' <remarks>No esta implementado</remarks>
'    'Public Property FechaModificacion() As Date Implements IDatoPersistenteDN.FechaModificacion
'    '    Get
'    '        Throw New NotImplementedException
'    '    End Get
'    '    Set(ByVal Value As Date)
'    '        Throw New NotImplementedException
'    '    End Set
'    'End Property
'#End Region

'#Region "Metodos"
'    Public Function Contiene(ByVal pEntidadDN As IEntidadDN, ByRef mismaref As Boolean) As Boolean

'        Dim Ent As IEntidadDN
'        For Each Ent In Me

'            If pEntidadDN Is Ent Then ' lo pongo primero para el caso de que fuera un nuevo ojeto , que como sabemos su ide es "" o nothing al ser reconocida como la misma isntacia se sepa que es el mismo
'                mismaref = True
'                Return True
'            End If

'            If Not pEntidadDN.ID Is Nothing AndAlso pEntidadDN.ID <> "" AndAlso pEntidadDN.ID = Ent.ID Then

'                If pEntidadDN Is Ent Then
'                    mismaref = True
'                Else
'                    mismaref = False
'                End If
'                Return True

'            End If

'        Next

'        mismaref = False
'        Return False

'    End Function

'    Public Function Contiene(ByVal pIdEntidadDN As String) As Boolean

'        Dim Ent As IEntidadDN
'        For Each Ent In Me

'            If Ent.ID.ToLower = pIdEntidadDN.ToLower Then
'                Return True
'            End If

'        Next

'        Return False

'    End Function
'    Public Function RecuperarxID(ByVal idEntidadDN As String) As IEntidadDN

'        Dim Ent As IEntidadDN

'        For Each Ent In Me
'            If Ent.ID.ToLower = idEntidadDN.ToLower Then
'                Return Ent
'            End If
'        Next



'    End Function
'    Public Function EliminarEntidadDN(ByVal pEntidadDN As IEntidadDN) As ArrayList

'        ' elimina todas las visitas del mimo id
'        Dim ent As IEntidadDN
'        Dim vistasaEliminar As ArrayList



'        vistasaEliminar = New ArrayList

'        For Each ent In Me

'            If ent.ID.ToLower = pEntidadDN.ID.ToLower Then
'                If ent.ID Is Nothing OrElse ent.ID = "" Then
'                    ' en este caso se trata de intantacias a dar de alta es decir do intancias distiantas
'                    ' y solo deben ser eliminadas si se trata de la misma intancia en referencia de memoria
'                    If ent Is pEntidadDN Then
'                        vistasaEliminar.Add(ent)
'                    End If
'                Else
'                    vistasaEliminar.Add(ent)
'                End If
'            End If

'        Next

'        For Each ent In vistasaEliminar
'            Me.Remove(ent)
'        Next

'        Return vistasaEliminar

'    End Function
'    Public Function EliminarEntidadDN(ByVal idEntidadDN As String) As ArrayList

'        ' elimina todas las visitas del mimo id
'        Dim ent As IEntidadDN
'        Dim vistasaEliminar As ArrayList

'        If idEntidadDN = "" Then
'            Return Nothing
'        End If


'        vistasaEliminar = New ArrayList

'        For Each ent In Me

'            If ent.ID.ToLower = idEntidadDN.ToLower Then
'                vistasaEliminar.Add(ent)
'            End If

'        Next

'        For Each ent In vistasaEliminar
'            Me.Remove(ent)
'        Next

'        Return vistasaEliminar

'    End Function

'    Public Function AddUnico(ByVal pEntidadDN As IEntidadDN) As ArrayList
'        ' devuelve la lsita de vistas que fueron eliminadas

'        AddUnico = EliminarEntidadDN(pEntidadDN)
'        Me.Add(pEntidadDN)

'    End Function

'    ''' <summary>Añade un elemento al ArrayListValidable</summary>
'    ''' <remarks>
'    ''' Sobreescribe el metodo de ArrayList para incluir la validacion y los eventos de modificacion.
'    ''' </remarks>
'    ''' <param name="pElemento" type="Object">
'    ''' El objeto que añadimos al ArrayListValidable
'    ''' </param>
'    ''' <returns>El indice en el que se inserto el elemento</returns>
'    ''' 
'    Public Overrides Function Add(ByVal pElemento As Object) As Integer
'        Return AddPrivado(pElemento)
'    End Function
'    Public Overloads Function Add(ByVal pElemento As T) As Integer
'        Return AddPrivado(pElemento)
'    End Function

'    Private Function AddPrivado(ByVal pElemento As T) As Integer
'        If Not mValidador Is Nothing Then
'            If (Not ValTipoDeDatos(pElemento)) Then
'                Throw New ApplicationException("Error: se ha intentado añadir un tipo incorrecto al arraylist.")
'            End If
'        End If


'        MyBase.Add(pElemento)
'        OnElementoAñadido(Me, pElemento)
'    End Function

'    ''' <summary>Añade una coleccion al ArrayListValidable</summary>
'    ''' <remarks>
'    ''' Sobreescribe el metodo de ArrayList para incluir la validacion y los eventos de modificacion.
'    ''' </remarks>
'    ''' <param name="pColeccion" type="ICollection">
'    ''' La coleccion que añadimos al ArrayListValidable
'    ''' </param>
'    Public Overrides Sub AddRange(ByVal pColeccion As ICollection)
'        'If Not mValidador Is Nothing Then
'        '    If Not ValidarRango(pColeccion) Then
'        '        Throw New ApplicationException("Error: se ha intentado añadir una coleccion con algun tipo incorrecto al arraylist.")
'        '    End If
'        'End If


'        'MyBase.AddRange(pColeccion)
'        'OnElementoAñadido(Me, pColeccion)


'        Dim elemento As Object
'        For Each elemento In pColeccion
'            Me.AddPrivado(elemento)
'        Next

'    End Sub

'    ''' <summary>Elimina un elemento del ArrayListValidable</summary>
'    ''' <remarks>
'    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
'    ''' </remarks>
'    ''' <param name="pElemento" type="Object">
'    ''' El elemento que eliminamos del ArrayListValidable
'    ''' </param>
'    Public Overrides Sub Remove(ByVal pElemento As Object)
'        MyBase.Remove(pElemento)
'        OnElementoEliminado(Me, pElemento)
'    End Sub

'    ''' <summary>Elimina el elemento en la posicion indicada del ArrayListValidable</summary>
'    ''' <remarks>
'    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
'    ''' </remarks>
'    ''' <param name="pIndice" type="Integer">
'    ''' El indice del elemento que vamos a eliminar del ArrayListValidable
'    ''' </param>
'    Public Overrides Sub RemoveAt(ByVal pIndice As Integer)
'        Dim objeto As Object

'        'Guardamos el objeto para utilizarlo en el evento de eliminacion
'        objeto = Me.Item(pIndice)

'        MyBase.RemoveAt(pIndice)
'        OnElementoEliminado(Me, objeto)
'    End Sub

'    ''' <summary>Elimina un rango de elementos del ArrayListValidable</summary>
'    ''' <remarks>
'    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
'    ''' </remarks>
'    ''' <param name="pIndice" type="Integer">
'    ''' El indice del primer elemento que vamos a eliminar del ArrayListValidable
'    ''' </param>
'    ''' <param name="pNumero" type="Integer">
'    ''' El numero de elementos que vamos a eliminar
'    ''' </param>
'    Public Overrides Sub RemoveRange(ByVal pIndice As Integer, ByVal pNumero As Integer)
'        Dim objetos(pNumero - 1) As Object

'        'Guardamos los objetos eliminados para utilizarlos en el evento de eliminacion
'        Me.CopyTo(pIndice, objetos, 0, pNumero)

'        MyBase.RemoveRange(pIndice, pNumero)
'        RaiseEvent ElementoEliminado(Me, pNumero)
'    End Sub

'    ''' <summary>Obtiene una copia en profundidad del ArrayListValidable</summary>
'    ''' <returns>La copia del ArrayListValidable</returns>
'    Public Overrides Function Clone() As Object
'        Dim formateador As BinaryFormatter
'        Dim memoria As MemoryStream

'        formateador = New BinaryFormatter
'        memoria = New MemoryStream

'        'Nos serializamos y volvemos a poner el puntero de lectura/escritura al principio
'        formateador.Serialize(memoria, Me)
'        memoria.Seek(0, IO.SeekOrigin.Begin)

'        'Nos desserializamos para conseguir la copia
'        Return formateador.Deserialize(memoria)
'    End Function

'    ''' <summary>
'    ''' Indica si la validacion que se realiza sobre el ArrayListValidable es la misma que la del validador
'    ''' pasado por parametro.
'    ''' </summary>
'    ''' <param name="pValidador" type="IValidador">
'    ''' El validador contra el que vamos a comparar.
'    ''' </param>
'    ''' <returns>Si la validacion que realizan los dos validadores es la misma o no.</returns>
'    Public Function ValidacionIdentica(ByVal pValidador As IValidador) As Boolean Implements IValidable.ValidacionIdentica
'        If (Me.mValidador.Formula = pValidador.Formula) Then
'            Return True

'        Else
'            Return False
'        End If
'    End Function

'    '''' <summary>Obtiene informacion sobre el estado de consistencia del objeto.</summary>
'    '''' <remarks>No esta implementado</remarks>
'    '''' <param name="pMensaje" type="String">
'    '''' Mensaje informativo sobre el estado del objeto.
'    '''' </param>
'    '''' <returns>El estado de consistencia del objeto</returns>
'    'Public Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN Implements IDatoPersistenteDN.EstadoIntegridad
'    '    Throw New NotImplementedException
'    'End Function

'    '''' <summary>Metodo que registra a todas las partes de una entidad.</summary>
'    '''' <remarks>No esta implementado</remarks>
'    'Public Sub RegistrarParteTodas() Implements IDatoPersistenteDN.RegistrarParteTodas
'    '    Throw New NotImplementedException
'    'End Sub
'#End Region

'#Region "Metodos Auxiliares"
'    ''' <summary>Indica si un objeto es valido para este ArrayListValidable o no.</summary>
'    ''' <param name="pObjeto" type="Object">
'    ''' El objeto que queremos validar.
'    ''' </param>
'    ''' <returns>Si el objeto es valido o no.</returns>
'    Private Function ValTipoDeDatos(ByVal pObjeto As Object) As Boolean
'        If (Not mValidador Is Nothing) Then
'            Return Me.mValidador.Validacion(pObjeto)
'        End If

'        Return True
'    End Function

'    ''' <summary>Indica si una coleccion es valida para este ArrayListValidable o no.</summary>
'    ''' <param name="pColeccion" type="ICollection">
'    ''' La coleccion que queremos validar.
'    ''' </param>
'    ''' <returns>Si la coleccion es valida o no.</returns>
'    Private Function ValidarRango(ByVal pColeccion As ICollection) As Boolean
'        Dim enumerador As IEnumerator

'        'Si es una coleccion validable y la formula de validacion es la misma que la de mi objeto de validacion
'        'se asume que podemos  aceptar la coleccion
'        If (TypeOf pColeccion Is IValidable) Then
'            If (CType(pColeccion, IValidable).Validador.Formula = Me.mValidador.Formula) Then
'                Return True
'            End If
'        End If

'        'Si por contra no se cumple la condicion validamos cada uno de los objetos
'        enumerador = pColeccion.GetEnumerator

'        Do While (enumerador.MoveNext)
'            If Not ValTipoDeDatos(enumerador.Current()) Then
'                Return False
'            End If
'        Loop

'        Return True
'    End Function
'#End Region

'#Region "Eventos"
'    ''' <summary>Evento que indica que se ha añadido un elemento al arraylist.</summary>
'    Public Event ElementoAñadido(ByVal sender As Object, ByVal elemento As Object) Implements IColEventos.ElementoAñadido

'    ''' <summary>Evento que indica que se ha eliminado un elemento del arraylist.</summary>
'    Public Event ElementoEliminado(ByVal sender As Object, ByVal elemento As Object) Implements IColEventos.ElementoEliminado
'#End Region

'#Region "Manejadores Eventos"
'    ''' <summary>Emite un evento de elemento añadido si los eventos estan activos.</summary>
'    ''' <param name="pSender" type="Object">
'    ''' Objeto que emite el evento.
'    ''' </param>
'    ''' <param name="pElemento" type="Object">
'    ''' El elemento del evento.
'    ''' </param>
'    Protected Overridable Sub OnElementoAñadido(ByVal pSender As Object, ByVal pElemento As Object)
'        If (Me.mEventosActivos = True) Then
'            RaiseEvent ElementoAñadido(pSender, pElemento)
'        End If
'    End Sub

'    ''' <summary>Emite un evento de elemento eliminado si los eventos estan activos.</summary>
'    ''' <param name="pSender" type="Object">
'    ''' Objeto que emite el evento.
'    ''' </param>
'    ''' <param name="pElemento" type="Object">
'    ''' El elemento del evento.
'    ''' </param>
'    Protected Overridable Sub OnElementoEliminado(ByVal pSender As Object, ByVal pElemento As Object)
'        If (Me.mEventosActivos = True) Then
'            RaiseEvent ElementoEliminado(pSender, pElemento)
'        End If
'    End Sub
'#End Region

'End Class


