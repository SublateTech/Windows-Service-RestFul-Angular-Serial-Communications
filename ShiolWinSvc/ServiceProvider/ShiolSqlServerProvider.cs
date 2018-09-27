using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AD;

namespace ShiolWinSvc
{
    class ShiolSqlServerProvider
    {
        private Decimal vPrecioVenta = 0;
        private String vcodigosTelefonicos = "";
        private Int32 vProductoId = 0;
        private Decimal vPrecioCosto = 0;
        private Int32 vMonedaID = 0;
        private Int32 vTiempoSegundos = 0;
        private Int32 vCollect = 0;
        private Int32 vFamiliaID = 0;
        private Int32 vAfectoIGV = 0;
        private Int32 vAfectoServicio = 0;

        private void GetShiolInfo(UniversalFrameProvider uFrameProvider)
        {
            vPrecioVenta = 0;
            vcodigosTelefonicos = "";
            vProductoId = 0;
            vPrecioCosto = 0;
            vMonedaID = 0;

            AD.ADTelefonos objTelefono = new AD.ADTelefonos();
            
            string primer_caracter_discado = uFrameProvider.DialedNumber.Left(1);
            string primeros_dos_caracteres_discados = uFrameProvider.DialedNumber.Left(1);
            
            DataTable telefonos;
            if (primer_caracter_discado == "0" && primer_caracter_discado.Trim().Length > 0)
                telefonos = objTelefono.telefonos(primeros_dos_caracteres_discados);
            else
                telefonos = objTelefono.telefonos(primer_caracter_discado);
 
            string vDiscado = uFrameProvider.DialedNumber;
            for (Int32 iX = 0; iX < vDiscado.Length + 1; iX++)
            {
                String discado_parcial = discado_parcial = vDiscado.Mid(0, vDiscado.Length - iX);
                foreach (DataRow filaTabla in telefonos.Rows)
                {
                    if (Convert.ToString(filaTabla["codigostelefonicos"]) == discado_parcial)
                    {
                        DataRow filaTel = filaTabla;
                        vPrecioVenta = Convert.ToDecimal(filaTel["PrecioVenta"]);
                        vcodigosTelefonicos = Convert.ToString(filaTel["codigosTelefonicos"]);

                        vProductoId = Convert.ToInt32(filaTel["ProductoId"]);
                        vPrecioCosto = Convert.ToDecimal(filaTel["preciocosto"]);
                        vMonedaID = Convert.ToInt32(filaTel["MonedaID"]);
                        vTiempoSegundos = Convert.ToInt32(filaTel["TiempoSegundos"]);
                        vCollect = Convert.ToInt32(filaTel["Collect"]);
                        vFamiliaID = Convert.ToInt32(filaTel["FamiliaID"]);
                        vAfectoIGV = Convert.ToInt32(filaTel["AfectoIGv"]);
                        vAfectoServicio = Convert.ToInt32(filaTel["afectoservicio"]);
                        break;
                    }
                }

            }
        }
        public void Save(ref UniversalFrameProvider uFrameProvider)
        {
            
            AD.Conexion.SqlConnectionString = ShiolConfiguration.Instance.Config.SqlServerConnection.ConnectionString;

            if (uFrameProvider.DialedNumber.ToString().Trim() == "")
                return;

            try
            {
                GetShiolInfo(uFrameProvider);
                
                /*String anexo, 
                 * string discado,
                 * DateTime fecha, 
                 * DateTime hora,
                 * string tiempo,
                 * string codigo,
                 * decimal precioventa, 
                 * decimal preciocosto,
                 * int productoId, 
                 * int monedaId, 
                 * int usuarioID, 
                 * int minutos, 
                 * string anexoregistro,
                 * decimal descuento,
                 * int tipo,
                 * int troncalid*/

                if (ShiolConfiguration.Instance.Config.SqlServerConnection.Connection)
                {
                    if (uFrameProvider.Duration < ShiolConfiguration.Instance.Config.MinCallDuration)
                    {
                        if (ShiolConfiguration.Instance.Config.SaveAllCalls)
                            uFrameProvider.Shiol = $"OK  {uFrameProvider.Duration} < {ShiolConfiguration.Instance.Config.MinCallDuration} config.";
                        else
                        {
                            uFrameProvider.Shiol = $"NO  {uFrameProvider.Duration} < {ShiolConfiguration.Instance.Config.MinCallDuration} config.";
                            return;
                        }
                    }
                    else
                        uFrameProvider.Shiol = "OK";
                }
                else
                {
                    uFrameProvider.Shiol = "Shiol disabled";
                    return;
                }
                
                AD.ADCentrales sqlObject = new ADCentrales();

                //objCentral.saveLlamada(vanexo, vDiscado, vfecha, vhora, vtiempo, 
                //vusuario, vPrecioVenta, vPrecioCosto, vProductoId, vMonedaID, 0, vMinutos, "", 0, vTipo, 0) == true)

                sqlObject.saveLlamada(uFrameProvider.Anexo,
                uFrameProvider.DialedNumber,
                uFrameProvider.Date,
                uFrameProvider.Time,
                UniversalFrameProvider.SecondsToDurationFormat(uFrameProvider.Duration), uFrameProvider.Anexo,
                vPrecioVenta, vPrecioCosto, vProductoId, vMonedaID,
                0,
                ShiolExtension.SecondsToMinutes(uFrameProvider.Duration),
                "", 0, uFrameProvider.Type, 0);

            }
            catch (Exception ex)
            {
                LogFile.saveRegistro("Shiol Sql Server Saving: " + ex.Message, levels.error);
                uFrameProvider.Shiol = "Sql Server Error";
            }

            return;
        }
    }
}
