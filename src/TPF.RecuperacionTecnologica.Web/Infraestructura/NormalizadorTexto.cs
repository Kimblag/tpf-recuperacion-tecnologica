using System.Text;

namespace TPF.RecuperacionTecnologica.Web.Infraestructura;

public static class NormalizadorTexto
{
    public static string Normalizar(string texto)
    {
        var resultado = new StringBuilder(texto.Length);
        var espacioPendiente = false;

        foreach (var caracter in texto)
        {
            if (caracter is '​' or '‌' or '‍' or '﻿')
            {
                continue;
            }

            if (caracter is '\r' or '\n')
            {
                resultado.Append(caracter);
                espacioPendiente = false;
                continue;
            }

            if (char.IsWhiteSpace(caracter))
            {
                espacioPendiente = true;
                continue;
            }

            if (espacioPendiente && resultado.Length > 0 && resultado[^1] is not ('\r' or '\n'))
            {
                resultado.Append(' ');
            }

            espacioPendiente = false;
            resultado.Append(caracter);
        }

        return resultado.ToString().Trim();
    }
}
