''' <summary>
''' representa una carpeta compartida en web que el servidor de documentos puede usar para almacenar ficheros  hasta que se llene su espacio
''' una vez cerrada no volverá a ser usada hasta ser reavierta
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class RutaAlmacenamientoFicherosDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "Atributos"
    Protected mRutaCarpeta As String ' ruta a la carpeta de almacenamiento
    Protected mEstadoRAF As RutaAlmacenamientoFicherosEstado
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        mEstadoRAF = RutaAlmacenamientoFicherosEstado.Disponible
        modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub

    Public Sub New(ByVal nombre As String, ByVal rutaCarpeta As String)
        CambiarValorVal(Of String)(nombre, mNombre)
        CambiarValorVal(Of String)(rutaCarpeta, mRutaCarpeta)
        CambiarValorRef(Of RutaAlmacenamientoFicherosEstado)(RutaAlmacenamientoFicherosEstado.Disponible, mEstadoRAF)

        modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Property RutaCarpeta() As String
        Get
            Return Me.mRutaCarpeta
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mRutaCarpeta)
        End Set
    End Property

    Public Property EstadoRAF() As RutaAlmacenamientoFicherosEstado
        Get
            Return Me.mEstadoRAF
        End Get
        Set(ByVal value As RutaAlmacenamientoFicherosEstado)
            If value = RutaAlmacenamientoFicherosEstado.Disponible Then
                Me.mPeriodo.FFinal = Now
            End If

            Me.CambiarValorVal(Of RutaAlmacenamientoFicherosEstado)(value, mEstadoRAF)
        End Set
    End Property

#End Region

#Region "Métodos"

    ''' <summary>
    '''  genera la ruta a fichero desde su huella para el momento en el que sera guardado
    '''  una vez guardado la ruta al fichero estará contenida en su guella
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function GenerarRuta() As String
        Return "\a" & Now.Year.ToString & "\m" & Now.Month.ToString & "\d" & Now.Day.ToString
    End Function

#End Region


End Class

<Serializable()> _
Public Class ColRutaAlmacenamientoFicherosDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of RutaAlmacenamientoFicherosDN)
End Class

Public Enum RutaAlmacenamientoFicherosEstado
    Disponible
    Abierta
    Incidentada
    Cerrada
End Enum