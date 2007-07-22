Imports MotorBusquedaBasicasDN
<Serializable()> _
Public Class PaqueteFormularioBusqueda
    ''' PAQUETE:
    '''   "Filtro" as MotorBusquedaDN.FiltroDN
    '''   "ParametroCargaEstructura" as ParametroCargaEstructuraDN
    '''   "MultiSelect" as Boolean
    '''   "TipoNavegacion" as MotorIU.Motor.TipoNavegacion
    '''   "Agregable" as Boolean
    '''   "EnviarDatatableAlNavegar" as Boolean
    '''   "Titulo" as String
    '''   "AlternatingBackcolorResultados" as System.Drawing.Color
    '''   "AlternatingBackcolorFiltro" as System.Drawing.Color

    Private mFiltro As MotorBusquedaDN.FiltroDN
    Private mParametroCargaEstructura As ParametroCargaEstructuraDN
    Private mMultiSelect As Boolean = False
    Private mTipoNavegacion As MotorIU.Motor.TipoNavegacion = MotorIU.Motor.TipoNavegacion.Normal
    Private mAgregable As Boolean = False
    Private mEnviarDatatableAlNavegar As Boolean = False
    Private mTitulo As String
    Private mAlternatingBackcolorResultados As System.Drawing.Color
    Private mAlternatingBackcolorFiltro As System.Drawing.Color
    Private mNavegable As Boolean = True
    Private mListaValores As New List(Of ValorCampo)
    Public EjecutarOperacion As Boolean = False
    'Public EntidadReferidora As Framework.DatosNegocio.IEntidadBaseDN
    Public BusquedaAutomatica As Boolean ' hace que el buscador realice la llamada a buscar al iniciarse
    Public DevolucionAutomatica As Boolean ' hace que el formulario no sea visible y devuelva los datos si hay un unico resultdo pero si hay <>1 sea visible
    Private mOcultarAccionesxDefecto As Boolean = False
    Private mFiltroVisible As Boolean = True
    Private mFiltrable As Boolean = True
    Protected mColComandoMap As MV2DN.ColComandoMapDN



    Public Property ColComandoMap() As MV2DN.ColComandoMapDN
        Get
            Return Me.mColComandoMap
        End Get
        Set(ByVal value As MV2DN.ColComandoMapDN)
            mColComandoMap = value
        End Set
    End Property




    Public Property Filtrable() As Boolean
        Get
            Return mFiltrable
        End Get
        Set(ByVal value As Boolean)
            mFiltrable = value
        End Set
    End Property
    Public Property FiltroVisible() As Boolean
        Get
            Return mFiltroVisible
        End Get
        Set(ByVal value As Boolean)
            mFiltroVisible = value
        End Set
    End Property

    Public Property ListaValores() As List(Of ValorCampo)
        Get
            Return Me.mListaValores
        End Get
        Set(ByVal value As List(Of ValorCampo))
            Me.mListaValores = value
        End Set
    End Property


    ''' <summary>
    ''' Si se va a ver el botón de "Navegar" en el listado de resultados
    ''' </summary>
    <System.ComponentModel.DefaultValue(True)> _
    Public Property Navegable() As Boolean
        Get
            Return Me.mNavegable
        End Get
        Set(ByVal value As Boolean)
            Me.mNavegable = value
        End Set
    End Property

    ''' <summary>
    ''' El Filtro que vamos a usar en la búsqueda
    ''' </summary>
    Public Property Filtro() As MotorBusquedaDN.FiltroDN
        Get
            Return Me.mFiltro
        End Get
        Set(ByVal value As MotorBusquedaDN.FiltroDN)
            Me.mFiltro = value
        End Set
    End Property

    ''' <summary>
    ''' Las características especiales de comportamiento cuando se seleccionen
    ''' diferentes campos
    ''' </summary>
    Public Property ParametroCargaEstructura() As ParametroCargaEstructuraDN
        Get
            Return Me.mParametroCargaEstructura
        End Get
        Set(ByVal value As ParametroCargaEstructuraDN)
            Me.mParametroCargaEstructura = value
        End Set
    End Property

    ''' <summary>
    ''' Si el listado de resultados va a permitir seleccionar más de una fila
    ''' </summary>
    Public Property MultiSelect() As Boolean
        Get
            Return Me.mMultiSelect
        End Get
        Set(ByVal value As Boolean)
            Me.mMultiSelect = value
        End Set
    End Property

    ''' <summary>
    ''' El Tipo de Navegación que va a reailzar el Formualrio cuando se le 
    ''' de al botón de navegar (no se tiene en cuenta si el buscador se abre
    ''' modalmente, ya que al navegar se cerrará)
    ''' </summary>
    Public Property TipoNavegacion() As MotorIU.Motor.TipoNavegacion
        Get
            Return Me.mTipoNavegacion
        End Get
        Set(ByVal value As MotorIU.Motor.TipoNavegacion)
            Me.mTipoNavegacion = value
        End Set
    End Property

    ''' <summary>
    ''' Si puede agregarse un elemento a la lista de resultados
    ''' </summary>
    Public Property Agregable() As Boolean
        Get
            Return Me.mAgregable
        End Get
        Set(ByVal value As Boolean)
            Me.mAgregable = value
        End Set
    End Property

    ''' <summary>
    ''' Si cuando se navege (o se vuelva del documento si es modal) se agrega en el
    ''' paquete el datatble de resultados
    ''' </summary>
    Public Property EnviarDatatableAlNavegar() As Boolean
        Get
            Return Me.mEnviarDatatableAlNavegar
        End Get
        Set(ByVal value As Boolean)
            Me.mEnviarDatatableAlNavegar = value
        End Set
    End Property

    ''' <summary>
    ''' El título del formulario de búsqueda
    ''' </summary>
    Public Property Titulo() As String
        Get
            Return Me.mTitulo
        End Get
        Set(ByVal value As String)
            Me.mTitulo = value
        End Set
    End Property

    Public Property AlternatingBackcolorResultados() As System.Drawing.Color
        Get
            Return Me.mAlternatingBackcolorResultados
        End Get
        Set(ByVal value As System.Drawing.Color)
            Me.mAlternatingBackcolorResultados = value
        End Set
    End Property

    Public Property AlternatingBackcolorFiltro() As System.Drawing.Color
        Get
            Return Me.mAlternatingBackcolorFiltro
        End Get
        Set(ByVal value As System.Drawing.Color)
            Me.mAlternatingBackcolorFiltro = value
        End Set
    End Property

    ''' <summary>
    ''' Si los botones guardar y aceptar genéricos son ocultados
    ''' </summary>
    Public Property OcultarAccionesxDefecto() As Boolean
        Get
            Return Me.mOcultarAccionesxDefecto
        End Get
        Set(ByVal value As Boolean)
            Me.mOcultarAccionesxDefecto = value
        End Set
    End Property

End Class


