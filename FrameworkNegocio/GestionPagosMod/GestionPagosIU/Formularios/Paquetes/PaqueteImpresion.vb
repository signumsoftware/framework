Public Class PaqueteImpresion
    Inherits MotorIU.PaqueteIU

    ''' <summary>
    ''' El talón documento que se quiere imprimir
    ''' </summary>
    Public TalonDocumento As FN.GestionPagos.DN.TalonDocumentoDN
    ''' <summary>
    ''' La configuración de impresión que se quiere aplicar
    ''' </summary>
    Public ConfiguracionImpresion As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
    ''' <summary>
    ''' Indica como valor de retorno si se ha impreso o se ha cancelado la impresión
    ''' </summary>
    Public Impreso As Boolean
    ''' <summary>
    ''' Indica si el formulario de impresión debe mostrarse o no al usuario
    ''' </summary>
    Public ImpresionSilenciosa As Boolean
    ''' <summary>
    ''' El mensaje de error del servidor al intentar realizar la operación "impresión"
    ''' </summary>
    ''' <remarks></remarks>
    Public MensajeError As String
    ''' <summary>
    ''' La configuración de impresora que usamos en el caso de que sea automático
    ''' </summary>
    ''' <remarks></remarks>
    Public PrinterSettings As System.Drawing.Printing.PrinterSettings
    ''' <summary>
    ''' Si se trata de una impresión de prueba
    ''' </summary>
    Public Prueba As Boolean = False



End Class
