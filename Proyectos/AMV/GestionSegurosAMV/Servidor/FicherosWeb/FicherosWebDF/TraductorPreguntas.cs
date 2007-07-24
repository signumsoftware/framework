using System;
using System.Collections.Generic;
using System.Text;

namespace FicherosWebAD
{
    public static class TraductorPreguntas
    {
        public static Pregunta? TraducirPregunta(int? codigoPregunta)
        {
            if (codigoPregunta.HasValue)
                return TraducirPregunta(codigoPregunta.Value);
            else return null;
        }

        public static Pregunta TraducirPregunta(int codigoPregunta)
        {
            switch (codigoPregunta)
            {
                case 241:
                case 242:
                case 243:
                case 244:
                case 245:
                case 246:
                case 247:
                case 248:
                case 249:
                case 250:
                case 251:
                case 252:
                case 262: return Pregunta.SeguroCancelado;

                case 183:
                case 184:
                case 185:
                case 186:
                case 187:
                case 188:
                case 189:
                case 190:
                case 191:
                case 192:
                case 193:
                case 194:
                case 257: return Pregunta.SiniestrosConResponsabilidad;

                case 195:
                case 196:
                case 197:
                case 198:
                case 199:
                case 200:
                case 201:
                case 202:
                case 203:
                case 204:
                case 205:
                case 206:
                case 258: return Pregunta.SiniestrosSinResponsabilidad;

                case 162:
                case 163:
                case 164:
                case 165:
                case 166:
                case 167:
                case 168:
                case 169:
                case 170:
                case 171:
                case 255: return Pregunta.DisponesPermisoCirculacionEspañol;

                case 182: return Pregunta.SeguroMotoSuperio124cm3;

                case 172:
                case 173:
                case 174:
                case 175:
                case 176:
                case 177:
                case 178:
                case 179:
                case 180:
                case 181:
                case 256: return Pregunta.TitularPermisoCirculacion;

                case 141:
                case 153:
                case 154:
                case 155:
                case 156:
                case 157:
                case 158:
                case 159:
                case 160:
                case 161:
                case 254: return Pregunta.UnicoConductorDelVehiculo;

                case 219:
                case 220:
                case 221:
                case 222:
                case 223:
                case 224:
                case 225:
                case 226:
                case 227:
                case 228:
                case 229:
                case 230:
                case 260: return Pregunta.InfraccionPorConducirEbrio;

                case 207:
                case 208:
                case 209:
                case 210:
                case 211:
                case 212:
                case 213:
                case 214:
                case 215:
                case 216:
                case 217:
                case 218:
                case 259: return Pregunta.InfraccionConRetiradaDeCarnet;

                case 231:
                case 232:
                case 233:
                case 234:
                case 235:
                case 236:
                case 237:
                case 238:
                case 239:
                case 240:
                case 261: return Pregunta.UtilizaVehiculoParaTransporteRemunerado;

                default:
                    throw new ArgumentException("No hay ninguna Pregunta mapeada para el valor " + codigoPregunta);
            }
        }
    }


    public enum Pregunta
    {
        SiniestrosConResponsabilidad = 1,
        SiniestrosSinResponsabilidad = 2,
        InfraccionConRetiradaDeCarnet = 3,
        InfraccionPorConducirEbrio = 4,
        UtilizaVehiculoParaTransporteRemunerado = 5,
        SeguroCancelado = 6,
        UnicoConductorDelVehiculo = 7,
        DisponesPermisoCirculacionEspañol = 8,
        TitularPermisoCirculacion = 9,
        SeguroMotoSuperio124cm3 = 10,
    }
}
