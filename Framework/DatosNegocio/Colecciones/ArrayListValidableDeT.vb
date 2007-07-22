Option Explicit On
Option Strict On

Imports System.Collections.Generic
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary


Public Interface IArrayListValidable(Of T)
    Inherits IValidable
    Inherits IColEventos
    Inherits IList(Of T)
    Inherits ICloneable
    'Inherits IList
    Inherits IColDn
    Function Contiene(ByVal pEntidadDN As IEntidadBaseDN, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean
    Function Contiene(ByVal pEntidadDN As IEntidadBaseDN, ByRef mismaref As Boolean) As Boolean
    '   Function UpCasting(Of K As {T inherits K})() As IArrayListValidable(Of K)
    Function ToListOFt() As List(Of T)
    Function ContieneAlguno(ByVal pCol As IList(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean
    Function ToHtGUIDs(ByVal phtGUIDEntidades As System.Collections.Hashtable) As System.Collections.Hashtable

End Interface

<Serializable()> _
Public Class ArrayListValidable(Of T As {IEntidadBaseDN, Class})
    'Implements IValidable
    'Implements IColEventos


    'Implements IList(Of T)
    'Implements ICloneable
    Implements IList
    'Implements IColDn

    Implements IArrayListValidable(Of T)



    ' Implements IDatoPersistenteDN

#Region "Atributos"
    'Validador del objeto
    Private mValidador As IValidador

    Private mLista As List(Of T)

    'Indica si los eventos de modificacion estan activos (por defecto lo estan)
    Private mEventosActivos As Boolean = True

    ' la idea es que este campo valga para saber si algun elemtno esta en estado alterado en la coleccion es decir para insertar o modificado
    Private mEstadoDatosDN As EstadoDatosDN

#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto.</summary>
    ''' <param name="pValidador" type="IValidador">
    ''' El validador que vamos a usar para validar el arraylist.
    ''' </param>
    Public Sub New(ByVal pValidador As IValidador)
        mLista = New List(Of T)()
        mValidador = pValidador
    End Sub
    Public Sub New()
        mLista = New List(Of T)()
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
    '        Throw New NotImplementedException
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
    Public Function RecuperarLsitaGUID() As System.Collections.Generic.List(Of String) Implements IColDn.RecuperarLsitaGUID

        Dim ls As New Generic.List(Of String)

        For Each midn As IEntidadBaseDN In Me
            ls.Add(midn.GUID)
        Next




        Return ls





    End Function

    ''' <summary>
    ''' Método que comprueba si la colección contiene todas las entidades de otra colección. Devuelve true
    ''' si la colección está totalmente contenida en mi
    ''' </summary>
    ''' <param name="coleccion">Colección que debe estar contenida en mi</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ContieneColeccion(ByVal coleccion As IList(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean
        If coleccion IsNot Nothing AndAlso coleccion.Count > 0 Then
            For Each entidad As T In coleccion
                If Not Me.Contiene(entidad, pCoincidencia) Then
                    Return False
                End If
            Next
        End If

        Return True
    End Function
    Public Function Interseccion(ByVal col As IList(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ArrayListValidable(Of T)

        Dim colResultado As New ArrayListValidable(Of T)


        If col IsNot Nothing AndAlso col.Count > 0 Then
            For Each entidad As T In col
                If Me.Contiene(entidad, pCoincidencia) Then
                    colResultado.Add(entidad)
                End If
            Next
        End If

        Return colResultado
    End Function

    Public Function DiferenciaA(ByVal col As IList(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ArrayListValidable(Of T)

        Dim colResultado As New ArrayListValidable(Of T)
        colResultado.AddRangeObject(Me)

        If col IsNot Nothing AndAlso col.Count > 0 Then
            For Each entidad As T In col
                If colResultado.Contiene(entidad, pCoincidencia) Then
                    colResultado.EliminarEntidadDN(entidad, pCoincidencia)
                End If
            Next

        End If

        Return colResultado
    End Function

    Public Sub ToListIXMLAdaptador(ByVal mIXMLAdaptador As IXMLAdaptador, ByVal plista As IList)

        Dim pm As IEntidadBaseDN
        Dim miIXMLAdaptador As IXMLAdaptador




        'ToListIXMLAdaptador = New List(Of IXMLAdaptador)
        For Each pm In Me
            miIXMLAdaptador = CType(Activator.CreateInstance(CType(mIXMLAdaptador, Object).GetType), IXMLAdaptador)
            miIXMLAdaptador.ObjetoToXMLAdaptador(pm)
            ' ToListIXMLAdaptador.Add(miIXMLAdaptador)
            plista.Add(miIXMLAdaptador)
        Next



    End Sub
    Protected Overridable Function ToXML(ByVal pNombreTag As String) As String

        Dim pm As IEntidadBaseDN
        Dim stb As New System.Text.StringBuilder
        stb.AppendLine("<" & pNombreTag & ">")

        For Each pm In Me
            stb.AppendLine(pm.ToXML)
        Next


        stb.AppendLine("</" & pNombreTag & ">")

        Return stb.ToString
    End Function
    Public Overridable Function ToXML() As String
        Return ToXML(Me.GetType.Name)
    End Function
    Public Function RecuperarPrimeroXNombre(ByVal pNombre As String) As T

        Dim elemento As T

        For Each elemento In Me
            If elemento.Nombre = pNombre Then
                Return elemento
            End If
        Next
        Return Nothing

    End Function
    Public Function RecuperarXNombre(ByVal pNombre As String) As IList(Of T)

        Dim al As New List(Of T)

        For Each elemento As T In Me
            If elemento.Nombre = pNombre Then
                al.Add(elemento)
            End If
        Next
        Return al

    End Function
    Public Function Contiene(ByVal pEntidadDN As IEntidadBaseDN, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean Implements IArrayListValidable(Of T).Contiene

        Dim Ent As IEntidadBaseDN
        If pEntidadDN Is Nothing Then
            Throw New ApplicationException("no se admite nothing como valor de busqueda")
        End If

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
    Public Function ContieneAlguno(ByVal pCol As IList(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean Implements IArrayListValidable(Of T).ContieneAlguno




        If pCol Is Nothing Then
            Throw New ApplicationException("no se admite nothing como valor de busqueda")
        End If


        For Each miEntidadDN As IEntidadBaseDN In pCol
            If Contiene(miEntidadDN, pCoincidencia) Then
                Return True
            End If
        Next




        Return False

    End Function

    ''' <summary>
    ''' este metodo indica si se contiene o no a una entidad dn dentro de la coleccion y si es la misma referencia o un clon
    ''' 
    ''' </summary>
    ''' <param name="pEntidadDN"></param>
    ''' <param name="mismaref">devuelve true si todas las intancias encontradas eran la msima referencia y false si alguna era un clon</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Contiene(ByVal pEntidadDN As IEntidadBaseDN, ByRef mismaref As Boolean) As Boolean Implements IArrayListValidable(Of T).Contiene

        Dim Ent As IEntidadBaseDN
        mismaref = True
        Contiene = False

        For Each Ent In Me

            If pEntidadDN.GUID.ToLower = Ent.GUID.ToLower Then
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

        Dim Ent As IEntidadBaseDN
        For Each Ent In Me
            If Ent.ID.ToLower = pIdEntidadDN.ToLower Then
                Return True
            End If
        Next

        Return False

    End Function

    ''' <summary>
    ''' devuelve true solo si la misma instancia ya esta referida
    ''' </summary>
    ''' <param name="pEntidadDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Contains(ByVal pEntidadDN As T) As Boolean Implements IList(Of T).Contains
        'Dim Ent As IEntidadDN
        'For Each Ent In mLista
        '    If pEntidadDN Is Ent Then ' lo pongo primero para el caso de que fuera un nuevo ojeto , que como sabemos su ide es "" o nothing al ser reconocida como la misma isntacia se sepa que es el mismo
        '        Return True
        '    End If
        '    'If Not pEntidadDN.ID Is Nothing AndAlso pEntidadDN.ID <> "" AndAlso pEntidadDN.ID = Ent.ID Then
        '    '    Return True
        '    'End If
        'Next
        'Return False

        Return mLista.Contains(pEntidadDN)

    End Function

    Public Sub CopyTo(ByVal array() As T, ByVal arrayIndex As Integer) Implements IList(Of T).CopyTo
        mLista.CopyTo(array, arrayIndex)
    End Sub


    Public Function RecuperarxID(ByVal idEntidadDN As String) As T

        Dim Ent As T

        For Each Ent In mLista

            If Not Ent Is Nothing Then
                If Ent.ID.ToLower = idEntidadDN.ToLower Then
                    Return Ent
                End If
            End If

        Next
        Return Nothing
    End Function

    Public Function RecuperarXGUID(ByVal pGuid As String) As T

        Dim Ent As T

        For Each Ent In mLista

            If Not Ent Is Nothing Then
                If String.Equals(Ent.GUID, pGuid, StringComparison.OrdinalIgnoreCase) Then
                    Return Ent
                End If
            End If

        Next
        Return Nothing
    End Function

    Public Function EliminarEntidadDN(ByVal pColEntidadDN As Generic.IList(Of T), ByVal pConcidencia As CoincidenciaBusquedaEntidadDN) As List(Of T)

        Dim elemento As T
        Dim vistasaEliminar As List(Of T)

        vistasaEliminar = New List(Of T)

        For Each elemento In pColEntidadDN
            vistasaEliminar.AddRange(EliminarEntidadDN(elemento, pConcidencia))
        Next
        Return vistasaEliminar
    End Function



    ''' <summary>
    ''' elimina todas las intancias que refieren al msimo tipo de entidad, usa GUId
    ''' </summary>
    ''' <param name="pEntidadDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function EliminarEntidadDN(ByVal pEntidadDN As T, ByVal pConcidencia As CoincidenciaBusquedaEntidadDN) As List(Of T)

        ' elimina todas las visitas del mimo id
        Dim ent As T
        Dim vistasaEliminar As List(Of T)

        vistasaEliminar = New List(Of T)

        For Each ent In mLista
            If ent.GUID.ToLower = pEntidadDN.GUID.ToLower Then


                Select Case pConcidencia
                    Case CoincidenciaBusquedaEntidadDN.Todos
                        vistasaEliminar.Add(ent)
                    Case CoincidenciaBusquedaEntidadDN.MismaRef
                        If ent Is pEntidadDN Then
                            vistasaEliminar.Add(ent)
                        End If
                    Case CoincidenciaBusquedaEntidadDN.Clones
                        If Not ent Is pEntidadDN Then
                            vistasaEliminar.Add(ent)
                        End If
                End Select



            End If

        Next

        For Each ent In vistasaEliminar
            Me.Remove(ent)
        Next

        Return vistasaEliminar

    End Function
    Public Function EliminarEntidadDN(ByVal idEntidadDN As String) As List(Of T)

        ' elimina todas las visitas del mimo id
        Dim ent As T
        Dim vistasaEliminar As List(Of T)

        If idEntidadDN = "" Then
            Return Nothing
        End If


        vistasaEliminar = New List(Of T)

        For Each ent In mLista

            If ent.ID.ToLower = idEntidadDN.ToLower Then
                vistasaEliminar.Add(ent)
            End If

        Next

        For Each ent In vistasaEliminar
            Me.Remove(ent)
        Next

        Return vistasaEliminar

    End Function


    Public Function EliminarEntidadDNxGUID(ByVal pGuid As String) As List(Of T)

        ' elimina todas las visitas del mimo id
        Dim ent As T
        Dim vistasaEliminar As List(Of T)

        If pGuid = "" Then
            Return Nothing
        End If


        vistasaEliminar = New List(Of T)

        For Each ent In mLista

            If String.Equals(ent.GUID, pGuid, StringComparison.OrdinalIgnoreCase) Then
                vistasaEliminar.Add(ent)
            End If

        Next

        For Each ent In vistasaEliminar
            Me.Remove(ent)
        Next

        Return vistasaEliminar

    End Function

    ''' <summary>
    ''' añade una dn a una coleccion y elimina a quellas que representan a la misma entidad clones
    ''' devuelve la lsita de vistas que fueron eliminadas
    ''' </summary>
    ''' <param name="pEntidadDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AddUnico(ByVal pEntidadDN As T) As List(Of T)
        ' 

        Dim result As List(Of T)

        If Me.Contiene(pEntidadDN, CoincidenciaBusquedaEntidadDN.MismaRef) Then

            ' ya le continen
            result = EliminarEntidadDN(pEntidadDN, CoincidenciaBusquedaEntidadDN.Clones)
        Else
            ' no le contine 
            result = EliminarEntidadDN(pEntidadDN, CoincidenciaBusquedaEntidadDN.Clones)
            Me.Add(pEntidadDN)


        End If


        Return result

    End Function

    ''' <summary>Añade un elemento al ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir la validacion y los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pElemento" type="Object">
    ''' El objeto que añadimos al ArrayListValidable
    ''' </param>
    ''' 
    Public Sub Add(ByVal pElemento As T) Implements System.Collections.Generic.ICollection(Of T).Add
        ValidarElemento(pElemento)
        Dim permitido As Boolean
        permitido = True
        Me.OnElementoaAñadir(Me, pElemento, permitido)
        If Not permitido Then
            Exit Sub
        End If
        mLista.Add(pElemento)
        OnElementoAñadido(Me, pElemento)
    End Sub
    'Public Function add(ByVal objeto As Object) As Integer Implements IList.Add
    '    Me.Add(CType(objeto, T))
    '    Return 1
    'End Function




    Private Function ValidarElemento(ByVal pElemento As T) As Integer
        If Not mValidador Is Nothing Then
            If (Not ValTipoDeDatos(pElemento)) Then
                Throw New ApplicationException("Error: se ha intentado añadir un tipo incorrecto al arraylist.")
            End If
        End If
    End Function

    ''' <summary>Añade una coleccion al ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir la validacion y los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pColeccion" type="ICollection">
    ''' La coleccion que añadimos al ArrayListValidable
    ''' </param>
    Public Sub AddRange(ByVal pColeccion As ICollection(Of T))
        If Not pColeccion Is Nothing Then
            For Each elemento As T In pColeccion
                ValidarElemento(elemento)
                Me.Add(elemento)
                OnElementoAñadido(Me, elemento)
            Next
        End If

    End Sub
    Public Sub AddRangeObject(ByVal pColeccion As IEnumerable)
        If Not pColeccion Is Nothing Then
            For Each elemento As T In pColeccion
                ValidarElemento(elemento)
                Me.Add(elemento)
                OnElementoAñadido(Me, elemento)
            Next
        End If

    End Sub
    Public Sub AddRangeObjectUnico(ByVal pColeccion As IEnumerable)
        If Not pColeccion Is Nothing Then
            For Each elemento As IEntidadDN In pColeccion
                ValidarElemento(CType(elemento, T))
                Me.AddUnico1(elemento)
                OnElementoAñadido(Me, elemento)
            Next
        End If

    End Sub
    ''' <summary>Elimina un elemento del ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pElemento" type="Object">
    ''' El elemento que eliminamos del ArrayListValidable
    ''' </param>
    Public Function Remove(ByVal pElemento As T) As Boolean Implements System.Collections.Generic.ICollection(Of T).Remove
        Dim permitido As Boolean
        permitido = True
        Me.OnElementoaEliminar(Me, pElemento, permitido)
        If Not permitido Then
            Exit Function
        End If

        Remove = mLista.Remove(pElemento)
        If Remove Then
            OnElementoEliminado(Me, pElemento)
        End If



        'Dim il As List(Of T) = Me.EliminarEntidadDN(pElemento, CoincidenciaBusquedaEntidadDN.MismaRef)
        'Return il.Count > 0

    End Function

    'Public Function EliminarxCol(ByVal pLsita As ArrayListValidable(Of T)) As ArrayListValidable(Of T)

    '    Dim eliminados As New ArrayListValidable(Of T)

    '    For Each elemento As T In pLsita

    '        Me.EliminarEntidadDN()
    '    Next
    '    Return eliminados

    'End Function
    ''' <summary>Elimina el elemento en la posicion indicada del ArrayListValidable</summary>
    ''' <remarks>
    ''' Sobreescribe el metodo de ArrayList para incluir los eventos de modificacion.
    ''' </remarks>
    ''' <param name="pIndice" type="Integer">
    ''' El indice del elemento que vamos a eliminar del ArrayListValidable
    ''' </param>
    Public Sub RemoveAt(ByVal pIndice As Integer) Implements System.Collections.Generic.IList(Of T).RemoveAt, System.Collections.IList.RemoveAt
        'Guardamos el objeto para utilizarlo en el evento de eliminacion
        Dim objeto As T = mLista(pIndice)

        Me.Remove(objeto)

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
    Public Sub RemoveRange(ByVal pIndice As Integer, ByVal pNumero As Integer)
        Dim objetos(pNumero - 1) As T
        Dim objeto As T

        'Guardamos los objetos eliminados para utilizarlos en el evento de eliminacion
        mLista.CopyTo(pIndice, objetos, 0, pNumero)

        For Each objeto In objetos
            Me.Remove(objeto)
        Next

    End Sub

    ''' <summary>Obtiene una copia en profundidad del ArrayListValidable</summary>
    ''' <returns>La copia del ArrayListValidable</returns>
    Public Function Clone() As Object Implements ICloneable.Clone
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

    Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of T).Count, System.Collections.ICollection.Count
        Get
            Return mLista.Count
        End Get
    End Property

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

    '''' <summary>Obtiene informacion sobre el estado de consistencia del objeto.</summary>
    '''' <remarks>No esta implementado</remarks>
    '''' <param name="pMensaje" type="String">
    '''' Mensaje informativo sobre el estado del objeto.
    '''' </param>
    '''' <returns>El estado de consistencia del objeto</returns>
    'Public Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN Implements IDatoPersistenteDN.EstadoIntegridad
    '    Throw New NotImplementedException
    'End Function

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
    Private Function ValTipoDeDatos(ByVal pObjeto As T) As Boolean
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
        If (TypeOf pColeccion Is IValidable) Then
            If (CType(pColeccion, IValidable).Validador.Formula = Me.mValidador.Formula) Then
                Return True
            End If
        End If

        'Si por contra no se cumple la condicion validamos cada uno de los objetos
        enumerador = pColeccion.GetEnumerator

        Do While (enumerador.MoveNext)
            If Not ValTipoDeDatos(CType(enumerador.Current, T)) Then
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
    ''' 

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
#Region "conversiones"
    Public Function ToListOFt() As List(Of T) Implements IArrayListValidable(Of T).ToListOFt
        ToListOFt = New List(Of T)
        ToListOFt.AddRange(Me.ToArray)


    End Function
#End Region


    Public Overridable Sub sort() Implements IArrayListValidable(Of T).Sort

        mLista.Sort()
    End Sub
    Public Sub sort(ByVal comparer As IComparer(Of T))
        mLista.Sort(comparer)
    End Sub
    Public Sub sort(ByVal index As Integer, ByVal count As Integer, ByVal comparer As IComparer(Of T))
        mLista.Sort(index, count, comparer)
    End Sub
    Public Sub sort(ByVal Comparison As Comparison(Of T))
        mLista.Sort(Comparison)
    End Sub
    Public Sub Clear() Implements System.Collections.Generic.ICollection(Of T).Clear, System.Collections.IList.Clear
        Do While Me.Count > 0
            RemoveAt(Me.Count - 1)
        Loop
    End Sub



    Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of T).IsReadOnly, System.Collections.IList.IsReadOnly
        Get
            Return False
        End Get
    End Property

    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
        Return mLista.GetEnumerator()
    End Function

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return mLista.GetEnumerator()
    End Function

    Public Function IndexOf(ByVal item As T) As Integer Implements System.Collections.Generic.IList(Of T).IndexOf
        Return mLista.IndexOf(item)
    End Function


    ''' <summary>
    ''' SQL no mantiene el orden por lo tanto INSERT = ADD
    ''' </summary>
    ''' <param name="index"></param>
    ''' <remarks></remarks>
    Public Sub Insert(ByVal index As Integer, ByVal pElemento As T) Implements System.Collections.Generic.IList(Of T).Insert
        'ValidarElemento(pElemento)
        'mLista.Insert(index, pElemento)
        'OnElementoAñadido(Me, pElemento)



        ValidarElemento(pElemento)
        Dim permitido As Boolean
        permitido = True
        Me.OnElementoaAñadir(Me, pElemento, permitido)
        If Not permitido Then
            Exit Sub
        End If
        mLista.Insert(index, pElemento)
        OnElementoAñadido(Me, pElemento)


    End Sub

    Default Public Property Item(ByVal index As Integer) As T Implements System.Collections.Generic.IList(Of T).Item
        Get
            Return mLista.Item(index)
        End Get
        Set(ByVal value As T)
            Me.RemoveAt(index)
            Me.Insert(index, value)
        End Set
    End Property



    Public Sub CopyTo1(ByVal array As System.Array, ByVal index As Integer) Implements System.Collections.ICollection.CopyTo
        mLista.CopyTo(CType(array, T()), index)
    End Sub



    Public ReadOnly Property IsSynchronized() As Boolean Implements System.Collections.ICollection.IsSynchronized
        Get
            Return IsSynchronized
        End Get
    End Property

    Public ReadOnly Property SyncRoot() As Object Implements System.Collections.ICollection.SyncRoot
        Get
            Return Nothing '' ¿?¿?
        End Get
    End Property

    Private Function Add1(ByVal value As Object) As Integer Implements System.Collections.IList.Add
        Me.Add(CType(value, T))

    End Function

    Private Function Contains1(ByVal value As Object) As Boolean Implements System.Collections.IList.Contains
        If TypeOf value Is T Then
            IndexOf(CType(value, T))
        Else
            Return False
        End If
    End Function

    Private Function IndexOf1(ByVal value As Object) As Integer Implements System.Collections.IList.IndexOf
        If TypeOf value Is T Then
            IndexOf(CType(value, T))
        Else
            Return -1
        End If
    End Function

    Private Sub Insert1(ByVal index As Integer, ByVal value As Object) Implements System.Collections.IList.Insert
        If TypeOf value Is T Then
            Insert(index, CType(value, T))
        Else
            Throw New Exception("Error: No puedes meter una variable de este tipo en este ArrayList validable")
        End If
    End Sub

    Private ReadOnly Property IsFixedSize() As Boolean Implements System.Collections.IList.IsFixedSize
        Get
            Return False
        End Get
    End Property

    Private Overloads Property Item1(ByVal index As Integer) As Object Implements System.Collections.IList.Item
        Get
            Return Item(index)
        End Get
        Set(ByVal value As Object)
            If TypeOf value Is T Then
                Item(index) = CType(value, T)
            Else
                Throw New Exception("Error: No puedes meter una variable de este tipo en este ArrayList validable")
            End If
        End Set
    End Property

    Private Sub Remove1(ByVal value As Object) Implements System.Collections.IList.Remove
        If TypeOf value Is T Then
            Remove(CType(value, T))
        Else
            Throw New Exception("Error: No puedes meter una variable de este tipo en este ArrayList validable")
        End If

    End Sub

    Public Function ToArray() As T()
        Return Me.mLista.ToArray()
    End Function

#Region "Cosas de List que no tiene IList y que están de la caña"
    Public Function ConvertAll(Of TOutput)(ByVal converter As System.Converter(Of T, TOutput)) As List(Of TOutput)
        Return mLista.ConvertAll(converter)
    End Function

    Public Function Exist(ByVal match As Predicate(Of T)) As Boolean
        Return mLista.Exists(match)
    End Function

    Public Function Find(ByVal match As Predicate(Of T)) As T
        Return mLista.Find(match)
    End Function

    Public Function FindAll(ByVal match As Predicate(Of T)) As List(Of T)
        Return mLista.FindAll(match)
    End Function

    Public Function FindIndex(ByVal match As Predicate(Of T)) As Integer
        Return mLista.FindIndex(match)
    End Function

    Public Function FindLast(ByVal match As Predicate(Of T)) As T
        Return mLista.FindLast(match)
    End Function

    Public Function FindLastIndex(ByVal match As Predicate(Of T)) As Integer
        Return mLista.FindLastIndex(match)
    End Function

    Public Sub ForEach(ByVal action As Action(Of T))
        mLista.ForEach(action)
    End Sub

    Public Function GetRange(ByVal index As Integer, ByVal count As Integer) As List(Of T)
        Return mLista.GetRange(index, count)
    End Function

    Public Sub Reverse()
        mLista.Reverse()
    End Sub

    Public Sub Reverse(ByVal index As Integer, ByVal count As Integer)
        mLista.Reverse(index, count)
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
        Dim objeto As T
        objeto = CType(Me.RecuperarItemxHuellaTextual(huellaTextual), T)
        If Not objeto Is Nothing Then
            Me.Remove(objeto)

        End If
        Return objeto
    End Function

    Private Function AddUnico1(ByVal pEntidadDN As IEntidadDN) As System.Collections.ArrayList Implements IColDn.AddUnico
        Dim al As New ArrayList
        al.AddRange(Me.AddUnico(CType(pEntidadDN, T)))
        Return al
    End Function

    Private Sub AddRange1(ByVal pColeccion As System.Collections.ICollection) Implements IColDn.AddRange
        Me.AddRange1(pColeccion)
    End Sub



    Public ReadOnly Property ModificadosElemtosCol() As Boolean Implements IColEventos.ModificadosElemtosCol
        Get

            For Each elemtoIEntidadBaseDN As IEntidadBaseDN In Me.mLista
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

    Public Overrides Function ToString() As String

        Dim stb As New System.Text.StringBuilder


        For Each elemento As T In Me
            stb.Append(elemento.ToString)
            stb.Append(", ")
        Next


        Return stb.ToString
    End Function

    Public Function ToHtGUIDs(ByVal phtGUIDEntidades As System.Collections.Hashtable) As System.Collections.Hashtable Implements IArrayListValidable(Of T).ToHtGUIDs
        If phtGUIDEntidades Is Nothing Then
            phtGUIDEntidades = New System.Collections.Hashtable


        End If



        For Each mit As T In Me

            mit.ToHtGUIDs(phtGUIDEntidades)

        Next

        Return phtGUIDEntidades
    End Function
End Class


Public Enum CoincidenciaBusquedaEntidadDN
    Todos
    MismaRef
    Clones
End Enum


Public Class ArrayListValidableEntTemp(Of T As {IEntidadTemporalDN, Class})
    Inherits ArrayListValidable(Of T)

    Public Function RecuperarXPar(ByVal pPar As Framework.DatosNegocio.Localizaciones.Temporales.ParIntervalos) As ArrayListValidableEntTemp(Of T)
        Dim col As New ArrayListValidableEntTemp(Of T)

        For Each pb As T In Me

            If pb.Periodo Is pPar.Int1 OrElse pb.Periodo Is pPar.Int2 Then
                col.Add(pb)
            End If
        Next

        Return col
    End Function
    Public Function RecuperarColPeridosFechas() As List(Of Framework.DatosNegocio.IIntervaloTemporal)
        Dim col As New List(Of Framework.DatosNegocio.IIntervaloTemporal)

        For Each pb As T In Me

            col.Add(pb.Periodo)

        Next

        Return col
    End Function

    Public Function RecuperarContenidosEnIntervalo(ByVal IIntervaloTemporal As Framework.DatosNegocio.IIntervaloTemporal) As ArrayListValidableEntTemp(Of T)


        Dim col As New ArrayListValidableEntTemp(Of T)

        For Each elemento As IEntidadTemporalDN In Me
            If IIntervaloTemporal.SolapadoOContenido(elemento.Periodo) = IntSolapadosOContenido.Contenedor Then
                col.Add(CType(elemento, T))
            End If

        Next

        Return col

    End Function
    Public Function RecuperarContienenFecha(ByVal pFecha As Date) As ArrayListValidableEntTemp(Of T)


        Dim col As New ArrayListValidableEntTemp(Of T)

        For Each elemento As IEntidadTemporalDN In Me

            If elemento.Periodo.Contiene(pFecha) Then
                col.Add(CType(elemento, T))
            End If
        Next

        Return col

    End Function
End Class
