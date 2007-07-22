Namespace Framework.AS
    ''' <summary>
    ''' Esta clase empaqueta objetos en su interior.
    ''' </summary>
    ''' <remarks>
    ''' Esta clase empaqueta objetos serializados en su interior para transportarlos en las comunicaciones cliente y servidor a través
    ''' de un servicio web. Esta clase es serializable.
    ''' </remarks>

    <Serializable()> Public Class ObjetoTransporte

#Region "Atributos"

        'Objeto serializado
        Private mDatos As Byte()
#End Region

#Region "Constructores"

        ''' <overloads>El constructor esta sobrecargado.</overloads>
        ''' <summary>
        ''' Constructor por defecto.
        ''' </summary>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' Constructor que acepta un array de bytes
        ''' </summary>
        ''' <param name="pDatos" type="Byte()">
        ''' Datos binarios que vamos a guardar en el objeto
        ''' </param>
        Public Sub New(ByVal pDatos As Byte())
            mDatos = pDatos
        End Sub
#End Region

#Region "Propiedades"

        ''' <summary>
        ''' Obtiene o asigna los datos que transportamos.
        ''' </summary>
        Public Property Datos() As Byte()
            Get
                Return mDatos
            End Get
            Set(ByVal Value As Byte())
                mDatos = Value
            End Set
        End Property
#End Region

    End Class
End Namespace

