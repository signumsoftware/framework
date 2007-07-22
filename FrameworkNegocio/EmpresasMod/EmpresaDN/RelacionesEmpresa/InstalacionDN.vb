#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

<Serializable()> _
Public Class InstalacionDN
    Inherits EntidadDN

    Private mEmpresaColaboradora As EntidadColaboradoraDN
    Private mSedeEmpresa As SedeEmpresaDN
    Private mColaboracion As ColaboracionDN

End Class
