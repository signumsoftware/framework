#Region "Importaciones"

Imports System.IO
Imports System.Text
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Security.Cryptography

#End Region

''' <summary>Esta clase calcula codigos hash.</summary>
''' <remarks>
''' Esta clase genera codigos hashes de diferentes datos para utilizarse como firma digital
''' o encriptacion de datos para moverlos a traves de una red. Se especifica tambien la
''' codificacion por defecto para las cadenas de texto.
''' </remarks>
Public Class GeneradorHashes

#Region "Atributos"
    'Algoritmo para calcular el hash
    Private mNombre As String
    Private mAlgoritmo As HashAlgorithm
#End Region

    ''' <overloads>El constructor esta sobrecargado.</overloads>
    ''' <summary>Constructor por defecto.</summary>
    ''' <remarks>Se utiliza como algoritmo hash SHA512 y como codificacion UTF8</remarks>
    Public Sub New()
        mNombre = "SHA512"
        mAlgoritmo = CType(CryptoConfig.CreateFromName(mNombre), HashAlgorithm)
    End Sub


    ''' <summary>Obtiene o asigna el algoritmo hash (a partir de su nombre)</summary>
    Public Property Algoritmo() As String
        Get
            Return mNombre
        End Get
        Set(ByVal Value As String)

            mNombre = Value
            mAlgoritmo = CType(CryptoConfig.CreateFromName(Value), HashAlgorithm)

        End Set
    End Property


#Region "Metodos"
    ''' <summary>Metodo que calcula el valor hash de array de bytes</summary>
    ''' <param name="pBytesCodificar" type="Byte()">
    ''' El array de bytes del que vamos a calcular el hash.
    ''' </param>
    ''' <returns>El valor del hash como un array de bytes</returns>
    Public Function CalcularHash(ByVal pBytesCodificar As Byte()) As Byte()
        Return mAlgoritmo.ComputeHash(pBytesCodificar)
    End Function

    ''' <summary>Metodo que calcula el valor hash de un fichero</summary>
    ''' <param name="pRuta" type="String">
    ''' Ruta del fichero del que vamos a calcular el hash.
    ''' </param>
    ''' <returns>El valor del hash como un array de bytes</returns>
    Public Function CalcularHashFile(ByVal pRuta As String) As Byte()

        Using fs As FileStream = File.Open(pRuta, FileMode.Open, FileAccess.Read, FileShare.Read)

            Return mAlgoritmo.ComputeHash(fs)

        End Using

    End Function
#End Region

End Class
