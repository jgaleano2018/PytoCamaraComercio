using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CC_Parser
{
    public class ParagraphExtracter
    {
        public ExtractResult parse_function(string fullOCR, string city)
        {
            //1
            string header;

            List<List<string[]>> wordList = new List<List<string[]>>();

            wordList.Add(new List<string[]>());
            wordList.Add(new List<string[]>());
            wordList.Add(new List<string[]>());


            if (city.Equals("Medellin"))
            {
                //Campo Facultad
                wordList[0].Add(new string[10] { "objeto social:", "limitaciones, prohibiciones, autorizaciones establecidas según los estatutos", "limitaciones, prohibiciones, autorizaciones establecidas segun los estatutos", "prohibición a la sociedad", "prohibiciones:", "prohibicions del gerente", "paragrafo", "parágrafo", "prohibiciones de la sociedad", "paragrafo - prohibiciones" });
                wordList[0].Add(new string[26] { "a favor", "obligación", "servir", "respaldar", "fiar", "garante", "obligaciones", "terceros", "avalar", "creditos", "titulos", "valor", "cauciones", "caución", "tercero", "garantía", "garantías", "garantizar", "causiones", "caucionar", "prohibido", "caución", "constituirse", "codeudora", "deudor", "solidario" });

                //Campo Monto
                wordList[1].Add(new string[15] { "funciones", "funciones y obligaciones del gerente:", "facultades y obligaciones del gerente", "atribuciones del gerente", "facultades del representante legal", "funciones y facultadesd el gerente", "funciones del gerente", "funciones:", "funciones y atribuciones del representante legal", "limitaciones, prohibiciones, autorizaciones estabelecidas según los estatutos", "parágrafo: prohibiciones.", "paragrafo 1°", "prohibición a los administradores", "limitaciones, prohibiciones, autorizaciones estabelecidas segun los estatutos", "limitaciones, prohibiciones y autorizaciones estabelecidas segun los estatutos" });
                wordList[1].Add(new string[17] { "celebrar", "contrato", "contratos", "tendrá", "restricciones", "cuantía", "restricción", "contraer", "salarios minimos legales", "smlmv", "pesos", "millon", "limite", "dólar", "ejecutar", "realizar", "celebrar" });

                //Campo Actividad
                wordList[2].Add(new string[1] { "objeto social" });
                wordList[2].Add(new string[4] { "lícita", "licita", "lícitos", "licitos" });
            }

            header = @"CAMARA DE COMERCIO DE MEDELLIN PARA ANTIOQUIA ILI\r\n";
            header = header + @"CERTIFICADO GENERADO A TRAVES DE LA PLATAFORMA VIRTUAL\r\n";
            header = header + @"LUGAR Y FECHA : .+\r\n";
            header = header + @"NUMERO DE RADICADO : .+\r\n";
            header = header + @"CODIGO DE VERIFICACION : .+\r\n";

            Regex rHeader = new Regex(header);
            Regex rBlock = new Regex("\\r\\n");

            fullOCR = rHeader.Replace(fullOCR, "");
            fullOCR = fullOCR.ToLower();
            fullOCR = fullOCR.Replace("certieica", "certifica");


            string[] docblocs = fullOCR.Split(new string[] { "certifica\r\n"}, StringSplitOptions.None);
            List<List<string>> research = new List<List<string>>();

            research.Add(new List<string>());
            research.Add(new List<string>());
            research.Add(new List<string>());
            //2

            foreach (string bloco in docblocs)
            {
                for (int field = 0; field < 3; field++)
                {
                    string bloco2 = rBlock.Replace(bloco, " ");

                    foreach (string trigger in wordList[field][0])
                    {
                        if (bloco2.Contains(trigger))
                        {
                            string[] units = bloco.Split(new string[] { ".\r\n" }, StringSplitOptions.None);

                            foreach (string unit in units)
                            {
                                foreach (string flag in wordList[field][1])
                                {
                                    if (unit.Contains(flag + " ") || unit.Contains(flag + ".") || unit.Contains(flag + ","))
                                    {
                                        research[field].Add(unit);
                                        break;
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
            }

            ExtractResult result = new ExtractResult();

            result.Facultad = research[0].ToArray();
            result.Monto = research[1].ToArray();
            result.Actividad = research[2].ToArray();

            return result;
        }
    }
}
