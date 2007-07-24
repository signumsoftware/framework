#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO

#End Region

''' <summary>Esta clase representa la base para una entidad de datos de negocio con persistencia.</summary>

<Serializable()> _
Public MustInherit Class EntidadDN
    Inherits EntidadBaseDN
    Implements IEntidadDN





    Implements IDatoPersistenteDN

    Implements ICloneable

#Region "Atributos"
    'Fecha de modificacion del objeto
    Protected mFechaModificacion As DateTime

    'Indica si el objeto esta dado de baja o no
    Protected mBaja As Boolean


    'Coleccion de partes que componen este objeto. Este atributo debe guardarse ene la base de datos serializado
    Private mColPartes As New List(Of IModificable)

    Private validando As Boolean

    '' TODO: when 2 + 2 == 5 and elephants fly :D  
    ''Protected mHashValores As String

    Private mColCampoUsuario As ColCampoUsuario


#End Region

#Region "Constructores"
    ''' <overloads>El constructor esta sobrecargado.</overloads>
    ''' <summary>Constructor por defecto.</summary>
    Public Sub New()
        Me.mEstado = EstadoDatosDN.Inconsistente
    End Sub

    ''' <summary>Constructor que admite la fecha de modificacion del objeto.</summary>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion que vamos a asignar al objeto.
    ''' </param>
    Public Sub New(ByVal pFechaModificacion As DateTime)
        mFechaModificacion = pFechaModificacion
    End Sub

    ''' <summary>Constructor que recibe toda la informacion de la entidad.</summary>
    ''' <param name="pID" type="String">
    ''' Id que vamos a asignar al objeto.
    ''' </param>
    ''' <param name="pNombre" type="String">
    ''' Nombre que vamos a asignar al objeto.
    ''' </param>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion que vamos a asignar al objeto.
    ''' </param>
    ''' <param name="pBaja" type="Boolean">
    ''' Estado de la baja que vamos a asignar al objeto.
    ''' </param>
    Public Sub New(ByVal pID As String, ByVal pNombre As String, ByVal pFechaModificacion As DateTime, ByVal pBaja As Boolean)
        MyBase.New(pID, pNombre)

        Dim mensaje As String = String.Empty

        If (ValFechaModificacion(pFechaModificacion, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If

        mFechaModificacion = pFechaModificacion
        Me.mBaja = pBaja
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene o modifica el id de la entidad.</summary>
    <System.ComponentModel.Browsable(False)> Public Overrides Property ID() As String
        Get
            Return Me.mID
        End Get
        Set(ByVal Value As String)
            CambiarValorPropiedadVal(Value, Me.mID)
        End Set
    End Property

    ''' <summary>Obtiene o modifica el nombre de la entidad.</summary>
    Public Overrides Property Nombre() As String
        Get
            Return Me.mNombre
        End Get
        Set(ByVal Value As String)
            CambiarValorPropiedadVal(Value, Me.mNombre)
        End Set
    End Property

    ''' <summary>Obtiene la fecha de modificacion de la entidad.</summary>
    <System.ComponentModel.Browsable(False)> Public Overridable ReadOnly Property FechaModificacion() As DateTime Implements IEntidadDN.FechaModificacion
        Get
            Return mFechaModificacion
        End Get
    End Property

    ''' <summary>Obtiene el estado de la baja de la entidad.</summary>
    Public ReadOnly Property Baja() As Boolean Implements IEntidadDN.Baja
        Get
            Return Me.mBaja
        End Get
    End Property

    <System.ComponentModel.Browsable(False)> Protected Overloads WriteOnly Property modificarEstado() As EstadoDatosDN
        Set(ByVal Value As EstadoDatosDN)
            Me.modificarEstado(False) = Value
        End Set
    End Property

    ''' <summary>Modifica el estado de modificacion de la entidad.</summary>
    <System.ComponentModel.Browsable(False)> _
    Protected Overloads WriteOnly Property ModificarEstado(ByVal pSombrearEstadoHijos As Boolean) As EstadoDatosDN
        Set(ByVal Value As EstadoDatosDN)
            '    If (Me.mEstado <> Value) Then
            '        Me.mEstado = Value

            '        If (Not Value = EstadoDatosDN.Inconsistente) Then
            '            OnCambioEstadoDatos()
            '        End If
            '    End If
            'End Set
            If Me.mEstado <> Value Then

                ' ***** impedir sombrear modificaciones *****
                ' este codigo impide que un objeto en su cosntructor modifique a una de sus partes, con lo que su parte estaria modificada
                ' y que en su ultima linea de constructor establezca su estoda a sinmodificar , con lo que
                ' sombrearia las modificaciones de sus partes

                If Value = EstadoDatosDN.SinModificar AndAlso pSombrearEstadoHijos = False Then
                    Dim entidadPersistente As IDatoPersistenteDN
                    For Each entidadPersistente In Me.mColPartes
                        If entidadPersistente.EstadoDatos <> EstadoDatosDN.SinModificar Then
                            Exit Property
                        End If
                    Next
                End If



                'Me.mEstado = Value
                'If Not Value = EstadoDatosDN.Inconsistente AndAlso mEstado <> EstadoDatosDN.SinModificar Then
                '    OnCambioEstadoDatos()
                'End If

                Dim estadoAnteriror As EstadoDatosDN
                estadoAnteriror = mEstado
                Me.mEstado = Value
                If Me.mEstado <> estadoAnteriror Then
                    OnCambioEstadoDatos()
                End If
            End If

        End Set
    End Property

    ''' <summary>Obtiene el estado de modificacion de la entidad.</summary>
    <System.ComponentModel.Browsable(False)> Public ReadOnly Property Estado() As EstadoDatosDN Implements IEntidadDN.Estado
        Get
            Return Me.mEstado
        End Get
    End Property

    ''' <summary>Obtiene o modifica la fecha de modificacion de la entidad.</summary>
    <System.ComponentModel.Browsable(False)> Private Property FechaModificacionPersistente() As Date Implements IDatoPersistenteDN.FechaModificacion
        Get
            Return Me.mFechaModificacion
        End Get
        Set(ByVal Value As Date)
            Dim mensaje As String = String.Empty

            If (ValFechaModificacion(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            Me.mFechaModificacion = Value
        End Set
    End Property

    ''' <summary>Obtiene o modifica el estado de la baja de la entidad.</summary>
    Protected Overridable Property BajaPersistente() As Boolean Implements IDatoPersistenteDN.Baja
        Get
            Return Me.mBaja
        End Get
        Set(ByVal Value As Boolean)
            CambiarValorPropiedadVal(Value, Me.mBaja)
        End Set
    End Property

    ''' <summary>Obtiene o modifica el estado de modificacion de la entidad.</summary>
    Private Property EstadoDatosPersistente() As EstadoDatosDN Implements IDatoPersistenteDN.EstadoDatos
        Get
            Return Me.mEstado
        End Get
        Set(ByVal Value As EstadoDatosDN)
            Me.mEstado = Value
        End Set
    End Property

    Private Property GUID1() As String Implements IDatoPersistenteDN.GUID
        Get
            Return Me.mGUID
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mGUID)
        End Set
    End Property

    Private Property ID1() As String Implements IDatoPersistenteDN.ID
        Get
            Return Me.mID
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mID)

        End Set
    End Property

#End Region

#Region "Metodos Validacion"
    ''' <summary>Metodo que valida que la fecha de modificacion del objeto sea correcta.</summary>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' La fecha que vamos a validar.
    ''' </param> 
    ''' <param name="pMensaje" type="String">
    ''' String donde vamos a devolver un mensaje de error en caso de que no se supere la validacion.
    ''' </param>
    ''' <returns>Si la fecha de modificacion era valida o no.</returns>
    Public Shared Function ValFechaModificacion(ByVal pFechaModificacion As DateTime, ByRef pMensaje As String) As Boolean
        If (Not IsDate(pFechaModificacion)) Then
            pMensaje = "Error: no se puede crear una entidad sin fecha de modificacion."

            Return False
        End If

        Return True
    End Function
#End Region


#Region "Metodos de Registro"
    'TODO: DESCOMENTAR ESTO!!!
    ''' <summary>Metodo que registra a todas las partes de una entidad.</summary>
    ''' <remarks>Se usa reflection para saber las partes que componen a cada entidad.</remarks>
    Public Sub RegistrarParteTodas() Implements IDatoPersistenteDN.RegistrarParteTodas


        Throw New NotImplementedException()


        'Dim rf As New ReflectionHelper.ReflectionHelper

        'Dim campos As Reflection.FieldInfo()
        'Dim campo As Reflection.FieldInfo
        'Dim parte As IEntidadDN


        'campos = rf.ReuperarCampos(Me.GetType)

        'For Each campo In campos
        '    If rf.EsRef(campo.FieldType) Then
        '        If TypeOf campo.GetValue(Me) Is EntidadDN Then
        '            parte = campo.GetValue(Me)
        '            Me.DesRegistrarParte(parte)
        '            Me.RegistrarParte(parte)

        '            'Me._colpartes.Add(campo.GetValue(Me))
        '        End If
        '        If TypeOf campo.GetValue(Me) Is IEnumerable Then
        '            parte = campo.GetValue(Me)
        '            Me.DesRegistrarParte(parte)
        '            Me.RegistrarParte(parte)

        '            'Me._colpartes.Add(campo.GetValue(Me))
        '        End If

        '    End If

        'Next

    End Sub

    Public Overridable Sub ElementoaEliminar(ByVal pSender As Object, ByVal pElemento As Object, ByRef pPermitido As Boolean)

    End Sub
    Public Overridable Sub ElementoaAñadir(ByVal pSender As Object, ByVal pElemento As Object, ByRef pPermitido As Boolean)

    End Sub

    ''' <summary>Metodo que añade una parte a una entidad.</summary>
    ''' <remarks>Al añadir el elemento se registra a los eventos.</remarks>
    ''' <param name="pSender" type="Object">
    ''' Objeto que emite el evento.
    ''' </param>
    ''' <param name="pElemento" type="Object">
    ''' Elemento que añadimos.
    ''' </param>
    Public Overridable Sub ElementoAñadido(ByVal pSender As Object, ByVal pElemento As Object)
        Me.RegistrarParte(pElemento)
        Me.modificarEstado = EstadoDatosDN.Modificado
    End Sub

    ''' <summary>Metodo que elimina una parte de una entidad.</summary>
    ''' <remarks>Al eliminar el elemento se desregistra de los eventos.</remarks>
    ''' <param name="pSender" type="Object">
    ''' Objeto que emite el evento.
    ''' </param>
    ''' <param name="pElemento" type="Object">
    ''' Elemento que eliminamos.
    ''' </param>
    Public Overridable Sub ElementoEliminado(ByVal pSender As Object, ByVal pElemento As Object)
        Me.DesRegistrarParte(pElemento)
        Me.modificarEstado = EstadoDatosDN.Modificado
    End Sub

    ''' <summary>Metodo que cambia el estado de modificacionde una entidad a Modificado.</summary>
    ''' <remarks>
    ''' Ha de ser public por restricciones de serializacion y delegados.
    ''' </remarks>
    ''' <param name="pSender" type="Object">
    ''' Objeto que emite el evento.
    ''' </param>
    Public Sub CambioEstadoHijo(ByVal pSender As Object)
        Me.modificarEstado = EstadoDatosDN.Modificado
    End Sub


    Public Sub RegistrarParte(ByVal Parte As Object) Implements IDatoPersistenteDN.RegistrarParte
        If TypeOf Parte Is IModificable Then
            RegistrarParteIEntidadDN(Parte)
        End If
        If TypeOf Parte Is IEnumerable Then
            RegistrarParteIEnumerable(Parte)
        End If
        If TypeOf Parte Is IColEventos Then
            RegistrarParteIColEventos(Parte)
        End If

    End Sub
    Public Sub DesRegistrarParte(ByVal Parte As Object) Implements IDatoPersistenteDN.DesRegistrarParte
        If Parte Is Nothing Then Exit Sub

        If TypeOf Parte Is IModificable Then
            DesRegistrarParteIEntidadDN(Parte)
        End If
        If TypeOf Parte Is IEnumerable Then
            DesRegistrarParteIEnumerable(Parte)
        End If
        If TypeOf Parte Is IColEventos Then
            DesRegistrarParteIColEventos(Parte)
        End If

    End Sub

    Public Sub DesRegistrarTodo()
        Dim edn As IModificable

        Dim lista As New List(Of IModificable)
        lista.AddRange(mColPartes)

        For Each edn In lista
            DesRegistrarParte(edn)
        Next

    End Sub

    Private Sub RegistrarParteIEntidadDN(ByVal Parte As IModificable)
        If Parte Is Nothing Then
            Exit Sub
        End If

        '    RemoveHandler Parte.CambioEstadoDatos, AddressOf CambioEstdoHijo

        Me.DesRegistrarParte(Parte)
        AddHandler Parte.CambioEstadoDatos, AddressOf CambioEstadoHijo
        mColPartes.Add(Parte)

    End Sub
    Private Sub RegistrarParteIEnumerable(ByVal colHijo As IEnumerable)

        ' registra los hijos de una coleccion contra la entidad dn que contiene la coleccion
        Dim elemento As IEntidadBaseDN
        ' Dim Parte As IModificable
        If colHijo Is Nothing Then
            Exit Sub
        End If

        For Each elemento In colHijo
            If TypeOf elemento Is IModificable Then
                RegistrarParte(elemento)
            End If

            'RemoveHandler Parte.CambioEstadoDatos, AddressOf CambioEstdoHijo
            'AddHandler Parte.CambioEstadoDatos, AddressOf CambioEstdoHijo
        Next


    End Sub
    Private Sub DesRegistrarParteIEnumerable(ByVal colHijo As IEnumerable)
        Dim Parte As IEntidadBaseDN
        If Not colHijo Is Nothing Then
            For Each Parte In colHijo
                If TypeOf Parte Is IModificable Then
                    DesRegistrarParteIEntidadDN(Parte)
                End If
            Next
        End If



    End Sub



    Private Sub RegistrarParteIColEventos(ByVal colHijo As IColEventos)
        ' si ala colecciones ivolevent registra sus eventos de parte añadida y eliminada a la entidad dn que la contiene

        If colHijo Is Nothing Then
            Exit Sub
        End If
        DesRegistrarParteIColEventos(colHijo)
        AddHandler colHijo.ElementoAñadido, AddressOf Me.ElementoAñadido
        AddHandler colHijo.ElementoEliminado, AddressOf Me.ElementoEliminado
        AddHandler colHijo.ElementoaAñadir, AddressOf Me.ElementoaAñadir
        AddHandler colHijo.ElementoaEliminar, AddressOf Me.ElementoaEliminar

    End Sub
    Private Sub DesRegistrarParteIColEventos(ByVal colHijo As IColEventos)
        If colHijo Is Nothing Then
            Exit Sub
        End If

        RemoveHandler colHijo.ElementoAñadido, AddressOf Me.ElementoAñadido
        RemoveHandler colHijo.ElementoEliminado, AddressOf Me.ElementoEliminado
        RemoveHandler colHijo.ElementoaAñadir, AddressOf Me.ElementoaAñadir
        RemoveHandler colHijo.ElementoaEliminar, AddressOf Me.ElementoaEliminar

    End Sub
    Private Sub DesRegistrarParteIEntidadDN(ByVal Parte As IModificable)
        If Not Parte Is Nothing Then
            RemoveHandler Parte.CambioEstadoDatos, AddressOf CambioEstadoHijo
            mColPartes.Remove(Parte)
        End If

    End Sub


    '''' <overloads>Este metodo esta sobrecargado.</overloads>
    '''' <summary>Metodo que registra un objeto como parte de nosotros.</summary>
    '''' <remarks>
    '''' Este metodo nos permite ser informados de los eventos de modificacion del objeto que registramos.
    '''' </remarks>
    '''' <param name="pParte" type="IEntidadDN">
    '''' Parte que vamos a registrar.
    '''' </param>
    ''Public Sub RegistrarParte(ByVal pParte As IEntidadDN)
    ''    If (pParte Is Nothing) Then
    ''        Exit Sub
    ''    End If

    ''    'Desregistramos la parte de los eventos (por si estaba ya registrada)
    ''    Me.DesregistrarParte(pParte)

    ''    'Añadimos el manejador de eventos y añadimos la parte a la coleccion de partes de esta entidad
    ''    AddHandler pParte.CambioEstadoDatos, AddressOf CambioEstadoHijo
    ''    mColPartes.Add(pParte)
    ''End Sub

    '''' <summary>Metodo que registra una coleccion de objetos como parte de nosotros.</summary>
    '''' <remarks>
    '''' Este metodo nos permite ser informados de los eventos de modificacion de los objetos de la coleccion
    '''' que registramos.
    '''' </remarks>
    '''' <param name="pColPartes" type="IEnumerable">
    '''' Coleccion que vamos a registrar.
    '''' </param>
    ''Public Sub RegistrarParte(ByVal pColPartes As IEnumerable)
    ''    Dim Parte As IEntidadDN

    ''    If (pColPartes Is Nothing) Then
    ''        Exit Sub
    ''    End If

    ''    'Registramos cada parte de la coleccion
    ''    For Each Parte In pColPartes
    ''        RegistrarParte(Parte)
    ''    Next
    ''End Sub

    '''' <overloads>Este metodo esta sobrecargado.</overloads>
    '''' <summary>Metodo que desregistra un objeto como parte de nosotros.</summary>
    '''' <remarks>
    '''' Este metodo hace que no seamos informados mas de los eventos de modificacion que emite el objeto que
    '''' desregistramos.
    '''' </remarks>
    '''' <param name="pParte" type="IEntidadDN">
    '''' Parte que vamos a desregistrar.
    '''' </param>
    'Public Sub DesregistrarParte(ByVal pParte As IEntidadDN)
    '    If (Not pParte Is Nothing) Then
    '        RemoveHandler pParte.CambioEstadoDatos, AddressOf CambioEstadoHijo
    '        mColPartes.Remove(pParte)
    '    End If
    'End Sub

    '''' <summary>Metodo que desregistra una coleccion de objetos como parte de nosotros.</summary>
    '''' <remarks>
    '''' Este metodo hace que no seamos informados mas de los eventos de modificacion que emiten los objetos de la
    '''' coleccion que desregistramos.
    '''' </remarks>
    '''' <param name="pColPartes" type="IEnumerable">
    '''' Coleccion que vamos a desregistrar.
    '''' </param>
    'Public Sub DesregistrarParte(ByVal pColPartes As IEnumerable)
    '    Dim Parte As IEntidadDN

    '    'Desregistramos cada objeto de la coleccion
    '    For Each Parte In pColPartes
    '        If (Not Parte Is Nothing) Then
    '            DesregistrarParte(Parte)
    '        End If
    '    Next
    'End Sub
#End Region

#Region "Metodos"


    ''' <overloads>Este metodo esta sobrecargado.</overloads>
    ''' <summary>Metodo que cambia el valor de una propiedad que sea una enumeracion.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pNuevoValor" type="[Enum]">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="[Enum]">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadVal(ByVal pNuevoValor As [Enum], ByRef pCampoDestino As [Enum])
        If (Not pNuevoValor.Equals(pCampoDestino)) Then
            pCampoDestino = pNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea un booleano.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pNuevoValor" type="Boolean">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="Boolean">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadVal(ByVal pNuevoValor As Boolean, ByRef pCampoDestino As Boolean)
        If (pNuevoValor <> pCampoDestino) Then
            pCampoDestino = pNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea un date.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pNuevoValor" type="Date">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="Date">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadVal(ByVal pNuevoValor As Date, ByRef pCampoDestino As Date)
        If (pNuevoValor <> pCampoDestino) Then
            pCampoDestino = pNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea un string.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pNuevoValor" type="String">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="String">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadVal(ByVal pNuevoValor As String, ByRef pCampoDestino As String)
        If (pNuevoValor <> pCampoDestino) Then
            pCampoDestino = pNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea un double.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pNuevoValor" type="Double">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="Double">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadVal(ByVal pNuevoValor As Double, ByRef pCampoDestino As Double)
        If (pNuevoValor <> pCampoDestino) Then
            pCampoDestino = pNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea un int64.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pNuevoValor" type="Int64">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="Int64">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadVal(ByVal pNuevoValor As Int64, ByRef pCampoDestino As Int64)
        If (pNuevoValor <> pCampoDestino) Then
            pCampoDestino = pNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea una EntidadBaseDN.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pEntidadNuevoValor" type="EntidadBaseDN">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="EntidadBaseDN">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadEntidadBaseRef(ByVal pEntidadNuevoValor As EntidadBaseDN, ByRef pCampoDestino As EntidadBaseDN)
        'Si es una IEntidadDN la tratamos con CambiarValorPropiedadEntidadRef
        If (TypeOf pEntidadNuevoValor Is IEntidadDN OrElse TypeOf pCampoDestino Is IEntidadDN) Then
            CambiarValorPropiedadEntidadRef(pEntidadNuevoValor, pCampoDestino)
            Exit Sub
        End If

        'Si ninguno es nothing comprobamos si lo hemos modificado o hemos asignado el mismo valor que teniamos antes
        If (Not (pEntidadNuevoValor Is Nothing AndAlso pCampoDestino Is Nothing)) Then
            If (Not pEntidadNuevoValor Is Nothing AndAlso Not pCampoDestino Is Nothing) Then
                'Si los valores son diferentes modificamos el campo y marcamos el objeto como modificado
                If (Not pEntidadNuevoValor Is pCampoDestino) Then
                    Me.modificarEstado = EstadoDatosDN.Modificado
                    pCampoDestino = pEntidadNuevoValor
                End If

            Else
                'Si uno es nothing y el otro no tambien lo modificamos
                pCampoDestino = pEntidadNuevoValor
                Me.modificarEstado = EstadoDatosDN.Modificado
            End If
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea un Object.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pEntidadNuevoValor" type="Object">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="Object">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadObjectRef(ByVal pEntidadNuevoValor As Object, ByRef pCampoDestino As Object)


        'Si el object es una IEntidadBaseDN llamamos a CambiarValorPropiedadEntidadBaseRef
        If (TypeOf pEntidadNuevoValor Is IEntidadBaseDN OrElse TypeOf pCampoDestino Is IEntidadBaseDN) Then
            Me.CambiarValorPropiedadEntidadBaseRef(pEntidadNuevoValor, pCampoDestino)
            Exit Sub
        End If

        'Si el object es una IEnumerable llamamos a CambiarValorPropiedadColEntidadRef ya que se trata de una coleccion
        If (TypeOf pEntidadNuevoValor Is IEnumerable OrElse TypeOf pCampoDestino Is IEnumerable) Then
            Me.CambiarValorPropiedadColEntidadRef(pEntidadNuevoValor, pCampoDestino)
            Exit Sub
        End If


        'Si el nuevo valor es diferente del anterior lo modificamos y lo marcamos como Modificado
        If (Not pEntidadNuevoValor Is pCampoDestino) Then
            pCampoDestino = pEntidadNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Metodo que cambia el valor de una propiedad que sea una EntidadDN.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pEntidadNuevoValor" type="EntidadDN">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="EntidadDN">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadEntidadRef(ByVal pEntidadNuevoValor As EntidadDN, ByRef pCampoDestino As EntidadDN)
        Dim valorAntiguo As EntidadDN

        'Si el nuevo valor es diferente del anterior lo modificamos y lo marcamos como Modificado
        If (Not pEntidadNuevoValor Is pCampoDestino) Then
            'Guardamos el valor anterior para desregistrarlo de los eventos
            valorAntiguo = pCampoDestino

            'Asignamos el nuevo valor y lo marcamos
            pCampoDestino = pEntidadNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado

            'Desregistramos el valor antiguo y registramos el nuevo
            Me.DesRegistrarParte(valorAntiguo)
            Me.RegistrarParte(pCampoDestino)
        End If
    End Sub

    ''' <summary>
    ''' Vesion generica para evitar que el primer y el segundo campo sean de distinto tipo
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="pNuevoValor"></param>
    ''' <param name="pCampoDestino"></param>
    ''' <remarks></remarks>
    Protected Sub CambiarValorRef(Of T)(ByVal pNuevoValor As T, ByRef pCampoDestino As T)
        Dim var As Object = pCampoDestino
        CambiarValorPropiedadObjectRef(pNuevoValor, var)
        pCampoDestino = var
    End Sub

    ''' <summary>
    ''' Vesion generica para evitar que el primer y el segundo campo sean de distinto tipo
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="pNuevoValor"></param>
    ''' <param name="pCampoDestino"></param>
    ''' <remarks></remarks>
    Protected Sub CambiarValorCol(Of T As IEnumerable)(ByVal pNuevoValor As T, ByRef pCampoDestino As T)
        Dim aux As IEnumerable
        aux = pCampoDestino
        CambiarValorPropiedadColEntidadRef(pNuevoValor, aux)
        pCampoDestino = aux
    End Sub


    ''' <summary>
    ''' Vesion generica para evitar que el primer y el segundo campo sean de distinto tipo
    ''' Debe usarse cuando el tipo es un tipo por valor segun la base de datos. (un string, un datetime, o un byte() lo son para una base de datos)
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <param name="pNuevoValor"></param>
    ''' <param name="pCampoDestino"></param>
    ''' <remarks></remarks>
    Protected Sub CambiarValorVal(Of T)(ByVal pNuevoValor As T, ByRef pCampoDestino As T)
        If pNuevoValor Is Nothing Then
            If pCampoDestino IsNot Nothing Then
                pCampoDestino = pNuevoValor
                Me.modificarEstado = EstadoDatosDN.Modificado
            End If
        Else
            If (Not pNuevoValor.Equals(pCampoDestino)) Then
                pCampoDestino = pNuevoValor
                Me.modificarEstado = EstadoDatosDN.Modificado
            End If
        End If



    End Sub



    ''' <summary>Metodo que cambia el valor de una propiedad que sea un IEnumerable.</summary>
    ''' <remarks>
    ''' Se utiliza este metodo en vez del operador de asignacion para llevar control del estado de modificacion.
    ''' </remarks>
    ''' <param name="pColEntidadNuevoValor" type="IEnumerable">
    ''' Nuevo valor que vamos a asignar.
    ''' </param>
    ''' <param name="pCampoDestino" type="IEnumerable">
    ''' Campo al que vamos a asignar el valor.
    ''' </param>
    Protected Overridable Sub CambiarValorPropiedadColEntidadRef(ByVal pColEntidadNuevoValor As IEnumerable, ByRef pCampoDestino As IEnumerable)
        Dim valorAntiguo As IEnumerable

        'Si no son el mismo valor los modificamos
        If (Not pColEntidadNuevoValor Is pCampoDestino) Then
            'Nos guardamos el valor antiguo para desregistrarlo
            valorAntiguo = pCampoDestino

            'Asignamos el nuevo valor
            pCampoDestino = pColEntidadNuevoValor

            'Desregistramos el valor antiguo y registramos el nuevo
            Me.DesRegistrarParte(valorAntiguo)
            Me.RegistrarParte(pCampoDestino)

            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub

    ''' <summary>Obtiene un array de bytes que representa a la entidad serializada de forma binaria.</summary>
    ''' <returns>El array de bytes que representa a la entidad.</returns>
    Public Overridable Function ToBytes() As Byte()
        Dim formateador As New BinaryFormatter
        Dim memoria As New MemoryStream

        formateador.Serialize(memoria, Me)

        Return memoria.GetBuffer
    End Function

    ''' <summary>Obtiene una copia en profundidad de la entidad.</summary>
    ''' <returns>La copia de la entidad.</returns>
    Public Overridable Function Clone() As Object Implements ICloneable.Clone
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

    ''' <summary>Obtiene una copia en profundidad de la entidad.</summary>
    ''' <returns>La copia de la entidad.</returns>
    Public Function CloneSinIdentidad() As Object
        Dim formateador As BinaryFormatter
        Dim memoria As MemoryStream

        formateador = New BinaryFormatter
        memoria = New MemoryStream

        'Nos serializamos y volvemos a poner el puntero de lectura/escritura al principio
        formateador.Serialize(memoria, Me)
        memoria.Seek(0, IO.SeekOrigin.Begin)
        Dim miDatoPersistente As IDatoPersistenteDN

        'Nos desserializamos para conseguir la copia
        miDatoPersistente = formateador.Deserialize(memoria)
        'miDatoPersistente.GUID = System.Guid.NewGuid.ToString()
        'miDatoPersistente.ID = ""
        NuevaIdentidad(miDatoPersistente)
        Return miDatoPersistente
    End Function


    Protected Sub NuevaIdentidad(ByVal pDatoPersistente As IDatoPersistenteDN)


        pDatoPersistente.GUID = System.Guid.NewGuid.ToString()
        pDatoPersistente.ID = ""


    End Sub

    ''' <summary>Obtiene una copia en superficial de la entidad.</summary>
    ''' <returns>La copia de la entidad.</returns>
    Public Overridable Function CloneSuperficial() As Object
        Return Me.MemberwiseClone()
    End Function
    Public Overridable Function CloneSuperficialSinIdentidad() As Object
        Dim miDatoPersistente As IDatoPersistenteDN = Me.MemberwiseClone()
        NuevaIdentidad(miDatoPersistente)
        Return miDatoPersistente
    End Function
    ''' <summary>Metodo que obtiene el estado de integridad de la entidad.</summary>
    ''' <remarks>
    ''' Para obtener el estado de la entidad se comprueba el estado de todas sus partes.
    ''' </remarks>
    ''' <param name="pMensaje" type="String">
    ''' Mensaje de informacion sobre el porque del estado de integridad de la entidad.
    ''' </param>
    ''' <returns>El estado de integridad de la entidad</returns>
    Public Overridable Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN Implements IDatoPersistenteDN.EstadoIntegridad
        Dim parte As IDatoPersistenteDN
        Dim modificable As IModificable

        Me.mToSt = Me.ToString


        'Si estamos en medio de una validacion lo indicamos
        If (validando = True) Then
            Return EstadoIntegridadDN.EnProcesoValidacion
        End If

        validando = True

        'Comprobamos el estado de cada una de las partes
        For Each modificable In mColPartes
            If TypeOf modificable Is IDatoPersistenteDN Then
                parte = modificable



                If (Not parte.EstadoIntegridad(pMensaje) = EstadoIntegridadDN.Consistente AndAlso Not parte.EstadoIntegridad(pMensaje) = EstadoIntegridadDN.EnProcesoValidacion) Then
                    validando = False
                    Return EstadoIntegridadDN.Inconsistente
                End If
            End If
        Next

        validando = False



        Return EstadoIntegridadDN.Consistente
    End Function

    Public Overrides Function ToString() As String
        Return Me.mNombre
    End Function
#End Region

#Region "Eventos"
    ''' <summary>Evento que indica que se ha modificado el valor de algun atributo.</summary>
    ''' <param name="pSender" type="Object">
    ''' Objeto que emite el evento.
    ''' </param>
    Public Event CambioEstadoDatos(ByVal pSender As Object) Implements IEntidadDN.CambioEstadoDatos
#End Region

#Region "Manejadores Eventos"
    ''' <summary>Emite un evento de modificacion de datos.</summary>
    Protected Overridable Sub OnCambioEstadoDatos()
        RaiseEvent CambioEstadoDatos(Me)
    End Sub
#End Region


    'Public Property ColCampoUsuario() As System.Collections.Generic.List(Of ICampoUsuario) Implements IEntidadDN.ColCampoUsuario
    '    Get
    '        Return mColCampoUsuario
    '    End Get
    '    Set(ByVal value As System.Collections.Generic.List(Of ICampoUsuario))
    '        Me.CambiarValorCol(Of List(Of ICampoUsuario))(value, mColCampoUsuario)
    '    End Set
    'End Property

    Public Property CampoUsuario(ByVal clave As String) As ICampoUsuario Implements IEntidadDN.CampoUsuario
        Get
            Return mColCampoUsuario.campo(clave)
        End Get
        Set(ByVal value As ICampoUsuario)
            mColCampoUsuario.campo(clave).Valor = value
        End Set
    End Property


    Public Property ColCampoUsuario() As ColCampoUsuario Implements IEntidadDN.ColCampoUsuario
        Get
            Return Me.mColCampoUsuario
        End Get
        Set(ByVal value As ColCampoUsuario)
            Me.CambiarValorCol(Of ColCampoUsuario)(value, Me.mColCampoUsuario)
        End Set
    End Property

    'Public Overridable Function ActualizarHashValores() As String Implements IDatoPersistenteDN.ActualizarHashValores
    '    'mHashValores = Nothing
    'End Function

    'Public Overridable ReadOnly Property HashValores() As String Implements IDatoPersistenteDN.HashValores
    '    Get
    '        Return mHashValores
    '    End Get
    'End Property

    Public Function ActualizarHashValores() As String Implements IDatoPersistenteDN.ActualizarHashValores

    End Function

    Public ReadOnly Property HashValores() As String Implements IDatoPersistenteDN.HashValores
        Get
            Return Nothing
        End Get
    End Property

    Public Overridable Function AsignarEntidad(ByVal pEntidad As IEntidadBaseDN) As Boolean Implements IEntidadDN.AsignarEntidad
        Return False
    End Function

    Public Overridable Function InstanciarEntidad(ByVal pTipo As System.Type) As IEntidadBaseDN Implements IEntidadDN.InstanciarEntidad
        Return Nothing
    End Function

    Public Overridable Function InstanciarEntidad(ByVal pTipo As System.Type, ByVal pPropidadDestino As System.Reflection.PropertyInfo) As IEntidadBaseDN Implements IEntidadDN.InstanciarEntidad
        Return Nothing
    End Function


    ''' <summary>
    ''' genera un ht donde las claves son los guid de los tipos por referencia contenidos y los valores son los propios tipos por referencia
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>

    Public Function ToHtGUIDs(ByVal phtGUIDEntidades As System.Collections.Hashtable, ByRef clones As ColIEntidadDN) As System.Collections.Hashtable Implements IEntidadDN.ToHtGUIDs


        If clones Is Nothing Then
            clones = New ColIEntidadDN
        End If

        If phtGUIDEntidades Is Nothing Then
            phtGUIDEntidades = New System.Collections.Hashtable
        Else
            ' si ya estoy procesado  o procensando no continuo

            If phtGUIDEntidades.ContainsKey(Me.mGUID) Then
                Dim entidad As IEntidadDN = phtGUIDEntidades.Item(Me.mGUID)

                ' si no soy yo es que soy un clon
                If Not entidad Is Me Then

                    If Not clones.Contains(Me) Then
                        clones.Add(Me)
                    End If
                    If Not clones.Contains(entidad) Then
                        clones.Add(entidad)
                    End If
                End If
                Return phtGUIDEntidades
            End If
        End If
        ' me añado ami y luego a mis referecnias

        phtGUIDEntidades.Add(Me.mGUID, Me)


        For Each entidad As IEntidadDN In Me.mColPartes
            entidad.ToHtGUIDs(phtGUIDEntidades, clones)
        Next

        Return phtGUIDEntidades


    End Function
End Class

<Serializable()> Public Class ColEntidadesDN
    Inherits ArrayListValidable(Of EntidadDN)

    ' metodos de coleccion
    '
End Class

<Serializable()> _
Public Structure CampoUsuario
    Implements ICampoUsuario


    Private mClave As String
    Private mvalor As String
    Private mEstado As EstadoDatosDN

    Public Event CambioEstadoDatos(ByVal sender As Object) Implements IModificable.CambioEstadoDatos

    Public Sub New(ByVal pClave As String, ByVal pValor As String)
        mvalor = pValor
        Me.mClave = pClave
        Me.mEstado = EstadoDatosDN.SinModificar
    End Sub
    Public Property Clave() As String Implements ICampoUsuario.Clave
        Get
            Return Me.mClave
        End Get
        Set(ByVal value As String)
            Me.CambiarValorPropiedadVal(value, Me.mClave)
        End Set
    End Property

    Public Property Valor() As String Implements ICampoUsuario.Valor
        Get
            Return Me.mvalor
        End Get
        Set(ByVal value As String)
            Me.CambiarValorPropiedadVal(value, Me.mvalor)
        End Set
    End Property


    'Public ReadOnly Property Estado() As EstadoDatosDN Implements IModificable.Estado
    '    Get
    '        Return mEstado
    '    End Get
    'End Property

    Public ReadOnly Property FechaModificacion() As Date Implements IModificable.FechaModificacion
        Get
            Return Date.MinValue
        End Get
    End Property


    Private Sub CambiarValorPropiedadVal(ByVal pNuevoValor As String, ByRef pCampoDestino As String)
        If (pNuevoValor <> pCampoDestino) Then
            pCampoDestino = pNuevoValor
            Me.modificarEstado = EstadoDatosDN.Modificado
        End If
    End Sub


    <System.ComponentModel.Browsable(False)> Private Overloads WriteOnly Property modificarEstado() As EstadoDatosDN
        Set(ByVal Value As EstadoDatosDN)


            Dim estadoAnteriror As EstadoDatosDN
            estadoAnteriror = mEstado
            Me.mEstado = Value
            If Me.mEstado <> estadoAnteriror Then
                OnCambioEstadoDatos()
            End If
        End Set
    End Property
    Private Sub OnCambioEstadoDatos()
        RaiseEvent CambioEstadoDatos(Me)
    End Sub
End Structure




<Serializable()> _
Public Class ColCampoUsuario
    Inherits System.Collections.Generic.List(Of ICampoUsuario)
    Public ReadOnly Property campo(ByVal clave As String) As ICampoUsuario
        Get
            Dim icu As ICampoUsuario
            For Each icu In Me
                If icu.Clave = clave Then
                    Return icu
                End If
            Next
            Return Nothing
        End Get
    End Property
End Class


<Serializable()> _
Public MustInherit Class EntidadTipoDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pNombre As String)
        Dim mensaje As String
        mensaje = ""

        If Not Me.ValNombre(mensaje, pNombre) Then
            Throw New ApplicationExceptionDN(mensaje)
        End If

        Me.mNombre = pNombre

        Me.mGUID = Me.GetType.Name & "-" & pNombre
        'Me.mHashValores = Me.mGUID
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Overrides Property Nombre() As String
        Get
            Return MyBase.Nombre
        End Get
        Set(ByVal value As String)
            Dim mensaje As String
            mensaje = ""

            If Not Me.ValNombre(mensaje, value) Then
                Throw New ApplicationExceptionDN(mensaje)
            End If

            MyBase.Nombre = value
            Me.mGUID = Me.GetType.Name & "-" & value
            'Me.mHashValores = Me.mGUID
        End Set
    End Property

#End Region

#Region "Métodos de validación"

    Private Function ValNombre(ByRef mensaje As String, ByVal pNombre As String) As Boolean
        If String.IsNullOrEmpty(pNombre) Then
            mensaje = "El campo nombre no puede ser nulo"
            Return False
        End If

        Return True

    End Function

#End Region

End Class