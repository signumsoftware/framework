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

    'Metodo de codificacion
    Private mCodificacion As Encoding

    'Formateador para serializar los objetos
    Private mFormateador As IFormatter
#End Region

#Region "Constructores"
    ''' <overloads>El constructor esta sobrecargado.</overloads>
    ''' <summary>Constructor por defecto.</summary>
    ''' <remarks>Se utiliza como algoritmo hash SHA512 y como codificacion UTF8</remarks>
    Public Sub New()
        mNombre = "SHA512"
        mAlgoritmo = CType(CryptoConfig.CreateFromName(mNombre), HashAlgorithm)
        mCodificacion = New UTF8Encoding
        mFormateador = New BinaryFormatter
    End Sub

    ''' <summary>Constructor con algoritmos definidos por el usuario.</summary>
    ''' <param name="pNombreAlgoritmo" type="String">
    ''' El nombre del algoritmo hash que vamos a utilizar
    ''' </param>
    ''' <param name="pCodificacion" type="Encoding">
    ''' Codificacion que vamos a utilizar
    ''' </param>
    Public Sub New(ByVal pNombreAlgoritmo As String, ByVal pCodificacion As Encoding, ByVal pFormateador As IFormatter)
        Dim mensaje As String = String.Empty

        'Comprobamos los parametros
        If (ValAlgoritmo(pNombreAlgoritmo, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If

        If (ValCodificacion(pCodificacion, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If

        If (ValFormateador(pFormateador, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If

        'Asignaciones
        mNombre = pNombreAlgoritmo
        mAlgoritmo = CType(CryptoConfig.CreateFromName(pNombreAlgoritmo), HashAlgorithm)
        mCodificacion = pCodificacion
        mFormateador = pFormateador
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene o asigna el algoritmo hash (a partir de su nombre)</summary>
    Public Property Algoritmo() As String
        Get
            Return mNombre
        End Get
        Set(ByVal Value As String)
            Dim mensaje As String = String.Empty

            Try
                If (ValAlgoritmo(Value, mensaje) = False) Then
                    Throw New ApplicationException(mensaje)
                End If

                mNombre = Value
                mAlgoritmo = CType(CryptoConfig.CreateFromName(Value), HashAlgorithm)
            Catch ex As Exception
                Throw ex
            End Try
        End Set
    End Property

    ''' <summary>Obtiene o asigna la codificacion</summary>
    Public Property Codificacion() As Encoding
        Get
            Return mCodificacion
        End Get
        Set(ByVal Value As Encoding)
            Dim mensaje As String = String.Empty

            If (ValCodificacion(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mCodificacion = Value
        End Set
    End Property

    ''' <summary>
    ''' Obtiene o asigna el formateador que se utiliza para serializar los objetos para calcular su hash.
    ''' </summary>
    Public Property Formateador() As IFormatter
        Get
            Return mFormateador
        End Get
        Set(ByVal Value As IFormatter)
            Dim mensaje As String = String.Empty

            If (ValFormateador(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mFormateador = Value
        End Set
    End Property
#End Region

#Region "Metodos Validacion"
    ''' <summary>Metodo que valida que el nombre del algoritmo de hash sea correcto.</summary>
    ''' <remarks>Si ocurre algun error, se lanzará una excepción.</remarks>
    ''' <param name="pNombreAlgoritmo" type="String">
    ''' El nombre del algoritmo hash que vamos a validar.
    ''' </param> 
    ''' <param name="pMensaje" type="String">
    ''' String donde vamos a devolver un mensaje de error en caso de que no se supere la validacion.
    ''' </param>
    ''' <returns>Si el nombre era valido o no.</returns>
    Public Shared Function ValAlgoritmo(ByVal pNombreAlgoritmo As String, ByRef pMensaje As String) As Boolean
        Dim algoritmo As HashAlgorithm

        Try
            'Intentamos crear el algoritmo para ver si el nombre es correcto.
            algoritmo = CType(CryptoConfig.CreateFromName(pNombreAlgoritmo), HashAlgorithm)

            If (algoritmo Is Nothing) Then
                pMensaje = "Error: el algoritmo hash no existe."
                Return False
            End If

        Catch ex As Exception
            pMensaje = "Error: el algoritmo hash no existe."
            Return False
        End Try

        Return True
    End Function

    ''' <summary>Método que valida que la codificación es correcta.</summary>
    ''' <param name="pCodificacion" type="Encoding">
    ''' La codificación que vamos a validar.
    ''' </param>
    ''' <param name="pMensaje" type="String">
    ''' String donde vamos a devolver un mensaje de error en caso de que no se supere la validacion.
    ''' </param>
    ''' <returns>Si la codificacion era válida o no.</returns>
    Public Shared Function ValCodificacion(ByVal pCodificacion As Encoding, ByRef pMensaje As String) As Boolean
        If (pCodificacion Is Nothing) Then
            pMensaje = "Error: la codificacion no puede ser nula"
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Comprueba si un formateador es correcto para su utilizacion para serilizar objetos para calcular sus hashes.
    ''' </summary>
    ''' <param name="pFormateador" type="IFormatter">
    ''' El formateador que queremos comprobar.
    ''' </param>
    ''' <param name="pMensaje" type="String">
    ''' String donde vamos a devolver un mensaje de error en caso de que no se supere la validacion.
    ''' </param>
    ''' <return>
    ''' Si el formateador era correcto o no.
    ''' </return>
    Public Shared Function ValFormateador(ByVal pFormateador As IFormatter, ByRef pMensaje As String) As Boolean
        If (pFormateador Is Nothing) Then
            pMensaje = "Error: el formateador no puede ser nulo."
            Return False
        End If

        Return True
    End Function
#End Region

#Region "Metodos"
    ''' <summary>Metodo que calcula el valor hash de array de bytes</summary>
    ''' <param name="pBytesCodificar" type="Byte()">
    ''' El array de bytes del que vamos a calcular el hash.
    ''' </param>
    ''' <returns>El valor del hash como un array de bytes</returns>
    Public Function CalcularHash(ByVal pBytesCodificar As Byte()) As Byte()
        Return mAlgoritmo.ComputeHash(pBytesCodificar)
    End Function

    ''' <summary>Metodo que calcula el valor hash de un string con la codificacion seleccionada</summary>
    ''' <param name="pCadena" type="String">
    ''' El string del que vamos a calcular el hash.
    ''' </param>
    ''' <returns>El valor del hash como un array de bytes</returns>
    Public Function CalcularHash(ByVal pCadena As String) As Byte()
        Return CalcularHash(mCodificacion.GetBytes(pCadena))
    End Function

    ''' <summary>Metodo que calcula el valor hash de un objeto cualquiera</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto del que queremos calcular su hash.
    ''' </param>
    ''' <returns>El valor del hash como un array de bytes</returns>
    Public Function CalcularHash(ByVal pObjeto As Object) As Byte()
        Dim memoria As MemoryStream
        Dim hash As Byte()

        memoria = New MemoryStream

        'Serializamos el objeto en memoria
        mFormateador.Serialize(memoria, pObjeto)

        'Calculamos el hash del objeto y cerramos el flujo
        hash = CalcularHash(memoria.GetBuffer())
        memoria.Close()

        Return hash
    End Function

    ''' <summary>Metodo que calcula el valor hash de un dataset</summary>
    ''' <param name="pDataset" type="DataSet">
    ''' El dataset del que vamos a calcular el hash.
    ''' </param>
    ''' <param name="pModoEscritura" type="XmlWriteMode">
    ''' Modo en el que vamos a escribir el dataset en memoria
    ''' </param>
    ''' <returns>El valor del hash como un array de bytes</returns>
    Public Function CalcularHash(ByVal pDataset As DataSet, ByVal pModoEscritura As XmlWriteMode) As Byte()
        Dim flujo As MemoryStream
        Dim hash As Byte()

        'Escribimos el dataset en representacion XML en memoria
        flujo = New MemoryStream
        pDataset.WriteXml(flujo, pModoEscritura)

        'Calculamos su hash y cerramos el flujo
        hash = CalcularHash(flujo.GetBuffer())
        flujo.Close()

        Return hash
    End Function

    ''' <summary>Metodo que calcula el valor hash de un fichero</summary>
    ''' <param name="pRuta" type="String">
    ''' Ruta del fichero del que vamos a calcular el hash.
    ''' </param>
    ''' <returns>El valor del hash como un array de bytes</returns>
    Public Function CalcularHashFile(ByVal pRuta As String) As Byte()
        Dim fs As FileStream = Nothing
        Dim hash As Byte()

        Try
            fs = File.Open(pRuta, FileMode.Open, FileAccess.Read, FileShare.Read)
            hash = mAlgoritmo.ComputeHash(fs)

        Finally
            If (Not fs Is Nothing) Then
                fs.Close()
            End If
        End Try

        Return hash
    End Function
#End Region

End Class
