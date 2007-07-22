Public Class PaquetePostImpresion
    Inherits MotorIU.PaqueteIU

    ''' <summary>
    ''' El datatable que contiene los datos de los talones que se han impreso
    ''' </summary>
    Public Datatable As DataTable

    ''' <summary>
    ''' Un Hashtable que contiene el par ID/TalonDocumento
    ''' con los objs TalonDocumento que se han impreso
    ''' </summary>
    ''' <remarks></remarks>
    Public TalonesImpresos As Hashtable


End Class
