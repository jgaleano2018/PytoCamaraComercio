using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Data;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Configuration;
using System.Xml;
using System.Data.Entity.Core.EntityClient;
using System.Data.Linq;
using CC_Parser.Properties;

namespace CC_Parser
{
    public class ParagraphExtracter
    {
        public ExtractResult parse_function(string fullOCR, string city, string totalPaginas, string connectionString)
        {

            #region Update connectionString of Data Base.
            
            DCCamaraComercioDataContext DCCamaraComercio = new DCCamaraComercioDataContext(connectionString);

            #endregion

            //1
            string header;

            List<List<string[]>> wordList = new List<List<string[]>>();
            List<List<WordSearch>> deliveryList = new List<List<WordSearch>>();
            List<HeadCity> listHeadCity = new List<HeadCity>();
            List<HeadCity> listHeaderDetail = new List<HeadCity>();
            List<List<CC_Parser.WordSearch>> listWordSearchGral = new List<List<CC_Parser.WordSearch>>();
            

            //In this block it's initialize the lists for load dictionary key words for fields contextuals:

            List<FieldsContextual> listFieldsContextuals = new List<FieldsContextual>();

            var queryFieldsContextuals = from dcfc in DCCamaraComercio.spConsultarCamposContextuales_CamaraComercio()
                                      select dcfc;

            foreach (var item in queryFieldsContextuals)
            {
                listFieldsContextuals.Add(new FieldsContextual { Consecutivo = item.CONSECUTIVO, Field = item.FIELD, TotalWordLeft = Convert.ToInt32(item.TOTAL_WORDS_LEFT), TotalWordRight = Convert.ToInt32(item.TOTAL_WORDS_RIGTH) });
            }

            for (int i = 0; i < listFieldsContextuals.Count(); i++)
            {
                wordList.Add(new List<string[]>());
                deliveryList.Add(new List<WordSearch>());
                listWordSearchGral.Add(new List<CC_Parser.WordSearch>());
            }

            //In this section it is consult the list of base key words from database to extraction of text required into fields: Facultad de Avalar, Monto y Actividad Licita:
            List<KeyWords> listKeyWords = new List<KeyWords>();
            List<List<KeyWords>> listKeyWordsPpal = new List<List<KeyWords>>();

            foreach (FieldsContextual itemFC in listFieldsContextuals)
            {
                listKeyWords = new List<KeyWords>();

                var queryListKeyWords = from dckw in DCCamaraComercio.spConsultarPalabrasClaves_CamaraComercio(itemFC.Consecutivo)
                                        select dckw;

                foreach (var item in queryListKeyWords)
                {
                    listKeyWords.Add(new KeyWords { Consecutivo = item.CONSECUTIVO, KeyWord = item.KEY_WORD });
                }

                listKeyWordsPpal.Add(listKeyWords);
            }

            //Load and fill array or list key words for future process in the workflow of Document "Camara de Comercio":
            string[] arrayFillWordList = new string[1];
            int contadorListKW = 0;
            int contadorItemsWL = 0;
            int poscArrayFWL = 0;

            if (listKeyWordsPpal.Count() > 0)
            {
                for (int i = 0; i < listFieldsContextuals.Count(); i++)
                {

                    arrayFillWordList = new string[0];
                    contadorListKW = 0;
                    foreach (List<KeyWords> listKeyWordFilter in listKeyWordsPpal)
                    {
                        if (contadorListKW == i)
                        {
                            
                            Array.Resize(ref arrayFillWordList, listKeyWordFilter.Count());

                            poscArrayFWL = 0;
                            foreach (KeyWords itemKeyWord in listKeyWordFilter)
                            {
                                arrayFillWordList[poscArrayFWL] = itemKeyWord.KeyWord;
                                poscArrayFWL++;
                            }
                        }
                        contadorListKW++;
                    }

                    if (arrayFillWordList.Count() > 0)
                    {
                        wordList[i].Add(arrayFillWordList);
                    }
                }
            }

            //wordList[0].Add(new string[10] { "objeto social:", "limitaciones, prohibiciones, autorizaciones establecidas según los estatutos", "limitaciones, prohibiciones, autorizaciones establecidas segun los estatutos", "prohibición a la sociedad", "prohibiciones:", "prohibicions del gerente", "paragrafo", "parágrafo", "prohibiciones de la sociedad", "paragrafo - prohibiciones" });
            //wordList[0].Add(new string[26] { "a favor", "obligación", "servir", "respaldar", "fiar", "garante", "obligaciones", "terceros", "avalar", "creditos", "titulos", "valor", "cauciones", "caución", "tercero", "garantía", "garantías", "garantizar", "causiones", "caucionar", "prohibido", "caución", "constituirse", "codeudora", "deudor", "solidario" });

            //wordList[1].Add(new string[15] { "funciones", "funciones y obligaciones del gerente:", "facultades y obligaciones del gerente", "atribuciones del gerente", "facultades del representante legal", "funciones y facultadesd el gerente", "funciones del gerente", "funciones:", "funciones y atribuciones del representante legal", "limitaciones, prohibiciones, autorizaciones estabelecidas según los estatutos", "parágrafo: prohibiciones.", "paragrafo 1°", "prohibición a los administradores", "limitaciones, prohibiciones, autorizaciones estabelecidas segun los estatutos", "limitaciones, prohibiciones y autorizaciones estabelecidas segun los estatutos" });
            //wordList[1].Add(new string[17] { "celebrar", "contrato", "contratos", "tendrá", "restricciones", "cuantía", "restricción", "contraer", "salarios minimos legales", "smlmv", "pesos", "millon", "limite", "dólar", "ejecutar", "realizar", "celebrar" });

            //wordList[2].Add(new string[1] { "objeto social" });
            //wordList[2].Add(new string[4] { "lícita", "licita", "lícitos", "licitos" });

            //Facultad de Avalar
            //wordList[0].Add(new string[34] { "a favor de", "afianzar", "aval", "avaladora", "avalar", "avalista", "caucionar", "codeudora", "constituir", "constituirse", "contrato", "deudas", "deudor", "fiador", "fianza", "fiar", "garante", "garantía", "garantías", "garantizar", "obligación", "obligaciones", "pagares", "podrá", "respaldar", "solidario", "terceras", "tercero", "terceros", "titulares", "titulo valor", "títulos", "titulos valores", "valores" });

            //Monto
            //wordList[1].Add(new string[32] { "acto", "actos", "contratos", "credito", "cuantia", "dólar", "dolares", "exceda", "ilimitada", "legales", "limitación", "limitaciones", "limite", "limites", "mensuales", "millon", "millones", "minimos", "monto", "no tendra restricciones", "obligaciones", "pesos", "restriccion", "restricciones", "s.m.l.v", "salarios", "sin limite", "smlmv", "smmlv", "suma", "valor", "valores" });
            //wordList[1].Add(new string[29] { "credito", "cuantia", "dólar", "dolares", "exceda", "ilimitada", "legales", "limitación", "limitaciones", "limite", "limites", "mensuales", "millon", "millones", "minimos", "monto", "no tendra restricciones", "obligaciones", "pesos", "restriccion", "restricciones", "s.m.l.v", "salarios", "sin limite", "smlmv", "smmlv", "suma", "valor", "valores" });

            //Actividad
            //wordList[2].Add(new string[14] { "actividad", "actividades", "acto", "civil", "civiles", "comercial", "comerciales", "comercio", "económica", "ley 1258", "licita", "licitas", "licito", "licitos" });

            listHeadCity.Add(new HeadCity { City = "Medellin", SizeWord = 62 });
            listHeadCity.Add(new HeadCity { City = "Bogota", SizeWord = 65 });
            listHeadCity.Add(new HeadCity { City = "Cali", SizeWord = 60 });

            int poscStartWrd = 0;
            int poscEndWrd = 0;
            int poscStartWrd2 = 0;
            int poscEndWrd2 = 0;
            int typeHead = 0;
            int sizeFullOCR = 0;
            int counterWord = 0;

            double porcentajeConfianzaFA = 0;
            double porcentajeConfianzaM = 0;
            double porcentajeConfianzaA = 0;

            string strLeftSide = "";
            string strRigthSide = "";
            string fullOCRProcess = "";
            string[] charSep = new string[] { ". ", ",", ";", ":", " ", "\r\n", "certifica" };
            string[] arrayWords;
            string[] arrayWordsAux = null;
            string[] arrayWordsAux2 = null;

            List<WordSearch> listWordRights = new List<WordSearch>();
            List<WordSearch> listWordLefts = new List<WordSearch>();

            //List<CC_Parser.WordSearch> listWordFacultadAvalar = new List<CC_Parser.WordSearch>();
            //List<CC_Parser.WordSearch> listWordMonto = new List<CC_Parser.WordSearch>();
            //List<CC_Parser.WordSearch> listWordActividad = new List<CC_Parser.WordSearch>();

            List<HeaderCompany> listHeaderCompanies = new List<HeaderCompany>();
            List<HeaderCompany> listHeaderCompaniesProcess = new List<HeaderCompany>();
            List<CamaraComercio> listCamaraComercio = new List<CamaraComercio>();
            List<CamaraComercio> listNewCamaraComercio = new List<CamaraComercio>();

            bool bolEnableSubsHeader = false, bolIterateDocument = false;
            //DCCamaraComercioDataContext DCCamaraComercio = new DCCamaraComercioDataContext();


            //Get list of diferents sucursals of chamber of commerce and the goals it's to filter the sucursal of the document about execute at this momment:

            var queryCamaraComercio = from dccc in DCCamaraComercio.spConsultarLista_CamaraComercio()
                                      select dccc;

            foreach (var item in queryCamaraComercio)
            {
                listCamaraComercio.Add(new CamaraComercio { Consecutivo = item.CONSECUTIVO, Nombre = item.NOMBRE, CaptureCity = item.CAPTURE_CITY });
            }

            fullOCR = fullOCR.ToLower();
            fullOCR = fullOCR.Replace("certieica", "certifica");
            fullOCR = ChangeFullOCRFirst(fullOCR);

            fullOCRProcess = fullOCR;
            fullOCRProcess = ChangeFullOCR(fullOCRProcess);

            fullOCR = fullOCR.Replace("abua", "aburra");
            fullOCRProcess = fullOCRProcess.Replace("abua", "aburra");
            

            //

            List<CC_Parser.WordSearch> listWordSearchBase = new List<CC_Parser.WordSearch>();

            for (int i = 0; i < listFieldsContextuals.Count(); i++)
            {
                listWordSearchBase = new List<CC_Parser.WordSearch>();

                foreach (string keyWord in wordList[i][0])
                {
                    listWordSearchBase.Add(new WordSearch { Word = keyWord, Word2 = keyWord });
                }

                listWordSearchGral[i] = listWordSearchBase;
            }


            try
            {
                //Take the Full OCR and replace the words with it structure is similar to the list of key words this is it:

                string[] arrayFullOCR = fullOCR.Split(' ');

                for (int i = 0; i < listFieldsContextuals.Count(); i++)
                {
                    setReplaceParagrapher(ref fullOCR, arrayFullOCR, wordList, i);
                }

                #region Encabezado de Camara de Comercio x Ciudad

                /*CAMARA DE COMERCIO DE MEDELLIN PARA ANTIOQUIA
    Certificado generado a trav ^ s de la plataforma virtual A
      Lugar y fecha : Medellin, 2017 / 03 / 17 Hora : 14 : 53 CAMARADE COMERCIO ®
    N^mero de radicado : 0015010375 - SISSBA Pagina : 1 DE MEDELLIN PARA ANT1OQUIA
    C6digo de verificaci6n : liaVxzUhbkadfAjR Copia : 1 de 1*/

                string stringHeader = "";
                double distHeader = 0;
                double fuzzyMatch = 0;
                double[] fuzzyMatchResult = new double[1];
                double lengthFM = 0;
                int length = 0;
                string strCamaraComercioActive = "";
                int positionHeader = 0;
                string[] arrayHeader = null;

                //Se busca cual es la ciudad del documento de la camara de comercio procesado:
                var queryHeaderCompanies = from dcch in DCCamaraComercio.spConsultarEncabezadosGeneral_CamaraComercio()
                                           select dcch;

                foreach (var item in queryHeaderCompanies)
                {
                    listHeaderCompanies.Add(new HeaderCompany { City = item.CIUDAD, DescriptionHeader = item.DESCRIPCION });
                }

                IEnumerable<HeaderCompany> queryHeaders =
                from hc in listHeaderCompanies
                select hc;

                listHeaderCompaniesProcess = queryHeaders.ToList();

                bool enableDropHeader = false;

                if (listHeaderCompaniesProcess.Count() > 0)
                {
                    foreach (HeaderCompany itemHeader in listHeaderCompaniesProcess)
                    {
                        setReplaceHeaderFullOCR(ref fullOCR, ref fullOCRProcess, ref enableDropHeader, itemHeader, arrayFullOCR, ref arrayHeader, 1);
                    }
                }

                //Se busca cual es la ciudad del documento de la camara de comercio procesado:
                foreach (CamaraComercio item in listCamaraComercio)
                {
                    item.CaptureCity = item.CaptureCity + "|" + item.Nombre;

                    string[] arrayCaptureCity = item.CaptureCity.Split('|');
                    bool bolFoundCity = false;

                    foreach (string itemCC in arrayCaptureCity)
                    {
                        if (fullOCRProcess.IndexOf(itemCC.ToLower(), 0) > -1)
                        {
                            bolFoundCity = true;
                            break;
                        }
                    }

                    if (bolFoundCity)
                    {
                        strCamaraComercioActive = item.Nombre;
                        break;
                    }
                }
                
                if (strCamaraComercioActive == "ABURRA")
                {
                    strCamaraComercioActive = "ABURRA SUR";
                }

                if (strCamaraComercioActive == "BUENAVENTURA")
                {
                    fullOCR = fullOCRProcess;
                }
                
                listHeaderCompanies = new List<HeaderCompany>();


                //Consult in the database the list of header of diferents sucursals of chamber of commerce for to begin the excluction of the header of this document:

                var queryHeaderCompanies2 = from dcch in DCCamaraComercio.spConsultarEncabezados_CamaraComercio(strCamaraComercioActive)
                                            select dcch;

                foreach (var item in queryHeaderCompanies2)
                {
                    listHeaderCompanies.Add(new HeaderCompany { DescriptionHeader = item.DESCRIPCION });
                }


                #region Delete header for each page of document

                queryHeaders = from hc in listHeaderCompanies
                               select hc;

                listHeaderCompaniesProcess = queryHeaders.ToList();

                enableDropHeader = false;

                if (listHeaderCompaniesProcess.Count() > 0)
                {
                    foreach (HeaderCompany itemHeader in listHeaderCompaniesProcess)
                    {////99999
                        int contadorFuzzyM = 0;
                        int contadorVirgulilla = 0;
                        string lineEvalExec = "";
                        arrayHeader = null;
                        string[] arrayVirgulilla = null;
                        string[] arrayVirgulillaAux = null;
                        string[] arrayVirgulillaEnd = null;

                        //enableContinue = false;

                        setReplaceHeaderFullOCR(ref fullOCR, ref fullOCRProcess, ref enableDropHeader, itemHeader, arrayFullOCR, ref arrayHeader, 2);

                        positionHeader++;

                        //It's test after have been find it the header on the page, that the result is about rounder between 90 to 100 porcent.
                       
                        int poscAuxiliar = 0;
                        int incPagina = 0;
                        int idxArrayWords = 0;
                        int lengthAux = 0;
                        string wordFindHead = "";
                        string headquarter = "";
                        string strLeftSideAux = "";
                        string strRigthSideAux = "";
                        bool enableReadVirg = false;
                        bool enableExtracHeaderBog = false;

                        //En este caso se evalua si el documento de la camara de comercio es de Bogotá y el formato del encabezado inicia con camara de comercio de bogota:
                        if (strCamaraComercioActive == "BOGOTA" && arrayHeader[0].IndexOf("camara de comercio de bogota", 0) > -1)
                        {
                            arrayVirgulilla = null;
                            poscStartWrd = 0;
                            contadorFuzzyM = 0;
                            bolIterateDocument = false;

                            foreach (string lineEval in arrayHeader)
                            {
                                lineEvalExec = lineEval;
                                lineEvalExec = lineEvalExec.Replace("[", "");
                                lineEvalExec = lineEvalExec.Replace("]", "");

                                if (lineEvalExec.IndexOf("~", 0) >= 0)
                                {
                                    arrayVirgulilla = lineEvalExec.Split('~');
                                }
                            }

                            foreach (string lineEval in arrayVirgulilla)
                            {
                                enableReadVirg = false;
                                if ((fullOCR.IndexOf(arrayHeader[0], 0) > -1 && fullOCR.IndexOf(lineEval, fullOCR.IndexOf(arrayHeader[0], 0) + 1) > -1) || (fullOCR.IndexOf("camara de comercio", 0) > -1 && fullOCR.IndexOf(lineEval, fullOCR.IndexOf(arrayHeader[0], 0) + 1) > -1))
                                {
                                    enableReadVirg = true;
                                }

                                if (enableReadVirg)
                                {
                                    if (fullOCR.IndexOf("camara de comercio", 0) < fullOCR.IndexOf(arrayHeader[0], 0) && poscStartWrd == 0)
                                    {
                                        wordFindHead = "camara de comercio";
                                    }
                                    else
                                    {
                                        wordFindHead = arrayHeader[0];
                                    }

                                    headquarter = lineEval;
                                    length = stringHeader.Length;
                                    poscStartWrd = 0;
                                    contadorFuzzyM = 0;

                                    while (!bolIterateDocument)
                                    {
                                        arrayWords = null;
                                        strLeftSide = "";
                                        strRigthSide = "";
                                        bool continueExtracPage = false;


                                        if (fullOCR.IndexOf("camara de comercio", 0) == fullOCR.IndexOf(arrayHeader[0], 0) && poscStartWrd > 0)
                                        {
                                            wordFindHead = arrayHeader[0];
                                        }

                                        while (!continueExtracPage)
                                        {
                                            int poscheadquarter = fullOCRProcess.IndexOf(lineEval, poscStartWrd);
                                            poscStartWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd);

                                            if ((poscheadquarter - poscStartWrd) < 100)
                                            {
                                                continueExtracPage = true;
                                            }
                                            else
                                            {
                                                poscStartWrd++;
                                            }
                                        }

                                        enableExtracHeaderBog = false;
                                        if (arrayHeader[arrayHeader.Count() - 1].IndexOf("pagina :", 0) >= 0) //pagina : 1    pagina : 1 de 1
                                        {
                                            poscStartWrd2 = poscStartWrd;
                                            if (poscStartWrd2 > -1)
                                            {
                                                arrayWords = new string[3];
                                                wordFindHead = "pagina :";   //pagina : 1 de 10

                                                arrayWords = setArrayWords(arrayWords, fullOCR, 3, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[0]);
                                                arrayWords = setArrayWords(arrayWords, fullOCR, 3, 2, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[1]);
                                                arrayWords = setArrayWords(arrayWords, fullOCR, 3, 3, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[2]);

                                                arrayWordsAux = arrayHeader[arrayHeader.Count() - 1].Split(' ');

                                                if (arrayWordsAux.Count() == 5)
                                                {
                                                    if (!string.IsNullOrEmpty(arrayWords[0]) && !string.IsNullOrEmpty(arrayWords[1]) && !string.IsNullOrEmpty(arrayWords[2]))
                                                    {
                                                        if (EsNumerico(arrayWords[0]) && arrayWords[1] == "de" && EsNumerico(arrayWords[2]))
                                                        {
                                                            enableExtracHeaderBog = true;
                                                        }
                                                    }
                                                }
                                                if (enableExtracHeaderBog)
                                                {
                                                    lengthAux = 0;
                                                    for (int i = 0; i < arrayWords.Count(); i++)
                                                    {
                                                        if (!string.IsNullOrEmpty(arrayWords[i]))
                                                        {
                                                            lengthAux += arrayWords[i].Length;
                                                            idxArrayWords++;
                                                        }
                                                    }

                                                    if (arrayWords.Count() == idxArrayWords)
                                                    {
                                                        lengthAux += 11;
                                                        poscEndWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2) + lengthAux;
                                                    }
                                                    idxArrayWords = 0;
                                                    poscStartWrd2 = poscEndWrd + 1;
                                                }
                                                else
                                                {
                                                    arrayWords = new string[1];
                                                    wordFindHead = "pagina : ";         //pagina : 1 | pagina : 2 | pagina : 3

                                                    arrayWords = setArrayWords(arrayWords, fullOCR, 7, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                    bolEnableSubsHeader = true;

                                                    arrayWordsAux = arrayHeader[arrayHeader.Count() - 1].Split(' ');

                                                    if (arrayWordsAux.Count() == 3 && EsNumerico(arrayWords[0]))
                                                    {
                                                        if (!string.IsNullOrEmpty(arrayWords[0]))
                                                        {
                                                            poscEndWrd = poscAuxiliar;
                                                        }
                                                    }
                                                    idxArrayWords = 0;
                                                }
                                            }
                                            else
                                            {
                                                poscStartWrd2 = -1;
                                                poscEndWrd2 = -1;
                                            }
                                        }


                                        if (poscStartWrd >= 0 && poscEndWrd > 0)
                                        {
                                            sizeFullOCR = fullOCR.Length;
                                            if (poscStartWrd == 0)
                                            {
                                                strLeftSideAux = "";

                                                if (poscEndWrd - poscStartWrd < length)
                                                {
                                                    poscEndWrd = poscEndWrd + (length - (poscEndWrd - poscStartWrd));
                                                }
                                                strRigthSide = fullOCR.Substring(poscEndWrd, sizeFullOCR - poscEndWrd);
                                            }
                                            else
                                            {
                                                strLeftSide = fullOCR.Substring(0, poscStartWrd);
                                                if (poscEndWrd - poscStartWrd < length)
                                                {
                                                    poscEndWrd = poscEndWrd + (length - (poscEndWrd - poscStartWrd));
                                                }
                                                strRigthSide = fullOCR.Substring(poscEndWrd, sizeFullOCR - poscEndWrd);
                                            }

                                            if (!string.IsNullOrEmpty(strRigthSide))
                                            {
                                                if (poscStartWrd == 0)
                                                {
                                                    fullOCR = strRigthSide;
                                                }
                                                else
                                                {
                                                    fullOCR = string.Format("{0}{1}", strLeftSide, strRigthSide);
                                                }
                                                fullOCRProcess = fullOCR;
                                                fullOCRProcess = ChangeFullOCR(fullOCRProcess);
                                            }

                                            strLeftSide = arrayHeader[0];
                                            strRigthSide = arrayHeader[arrayHeader.Count() - 1];

                                            poscStartWrd = poscEndWrd + 1;
                                            poscEndWrd = 0;
                                        }
                                        else
                                        {
                                            bolIterateDocument = true;
                                        }
                                    }

                                }
                            }

                            string varresultok = "Is very good!!!!";
                        }
                        else
                        {
                            //  Se extrae cualquier documento distinto a la regional Bogota:
                            if (arrayHeader.Count() > 0)
                            {
                                strLeftSide = arrayHeader[0];
                                strRigthSide = arrayHeader[arrayHeader.Count() - 1];
                            }

                            length = stringHeader.Length;
                            poscStartWrd = 0;
                            contadorFuzzyM = 0;
                            bolIterateDocument = false;
                            poscStartWrd2 = 0;

                            int poscStartWrd3 = 0;
                            string parameterLegend = "";
                            bool processMethodSingle = false;
                            bool enableSetEnd = false;

                            while (!bolIterateDocument)
                            {
                                enableReadVirg = false;
                                enableExtracHeaderBog = false;
                                strLeftSideAux = "";

                                while (!enableReadVirg)
                                {
                                    if (strCamaraComercioActive == "BOGOTA")
                                    {
                                        if (strCamaraComercioActive == "BOGOTA" && arrayHeader[0].IndexOf("certificado", 0) > -1)
                                        {
                                            poscAuxiliar = fullOCRProcess.IndexOf("certificado", poscStartWrd);   // + strRigthSide.Length;
                                            if (poscAuxiliar >= 0)
                                            {
                                                if (fullOCR.Length - poscAuxiliar > 200)
                                                {
                                                    strLeftSideAux = fullOCRProcess.Substring(poscAuxiliar, 200);

                                                    if (!string.IsNullOrEmpty(strLeftSideAux))
                                                    {
                                                        if (strLeftSideAux.IndexOf("camara de comercio de bogota", 0) > -1 && strLeftSideAux.IndexOf("fecha :", 0) > -1 && strLeftSideAux.IndexOf("hora :", 0) > -1 && strLeftSideAux.IndexOf("codigo de verificacion :", 0) > -1 && strLeftSideAux.IndexOf("operacion :", 0) > -1 && strLeftSideAux.IndexOf("pagina :", 0) > -1)
                                                        {
                                                            if (strLeftSideAux.IndexOf("camara de comercio de bogota", 0) < strLeftSideAux.IndexOf("fecha :", 0) && strLeftSideAux.IndexOf("fecha :", 0) < strLeftSideAux.IndexOf("hora :", 0) && strLeftSideAux.IndexOf("hora :", 0) < strLeftSideAux.IndexOf("codigo de verificacion :", 0) && strLeftSideAux.IndexOf("codigo de verificacion :", 0) < strLeftSideAux.IndexOf("pagina :", 0))
                                                            {
                                                                enableReadVirg = true;
                                                                enableExtracHeaderBog = true;
                                                                processMethodSingle = true;
                                                                poscStartWrd = poscAuxiliar;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            poscStartWrd = poscAuxiliar + 1;
                                        }
                                        else
                                        {
                                            enableReadVirg = true;
                                            enableExtracHeaderBog = true;
                                            processMethodSingle = true;
                                        }

                                        if (poscAuxiliar == -1)
                                        {
                                            bolIterateDocument = true;
                                            enableExtracHeaderBog = false;
                                            enableReadVirg = true;
                                            poscStartWrd = -1;
                                            poscEndWrd = -1;
                                        }
                                    }
                                    else
                                    {
                                        if (strCamaraComercioActive == "BUENAVENTURA" && arrayHeader[0].IndexOf("camara de", 0) > -1 && poscStartWrd == poscStartWrd3)
                                        {
                                            poscAuxiliar = fullOCRProcess.IndexOf(arrayHeader[0], poscStartWrd);

                                            if (poscAuxiliar >= 0)
                                            {
                                                if (fullOCR.Length - poscAuxiliar > 550)
                                                {
                                                    strLeftSideAux = fullOCRProcess.Substring(poscAuxiliar, 550);

                                                    if (!string.IsNullOrEmpty(strLeftSideAux))
                                                    {
                                                        if (strLeftSideAux.IndexOf("camara de", 0) > -1 && strLeftSideAux.IndexOf("comercio codigo de verificacion :", 0) > -1 && strLeftSideAux.IndexOf("numero de radicacion :", 0) > -1 && strLeftSideAux.IndexOf("fecha de impresion :", 0) > -1 && strLeftSideAux.IndexOf("paginas :", 0) > -1 && strLeftSideAux.IndexOf("la matricula mercantil proporciona seguridad y confianza en los negocios. renueve su matricula a mas tardar el 31 de marzo y evite sanciones de hasta 17 s.m.l.m.v. republica de colombia certificado de existencia y representacion el suscrito secretario de la camara de comercio de buenaventura", 0) > -1)
                                                        {
                                                            if (strLeftSideAux.IndexOf("camara de", 0) < strLeftSideAux.IndexOf("comercio codigo de verificacion :", 0) && strLeftSideAux.IndexOf("comercio codigo de verificacion :", 0) < strLeftSideAux.IndexOf("numero de radicacion :", 0) && strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("fecha de impresion :", 0) < strLeftSideAux.IndexOf("paginas :", 0) && strLeftSideAux.IndexOf("paginas :", 0) < strLeftSideAux.IndexOf("la matricula mercantil proporciona seguridad y confianza en los negocios. renueve su matricula a mas tardar el 31 de marzo y evite sanciones de hasta 17 s.m.l.m.v. republica de colombia certificado de existencia y representacion el suscrito secretario de la camara de comercio de buenaventura", 0))
                                                            {
                                                                enableReadVirg = true;
                                                                enableExtracHeaderBog = true;
                                                                processMethodSingle = true;
                                                                poscStartWrd = poscAuxiliar;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //camara de ' comercio codigo de verificacion : 06164ihmt0 numero de radicacion : 20160042649 - pri fecha de impresion : 23 agosto 2016 10 : 18 am buenaventura paginas : 6 - 6 dado en buenaventura a los 23 dias del mes de agosto del aso 2016 hora : 10 : 18 : 35 am el secretario
                                                            if (strLeftSideAux.IndexOf("camara de", 0) > -1 && strLeftSideAux.IndexOf("comercio codigo de verificacion :", 0) > -1 && strLeftSideAux.IndexOf("numero de radicacion :", 0) > -1 && strLeftSideAux.IndexOf("fecha de impresion :", 0) > -1 && strLeftSideAux.IndexOf("paginas :", 0) > -1)
                                                            {
                                                                if (strLeftSideAux.IndexOf("camara de", 0) < strLeftSideAux.IndexOf("comercio codigo de verificacion :", 0) && strLeftSideAux.IndexOf("comercio codigo de verificacion :", 0) < strLeftSideAux.IndexOf("numero de radicacion :", 0) && strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("fecha de impresion :", 0) < strLeftSideAux.IndexOf("paginas :", 0))
                                                                {
                                                                    enableReadVirg = true;
                                                                    enableExtracHeaderBog = true;
                                                                    processMethodSingle = true;
                                                                    poscStartWrd = poscAuxiliar;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            poscStartWrd = poscAuxiliar + 1;

                                            if (poscStartWrd > 0)
                                            {
                                                poscStartWrd3 = poscStartWrd;
                                            }
                                        }
                                        else
                                        {
                                            if ((strCamaraComercioActive == "BUGA" || strCamaraComercioActive == "CALI") && arrayHeader[0].IndexOf("codigo de verificacion :", 0) > -1 && poscStartWrd == poscStartWrd3)
                                            {

                                                poscAuxiliar = fullOCRProcess.IndexOf(arrayHeader[0], poscStartWrd);

                                                if (poscAuxiliar >= 0)
                                                {
                                                    if (fullOCR.Length - poscAuxiliar > 550)
                                                    {
                                                        strLeftSideAux = fullOCRProcess.Substring(poscAuxiliar, 550);

                                                        if (!string.IsNullOrEmpty(strLeftSideAux))
                                                        {
                                                            if (strCamaraComercioActive == "BUGA")
                                                            {
                                                                parameterLegend = strRigthSide; //"la matricula mercantil proporciona seguridad y confianza en los negocios. renueve su matricula a mas tardar el 31 de marzo y evite sanciones de hasta 17 s.m.l.m.v. republica de colombia certificado de existencia y representacion el suscrito secretario de la camara de comercio de buga";
                                                            }
                                                            else
                                                            {
                                                                if (strCamaraComercioActive == "CALI")
                                                                {
                                                                    parameterLegend = strRigthSide;//"la matricula mercantil proporciona seguridad y confianza en los negocios. renueve su matricula mercantil a mas tardar el 31 de marzo y evite sanciones de hasta 17 s.m.l.m.v. republica de colombia certificado de existencia y representacion el suscrito secretario de la camara de comercio de cali";
                                                                }
                                                            }

                                                            if (strLeftSideAux.IndexOf("numero de radicacion :", 0) > -1 && strLeftSideAux.IndexOf("fecha de impresion :", 0) > -1 && strLeftSideAux.IndexOf("paginas :", 0) > -1 && strLeftSideAux.IndexOf(parameterLegend, 0) > -1 && strRigthSide.IndexOf("paginas : ") == -1)
                                                            {
                                                                if (strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("fecha de impresion :", 0) < strLeftSideAux.IndexOf("paginas :", 0) && strLeftSideAux.IndexOf("paginas :", 0) < strLeftSideAux.IndexOf(parameterLegend, 0))
                                                                {
                                                                    enableReadVirg = true;
                                                                    enableExtracHeaderBog = true;
                                                                    processMethodSingle = true;
                                                                    poscStartWrd = poscAuxiliar;
                                                                    enableSetEnd = true;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (strLeftSideAux.IndexOf("numero de radicacion :", 0) > -1 && strLeftSideAux.IndexOf("fecha de impresion :", 0) > -1 && strLeftSideAux.IndexOf("paginas :", 0) > -1)
                                                                {
                                                                    if (strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("fecha de impresion :", 0) < strLeftSideAux.IndexOf("paginas :", 0))
                                                                    {
                                                                        enableReadVirg = true;
                                                                        enableExtracHeaderBog = true;
                                                                        processMethodSingle = true;
                                                                        poscStartWrd = poscAuxiliar;
                                                                        enableSetEnd = false;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                poscStartWrd = poscAuxiliar + 1;

                                                if (poscStartWrd > 0)
                                                {
                                                    poscStartWrd3 = poscStartWrd;
                                                }
                                            }
                                            else
                                            {
                                                if (strCamaraComercioActive == "CARTAGENA" && arrayHeader[0].IndexOf("camara de comercio de cartagena", 0) > -1 && poscStartWrd == poscStartWrd3)
                                                {
                                                    poscAuxiliar = fullOCRProcess.IndexOf(arrayHeader[0], poscStartWrd);

                                                    if (poscAuxiliar >= 0)
                                                    {
                                                        if (fullOCR.Length - poscAuxiliar > 550)
                                                        {
                                                            strLeftSideAux = fullOCRProcess.Substring(poscAuxiliar, 550);

                                                            if (!string.IsNullOrEmpty(strLeftSideAux))
                                                            {
                                                                if (strLeftSideAux.IndexOf("camara de comercio de cartagena", 0) > -1 && strLeftSideAux.IndexOf("certificado generado a traves de taquillas", 0) > -1 && strLeftSideAux.IndexOf("lugar y fecha :", 0) > -1 && strLeftSideAux.IndexOf("hora :", 0) > -1 && strLeftSideAux.IndexOf("numero de radicado :", 0) > -1 && strLeftSideAux.IndexOf("pagina :", 0) > -1 && strLeftSideAux.IndexOf("codigo de verificacion :", 0) > -1 && strLeftSideAux.IndexOf("copia :", 0) > -1)
                                                                {
                                                                    if (strLeftSideAux.IndexOf("camara de comercio de cartagena", 0) < strLeftSideAux.IndexOf("certificado generado a traves de taquillas", 0) && strLeftSideAux.IndexOf("certificado generado a traves de taquillas", 0) < strLeftSideAux.IndexOf("lugar y fecha :", 0) && strLeftSideAux.IndexOf("lugar y fecha :", 0) < strLeftSideAux.IndexOf("hora :", 0) && strLeftSideAux.IndexOf("hora :", 0) < strLeftSideAux.IndexOf("numero de radicado :", 0) && strLeftSideAux.IndexOf("numero de radicado :", 0) < strLeftSideAux.IndexOf("pagina :", 0) && strLeftSideAux.IndexOf("pagina :", 0) < strLeftSideAux.IndexOf("codigo de verificacion :", 0) && strLeftSideAux.IndexOf("codigo de verificacion :", 0) < strLeftSideAux.IndexOf("copia :", 0))
                                                                    {
                                                                        enableReadVirg = true;
                                                                        enableExtracHeaderBog = true;
                                                                        processMethodSingle = true;
                                                                        poscStartWrd = poscAuxiliar;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }

                                                    poscStartWrd = poscAuxiliar + 1;

                                                    if (poscStartWrd > 0)
                                                    {
                                                        poscStartWrd3 = poscStartWrd;
                                                    }
                                                }
                                                else
                                                {
                                                    if (strCamaraComercioActive == "IBAGUE" && arrayHeader[0].IndexOf("camara de comercio de ibague", 0) > -1 && poscStartWrd == poscStartWrd3)
                                                    {
                                                        poscAuxiliar = fullOCRProcess.IndexOf(arrayHeader[0], poscStartWrd);

                                                        if (poscAuxiliar >= 0)
                                                        {
                                                            if (fullOCR.Length - poscAuxiliar > 550)
                                                            {
                                                                strLeftSideAux = fullOCRProcess.Substring(poscAuxiliar, 550);

                                                                if (!string.IsNullOrEmpty(strLeftSideAux))
                                                                {
                                                                    if (strLeftSideAux.IndexOf("camara de comercio de ibague", 0) > -1 && strLeftSideAux.IndexOf("certificado expedido a traves del portal de servicios virtuales ( sii ) certificado de existencia y representacion legal", 0) > -1)
                                                                    {
                                                                        if (strLeftSideAux.IndexOf("camara de comercio de ibague", 0) < strLeftSideAux.IndexOf("certificado expedido a traves del portal de servicios virtuales ( sii ) certificado de existencia y representacion legal", 0))
                                                                        {
                                                                            enableReadVirg = true;
                                                                            enableExtracHeaderBog = true;
                                                                            processMethodSingle = true;
                                                                            poscStartWrd = poscAuxiliar;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        poscStartWrd = poscAuxiliar + 1;

                                                        if (poscStartWrd > 0)
                                                        {
                                                            poscStartWrd3 = poscStartWrd;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if ((strCamaraComercioActive == "PALMIRA" || strCamaraComercioActive == "TULUA") && arrayHeader[0].IndexOf("codigo de verificacion :", 0) > -1 && poscStartWrd == poscStartWrd3)
                                                        {
                                                            poscAuxiliar = fullOCRProcess.IndexOf(arrayHeader[0], poscStartWrd);

                                                            if (poscAuxiliar >= 0)
                                                            {
                                                                if (fullOCR.Length - poscAuxiliar > 550)
                                                                {
                                                                    strLeftSideAux = fullOCRProcess.Substring(poscAuxiliar, 550);

                                                                    if (!string.IsNullOrEmpty(strLeftSideAux))
                                                                    {
                                                                        if (strLeftSideAux.IndexOf("codigo de verificacion :", 0) > -1 && strLeftSideAux.IndexOf("numero de radicacion :", 0) > -1 && strLeftSideAux.IndexOf("fecha de impresion :", 0) > -1 && strLeftSideAux.IndexOf("paginas :", 0) > -1)
                                                                        {
                                                                            if (strCamaraComercioActive == "PALMIRA")
                                                                            {
                                                                                parameterLegend = "la matricula mercantil proporciona seguridad y confianza en los negocios. renueve su matricula a mas tardar el 31 de marzo y evite sanciones de hasta 17 s.m.l.m.v. republica de colombia certificado de existencia y representacion el suscrito secretario de la camara de comercio de palmira";
                                                                            }
                                                                            else
                                                                            {
                                                                                parameterLegend = "la matricula mercantil proporciona seguridad y confianza en los negocios. renueve su matricula a mas tardar el 31 de marzo y evite sanciones de hasta 17 s.m.l.m.v. republica de colombia certificado de existencia y representacion el suscrito secretario de la camara de comercio de tulua";
                                                                            }

                                                                            if (strLeftSideAux.IndexOf(parameterLegend, 0) > -1)
                                                                            {
                                                                                if (strLeftSideAux.IndexOf("codigo de verificacion :", 0) < strLeftSideAux.IndexOf("numero de radicacion :", 0) && strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("fecha de impresion :", 0) < strLeftSideAux.IndexOf("paginas :", 0) && strLeftSideAux.IndexOf("paginas :", 0) < strLeftSideAux.IndexOf(parameterLegend, 0))
                                                                                {
                                                                                    enableReadVirg = true;
                                                                                    enableExtracHeaderBog = true;
                                                                                    processMethodSingle = true;
                                                                                    poscStartWrd = poscAuxiliar;
                                                                                    enableSetEnd = true;
                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (strLeftSideAux.IndexOf("codigo de verificacion :", 0) > -1 && strLeftSideAux.IndexOf("numero de radicacion :", 0) > -1 && strLeftSideAux.IndexOf("fecha de impresion :", 0) > -1 && strLeftSideAux.IndexOf("paginas :", 0) > -1)
                                                                            {
                                                                                if (strLeftSideAux.IndexOf("codigo de verificacion :", 0) < strLeftSideAux.IndexOf("numero de radicacion :", 0) && strLeftSideAux.IndexOf("numero de radicacion :", 0) < strLeftSideAux.IndexOf("fecha de impresion :", 0) && strLeftSideAux.IndexOf("fecha de impresion :", 0) < strLeftSideAux.IndexOf("paginas :", 0))
                                                                                {
                                                                                    enableReadVirg = true;
                                                                                    enableExtracHeaderBog = true;
                                                                                    processMethodSingle = true;
                                                                                    poscStartWrd = poscAuxiliar;
                                                                                    enableSetEnd = false;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            poscStartWrd = poscAuxiliar + 1;

                                                            if (poscStartWrd > 0)
                                                            {
                                                                poscStartWrd3 = poscStartWrd;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (strCamaraComercioActive == "TUNJA" && arrayHeader[0].IndexOf("camara de comercio de tunja", 0) > -1 && poscStartWrd == poscStartWrd3)
                                                            {
                                                                poscAuxiliar = fullOCRProcess.IndexOf(arrayHeader[0], poscStartWrd);

                                                                if (poscAuxiliar >= 0)
                                                                {
                                                                    if (fullOCR.Length - poscAuxiliar > 550)
                                                                    {
                                                                        strLeftSideAux = fullOCRProcess.Substring(poscAuxiliar, 550);

                                                                        if (!string.IsNullOrEmpty(strLeftSideAux))
                                                                        {
                                                                            if (strLeftSideAux.IndexOf("camara de comercio de tunja", 0) > -1 && strLeftSideAux.IndexOf("certificado expedido a traves del portal de servicios virtuales ( sii )", 0) > -1 && strLeftSideAux.IndexOf("certificado de existencia y representacion legal", 0) > -1)
                                                                            {
                                                                                if (strLeftSideAux.IndexOf("camara de comercio de cartagena", 0) < strLeftSideAux.IndexOf("certificado expedido a traves del portal de servicios virtuales ( sii )", 0) && strLeftSideAux.IndexOf("certificado expedido a traves del portal de servicios virtuales ( sii )", 0) < strLeftSideAux.IndexOf("certificado de existencia y representacion legal", 0))
                                                                                {
                                                                                    enableReadVirg = true;
                                                                                    enableExtracHeaderBog = true;
                                                                                    processMethodSingle = true;
                                                                                    poscStartWrd = poscAuxiliar;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }

                                                                poscStartWrd = poscAuxiliar + 1;

                                                                if (poscStartWrd > 0)
                                                                {
                                                                    poscStartWrd3 = poscStartWrd;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (strCamaraComercioActive == "BUENAVENTURA" || strCamaraComercioActive == "BUGA" || strCamaraComercioActive == "CALI" || strCamaraComercioActive == "CARTAGENA" || strCamaraComercioActive == "IBAGUE" || strCamaraComercioActive == "PALMIRA" || strCamaraComercioActive == "TULUA" || strCamaraComercioActive == "TUNJA")
                                                                {
                                                                    enableReadVirg = true;
                                                                    enableExtracHeaderBog = true;
                                                                    processMethodSingle = true;

                                                                    if (poscAuxiliar == -1)
                                                                    {
                                                                        bolIterateDocument = true;
                                                                        enableExtracHeaderBog = false;
                                                                        enableReadVirg = true;
                                                                        poscStartWrd = -1;
                                                                        poscEndWrd = -1;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    //En este caso ingresa para las demás camaras de comercio:
                                                                    enableExtracHeaderBog = true;
                                                                    enableReadVirg = true;
                                                                    enableSetEnd = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (enableExtracHeaderBog)
                                {
                                    arrayWords = null;
                                    bolEnableSubsHeader = false;
                                    
                                    if (!processMethodSingle)
                                    {
                                        if (poscStartWrd < fullOCRProcess.Length)
                                        {
                                            poscStartWrd = fullOCRProcess.IndexOf(strLeftSide, poscStartWrd);
                                        }
                                        else
                                        {
                                            poscStartWrd = -1;
                                            poscEndWrd = -1;
                                        }
                                    }

                                    poscAuxiliar = 0;
                                    poscStartWrd2 = poscStartWrd;

                                    if (poscStartWrd > -1)
                                    {
                                        if (strLeftSide.IndexOf("codigo de verificacion", 0) >= 0)
                                        {
                                            if (strRigthSide.IndexOf("paginas : ") >= 0)
                                            {
                                                arrayWords = new string[3];
                                                wordFindHead = "paginas :";   //paginas : 1 de 10

                                                arrayWords = setArrayWords(arrayWords, fullOCR, 3, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[0]);
                                                arrayWords = setArrayWords(arrayWords, fullOCR, 3, 2, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[1]);
                                                arrayWords = setArrayWords(arrayWords, fullOCR, 3, 3, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[2]);

                                                lengthAux = 0;
                                                for (int i = 0; i < arrayWords.Count(); i++)
                                                {
                                                    if (!string.IsNullOrEmpty(arrayWords[i]))
                                                    {
                                                        lengthAux += arrayWords[i].Length;
                                                        idxArrayWords++;
                                                    }
                                                }

                                                if (arrayWords.Count() == idxArrayWords)
                                                {
                                                    if (EsNumerico(arrayWords[0]) && EsNumerico(arrayWords[2]))
                                                    {
                                                        lengthAux += 12;
                                                        poscEndWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2) + lengthAux;
                                                    }
                                                    else
                                                    {
                                                        lengthAux += 9;
                                                        poscEndWrd = fullOCRProcess.IndexOf("paginas :", poscStartWrd2) + lengthAux;
                                                    }
                                                }
                                                idxArrayWords = 0;

                                                if (poscEndWrd - poscStartWrd <= 500)
                                                {
                                                    poscStartWrd2 = poscEndWrd + 1;
                                                }
                                                else
                                                {
                                                    poscEndWrd = -1;
                                                    poscStartWrd = -1;
                                                }
                                            }
                                            else
                                            {
                                                if (enableSetEnd)
                                                {
                                                    poscAuxiliar = fullOCRProcess.IndexOf(strRigthSide, poscStartWrd2);
                                                    if (poscAuxiliar > -1)
                                                    {
                                                        poscAuxiliar += strRigthSide.Length;
                                                        if (poscAuxiliar >= 0)
                                                        {
                                                            poscEndWrd = poscAuxiliar;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        poscEndWrd = -1;
                                                        poscStartWrd = -1;
                                                    }
                                                    enableSetEnd = false;
                                                }
                                                else
                                                {
                                                    poscEndWrd = -1;
                                                    poscStartWrd = -1;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (strRigthSide.IndexOf("numero de operacion : ", 0) >= 0)
                                            {
                                                arrayWordsAux = strRigthSide.Split(' ');

                                                if (arrayWordsAux.Count() >= 2)
                                                {
                                                    for (int i = 0; i < arrayWordsAux.Count(); i++)
                                                    {
                                                        poscEndWrd = poscEndWrd + arrayWordsAux[i].Length;
                                                    }

                                                    poscEndWrd = poscEndWrd + arrayWordsAux.Count() - 1;
                                                }
                                            }
                                            else
                                            {
                                                //In these case the header to begin with the other line distinct to "Código de Verificación/Número de Operación":
                                                if (strRigthSide.IndexOf("copia : ") >= 0)
                                                {
                                                    arrayWords = new string[3];
                                                    wordFindHead = "copia :";   //copia : 1 de 1

                                                    arrayWords = setArrayWords(arrayWords, fullOCR, 4, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                    wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[0]);
                                                    arrayWords = setArrayWords(arrayWords, fullOCR, 4, 2, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                    wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[1]);
                                                    arrayWords = setArrayWords(arrayWords, fullOCR, 4, 3, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                    wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[2]);

                                                    lengthAux = 0;
                                                    for (int i = 0; i < arrayWords.Count(); i++)
                                                    {
                                                        if (!string.IsNullOrEmpty(arrayWords[i]))
                                                        {
                                                            lengthAux += arrayWords[i].Length;
                                                            idxArrayWords++;
                                                        }
                                                    }

                                                    if (arrayWords.Count() == idxArrayWords)
                                                    {
                                                        lengthAux += 10;
                                                        poscEndWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2) + lengthAux;
                                                    }
                                                    idxArrayWords = 0;

                                                    if (poscEndWrd - poscStartWrd <= 500)
                                                    {
                                                        poscStartWrd2 = poscEndWrd + 1;
                                                    }
                                                    else
                                                    {
                                                        poscEndWrd = -1;
                                                        poscStartWrd = -1;
                                                    }
                                                }
                                                else
                                                {
                                                    if (strRigthSide.IndexOf("codigo de verificacion : ", 0) >= 0)
                                                    {
                                                        arrayWords = new string[1];
                                                        wordFindHead = "codigo de verificacion :";
                                                        arrayWords = setArrayWords(arrayWords, fullOCR, 5, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                        bolEnableSubsHeader = true;

                                                        for (int i = 0; i < arrayWords.Count(); i++)
                                                        {
                                                            if (!string.IsNullOrEmpty(arrayWords[i]))
                                                            {
                                                                idxArrayWords++;
                                                            }
                                                        }

                                                        if (arrayWords.Count() == idxArrayWords)
                                                        {
                                                            poscEndWrd = poscAuxiliar;
                                                        }
                                                        idxArrayWords = 0;

                                                        if (poscEndWrd - poscStartWrd > 500)
                                                        {
                                                            poscEndWrd = -1;
                                                            poscStartWrd = -1;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (strRigthSide.IndexOf("codigo de verificacion", 0) >= 0)
                                                        {
                                                            poscAuxiliar = fullOCRProcess.IndexOf("codigo de verificacion", poscStartWrd2);
                                                            poscEndWrd = poscAuxiliar + 22;

                                                            if (poscEndWrd - poscStartWrd > 500)
                                                            {
                                                                poscEndWrd = -1;
                                                                poscStartWrd = -1;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (strRigthSide.IndexOf("nit :", 0) >= 0)
                                                            {
                                                                arrayWords = new string[3];
                                                                wordFindHead = "nit : ";   //nit : 830.505.120 - 5.

                                                                arrayWords = setArrayWords(arrayWords, fullOCR, 6, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[0]);
                                                                arrayWords = setArrayWords(arrayWords, fullOCR, 6, 2, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[1]);
                                                                arrayWords = setArrayWords(arrayWords, fullOCR, 6, 3, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[2]);

                                                                lengthAux = 0;
                                                                for (int i = 0; i < arrayWords.Count(); i++)
                                                                {
                                                                    if (!string.IsNullOrEmpty(arrayWords[i]))
                                                                    {
                                                                        lengthAux += arrayWords[i].Length;
                                                                        idxArrayWords++;
                                                                    }
                                                                }

                                                                if (arrayWords.Count() == idxArrayWords)
                                                                {
                                                                    lengthAux += 8;
                                                                    poscEndWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2) + lengthAux;
                                                                }
                                                                idxArrayWords = 0;

                                                                if (poscEndWrd - poscStartWrd <= 500)
                                                                {
                                                                    poscStartWrd2 = poscEndWrd + 1;
                                                                }
                                                                else
                                                                {
                                                                    poscEndWrd = -1;
                                                                    poscStartWrd = -1;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (strRigthSide.IndexOf("pagina : ", 0) >= 0)
                                                                {
                                                                    arrayWords = new string[1];
                                                                    wordFindHead = "pagina :";

                                                                    arrayWords = setArrayWords(arrayWords, fullOCR, 7, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                    bolEnableSubsHeader = true;

                                                                    if (!string.IsNullOrEmpty(arrayWords[0]))
                                                                    {
                                                                        poscEndWrd = poscAuxiliar;
                                                                    }
                                                                    idxArrayWords = 0;

                                                                    if (poscEndWrd - poscStartWrd > 500)
                                                                    {
                                                                        poscEndWrd = -1;
                                                                        poscStartWrd = -1;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (strRigthSide.IndexOf("paginas : ") >= 0)
                                                                    {
                                                                        arrayWords = new string[3];
                                                                        wordFindHead = "paginas :";   //paginas : 1 de 10

                                                                        arrayWords = setArrayWords(arrayWords, fullOCR, 8, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                        wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[0]);
                                                                        arrayWords = setArrayWords(arrayWords, fullOCR, 8, 2, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                        wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[1]);
                                                                        arrayWords = setArrayWords(arrayWords, fullOCR, 8, 3, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                        wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[2]);

                                                                        lengthAux = 0;
                                                                        for (int i = 0; i < arrayWords.Count(); i++)
                                                                        {
                                                                            if (!string.IsNullOrEmpty(arrayWords[i]))
                                                                            {
                                                                                lengthAux += arrayWords[i].Length;
                                                                                idxArrayWords++;
                                                                            }
                                                                        }

                                                                        if (arrayWords.Count() == idxArrayWords)
                                                                        {
                                                                            if (EsNumerico(arrayWords[0]) && EsNumerico(arrayWords[2]))
                                                                            {
                                                                                lengthAux += 12;
                                                                                poscEndWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2) + lengthAux;
                                                                            }
                                                                            else
                                                                            {
                                                                                lengthAux += 9;
                                                                                poscEndWrd = fullOCRProcess.IndexOf("paginas :", poscStartWrd2) + lengthAux;
                                                                            }
                                                                        }
                                                                        idxArrayWords = 0;

                                                                        if (poscEndWrd - poscStartWrd <= 500)
                                                                        {
                                                                            poscStartWrd2 = poscEndWrd + 1;
                                                                        }
                                                                        else
                                                                        {
                                                                            poscEndWrd = -1;
                                                                            poscStartWrd = -1;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (strRigthSide.IndexOf("fecha de impresion : ") >= 0)
                                                                        {
                                                                            arrayWords = new string[10];
                                                                            wordFindHead = "fecha de impresion :";   //fecha de impresion : martes 28 febrero 2017 11 : 54 : 40 am

                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[0]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 2, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[1]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 3, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[2]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 4, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[3]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 5, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[4]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 6, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[5]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 7, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[6]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 8, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[7]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 9, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[8]);
                                                                            arrayWords = setArrayWords(arrayWords, fullOCR, 9, 10, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                            wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[9]);

                                                                            lengthAux = 0;
                                                                            for (int i = 0; i < arrayWords.Count(); i++)
                                                                            {
                                                                                if (!string.IsNullOrEmpty(arrayWords[i]))
                                                                                {
                                                                                    lengthAux += arrayWords[i].Length;
                                                                                    idxArrayWords++;
                                                                                }
                                                                            }

                                                                            if (arrayWords.Count() == idxArrayWords)
                                                                            {
                                                                                lengthAux += 30;
                                                                                poscEndWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2) + lengthAux;
                                                                            }
                                                                            idxArrayWords = 0;

                                                                            if (poscEndWrd - poscStartWrd <= 500)
                                                                            {
                                                                                poscStartWrd2 = poscEndWrd + 1;
                                                                            }
                                                                            else
                                                                            {
                                                                                poscEndWrd = -1;
                                                                                poscStartWrd = -1;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            //pagina: 1 camara de comercio de bogota                                                                
                                                                            if (strRigthSide.IndexOf("pagina : ") >= 0 && strRigthSide.IndexOf("camara de comercio de ") >= 0 && incPagina < Convert.ToInt32(totalPaginas))
                                                                            {
                                                                                if (poscStartWrd2 == 0)
                                                                                {
                                                                                    incPagina = 1;
                                                                                }

                                                                                headquarter = strRigthSide.Substring(strRigthSide.LastIndexOf("camara de comercio de "), (strRigthSide.Length - strRigthSide.LastIndexOf("camara de comercio de ")));
                                                                                wordFindHead = string.Format("pagina : {0} camara de comercio de {1}", Convert.ToString(incPagina), headquarter);
                                                                                poscAuxiliar = fullOCRProcess.LastIndexOf(wordFindHead, poscAuxiliar);

                                                                                if (poscAuxiliar >= 0)
                                                                                {
                                                                                    poscEndWrd = poscAuxiliar;
                                                                                }
                                                                                incPagina++;

                                                                                if (poscEndWrd - poscStartWrd > 500)
                                                                                {
                                                                                    poscEndWrd = -1;
                                                                                    poscStartWrd = -1;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                if ((strRigthSide.IndexOf("pagina : ") >= 0 && strRigthSide.IndexOf("camara de comercio de ") >= 0 && incPagina == Convert.ToInt32(totalPaginas)) || !(strRigthSide.IndexOf("pagina : ") >= 0 && strRigthSide.IndexOf("camara de comercio de ") >= 0))
                                                                                {
                                                                                    if (strRigthSide.IndexOf("pagina no.") >= 0)
                                                                                    {
                                                                                        if (poscStartWrd2 == 0)
                                                                                        {
                                                                                            incPagina = 1;
                                                                                        }

                                                                                        //pagina no. 1 *****************************************************************
                                                                                        wordFindHead = string.Format("pagina no. {0} *****************************************************************", Convert.ToString(incPagina));
                                                                                        poscAuxiliar = fullOCRProcess.LastIndexOf(wordFindHead, poscAuxiliar);

                                                                                        if (poscAuxiliar >= 0)
                                                                                        {
                                                                                            poscEndWrd = poscAuxiliar;
                                                                                        }
                                                                                        incPagina++;

                                                                                        if (poscEndWrd - poscStartWrd > 500)
                                                                                        {
                                                                                            poscEndWrd = -1;
                                                                                            poscStartWrd = -1;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        if (strRigthSide.IndexOf("operacion : ") >= 0)
                                                                                        {
                                                                                            arrayWords = new string[1];
                                                                                            wordFindHead = "operacion :";

                                                                                            poscAuxiliar = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2);
                                                                                            arrayWords[0] = fullOCRProcess.Substring(poscAuxiliar, fullOCRProcess.IndexOf(' ', poscAuxiliar + 1) - poscAuxiliar);

                                                                                            bolEnableSubsHeader = true;

                                                                                            if (!string.IsNullOrEmpty(arrayWords[0]))
                                                                                            {
                                                                                                wordFindHead = string.Format("operacion : {0} *****************************************************************", arrayWords[0]);
                                                                                                poscAuxiliar = fullOCRProcess.LastIndexOf(wordFindHead, poscStartWrd2);

                                                                                                if (poscAuxiliar >= 0)
                                                                                                {
                                                                                                    poscEndWrd = poscAuxiliar;
                                                                                                }
                                                                                            }
                                                                                            idxArrayWords = 0;

                                                                                            if (poscEndWrd - poscStartWrd > 500)
                                                                                            {
                                                                                                poscEndWrd = -1;
                                                                                                poscStartWrd = -1;
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if (strRigthSide.IndexOf("num. operacion. ") >= 0)
                                                                                            {
                                                                                                arrayWords = new string[1];
                                                                                                wordFindHead = "num. operacion.";

                                                                                                poscAuxiliar = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2);
                                                                                                arrayWords[0] = fullOCRProcess.Substring(poscAuxiliar, fullOCRProcess.IndexOf(' ', poscAuxiliar + 1) - poscAuxiliar);

                                                                                                bolEnableSubsHeader = true;

                                                                                                if (!string.IsNullOrEmpty(arrayWords[0]))
                                                                                                {
                                                                                                    if (poscAuxiliar >= 0)
                                                                                                    {
                                                                                                        poscEndWrd = poscAuxiliar;
                                                                                                    }
                                                                                                }
                                                                                                idxArrayWords = 0;

                                                                                                if (poscEndWrd - poscStartWrd > 500)
                                                                                                {
                                                                                                    poscEndWrd = -1;
                                                                                                    poscStartWrd = -1;
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (strRigthSide.IndexOf("operacion no.") >= 0)
                                                                                                {
                                                                                                    headquarter = strRigthSide.Substring(strRigthSide.LastIndexOf("operacion no. "), (strRigthSide.Length - strRigthSide.LastIndexOf("operacion no. ")));
                                                                                                    wordFindHead = string.Format("operacion no. {0}", headquarter);
                                                                                                    poscAuxiliar = fullOCRProcess.LastIndexOf(wordFindHead, poscAuxiliar);

                                                                                                    if (poscAuxiliar >= 0)
                                                                                                    {
                                                                                                        poscEndWrd = poscAuxiliar;
                                                                                                    }

                                                                                                    if (poscEndWrd - poscStartWrd > 500)
                                                                                                    {
                                                                                                        poscEndWrd = -1;
                                                                                                        poscStartWrd = -1;
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if (strRigthSide.IndexOf("fecha de generacion : ") >= 0)
                                                                                                    {
                                                                                                        arrayWords = new string[3];
                                                                                                        wordFindHead = "fecha de generacion :";   //fecha de generación : 03/11/2015 03:18 p.m.

                                                                                                        arrayWords = setArrayWords(arrayWords, fullOCR, 10, 1, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                                                        wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[0]);
                                                                                                        arrayWords = setArrayWords(arrayWords, fullOCR, 10, 2, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                                                        wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[1]);
                                                                                                        arrayWords = setArrayWords(arrayWords, fullOCR, 10, 3, wordFindHead, ref poscStartWrd2, ref poscAuxiliar);

                                                                                                        wordFindHead = string.Format("{0} {1}", wordFindHead, arrayWords[2]);

                                                                                                        lengthAux = 0;
                                                                                                        for (int i = 0; i < arrayWords.Count(); i++)
                                                                                                        {
                                                                                                            if (!string.IsNullOrEmpty(arrayWords[i]))
                                                                                                            {
                                                                                                                lengthAux += arrayWords[i].Length;
                                                                                                                idxArrayWords++;
                                                                                                            }
                                                                                                        }

                                                                                                        if (arrayWords.Count() == idxArrayWords)
                                                                                                        {
                                                                                                            lengthAux += 24;
                                                                                                            poscEndWrd = fullOCRProcess.IndexOf(wordFindHead, poscStartWrd2) + lengthAux;
                                                                                                        }
                                                                                                        idxArrayWords = 0;

                                                                                                        if (poscEndWrd - poscStartWrd <= 500)
                                                                                                        {
                                                                                                            poscStartWrd2 = poscEndWrd + 1;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            poscEndWrd = -1;
                                                                                                            poscStartWrd = -1;
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        poscAuxiliar = fullOCRProcess.IndexOf(strRigthSide, poscStartWrd2);
                                                                                                        if (poscAuxiliar > -1)
                                                                                                        {
                                                                                                            poscAuxiliar += strRigthSide.Length;
                                                                                                            if (poscAuxiliar >= 0)
                                                                                                            {
                                                                                                                poscEndWrd = poscAuxiliar;
                                                                                                            }

                                                                                                            if (poscEndWrd - poscStartWrd > 500)
                                                                                                            {
                                                                                                                poscEndWrd = -1;
                                                                                                                poscStartWrd = -1;
                                                                                                            }
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            poscEndWrd = -1;
                                                                                                            poscStartWrd = -1;
                                                                                                        }
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        poscStartWrd = -1;
                                        poscEndWrd = -1;
                                    }

                                    /*** En este bloque finaliza el corte y retiro del head del document
                                     * 
                                     * 
                                     * **/
                                     
                                    if (poscStartWrd >= 0 && poscEndWrd > 0)
                                    {
                                        sizeFullOCR = fullOCR.Length;   //50
                                        if (poscStartWrd == 0 || (poscStartWrd == 1 && processMethodSingle))
                                        {
                                            strLeftSideAux = "";

                                            if (poscEndWrd - poscStartWrd < length)
                                            {
                                                poscEndWrd = poscEndWrd + (length - (poscEndWrd - poscStartWrd));
                                            }
                                            strRigthSide = fullOCR.Substring(poscEndWrd, sizeFullOCR - poscEndWrd);
                                        }
                                        else
                                        {
                                            strLeftSide = fullOCR.Substring(0, poscStartWrd);
                                            if (poscEndWrd - poscStartWrd < length)
                                            {
                                                poscEndWrd = poscEndWrd + (length - (poscEndWrd - poscStartWrd));
                                            }
                                            strRigthSide = fullOCR.Substring(poscEndWrd, sizeFullOCR - poscEndWrd);
                                        }

                                        if (!string.IsNullOrEmpty(strRigthSide))
                                        {
                                            if (poscStartWrd == 0 || (poscStartWrd == 1 && processMethodSingle))
                                            {
                                                fullOCR = strRigthSide;
                                            }
                                            else
                                            {
                                                fullOCR = string.Format("{0}{1}", strLeftSide, strRigthSide);
                                            }
                                            fullOCRProcess = fullOCR;
                                            fullOCRProcess = ChangeFullOCR(fullOCRProcess);
                                        }

                                        strLeftSide = arrayHeader[0];
                                        strRigthSide = arrayHeader[arrayHeader.Count() - 1];

                                        poscStartWrd = poscEndWrd + 1;

                                        if (processMethodSingle)
                                        {
                                            poscStartWrd3 = poscStartWrd;
                                            enableSetEnd = false;
                                        }
                                    }
                                    else
                                    {
                                        bolIterateDocument = true;
                                    }

                                }

                            }//// End the validation
                        }
                    }
                }

                #endregion Delete header for each page of document


                #region Delete footer for each page of document

                fullOCRProcess = fullOCR;
                fullOCRProcess = ChangeFullOCR(fullOCRProcess);

                poscStartWrd = 0;

                if (strCamaraComercioActive.ToLower() != "medellin")
                {
                    poscStartWrd = 0;
                    poscEndWrd = 0;
                    for (int i = 1; i <= Convert.ToInt32(totalPaginas); i++)
                    {
                        stringHeader = string.Format("pag {0} de {1}", Convert.ToString(i), totalPaginas);

                        if (fullOCR.IndexOf(stringHeader, poscStartWrd) == -1)
                        {
                            stringHeader = string.Format("pag. {0} de {1}", Convert.ToString(i), totalPaginas);

                            if (fullOCR.IndexOf(stringHeader, poscStartWrd) == -1)
                            {
                                stringHeader = string.Format("pagina ", Convert.ToString(i));

                                if (fullOCR.IndexOf(stringHeader, poscStartWrd) == -1)
                                {
                                }
                                else
                                {
                                    poscStartWrd = fullOCRProcess.IndexOf(stringHeader, poscStartWrd);
                                    poscEndWrd = poscStartWrd + stringHeader.Length + 1;
                                }
                            }
                            else
                            {
                                poscStartWrd = fullOCRProcess.IndexOf(stringHeader, poscStartWrd);
                                poscEndWrd = poscStartWrd + stringHeader.Length + 1;
                            }
                        }
                        else
                        {
                            poscStartWrd = fullOCRProcess.IndexOf(stringHeader, poscStartWrd);
                            poscEndWrd = poscStartWrd + stringHeader.Length + 1;
                        }

                        if (poscStartWrd > 0 && poscEndWrd > 0)
                        {
                            if (!string.IsNullOrEmpty(stringHeader))
                            {
                                if (poscStartWrd > 0)
                                {
                                    strLeftSide = fullOCR.Substring(0, poscStartWrd);

                                    if (fullOCR.Length > poscEndWrd)
                                    {
                                        strRigthSide = fullOCR.Substring(poscEndWrd, fullOCR.Length - poscEndWrd);
                                    }
                                    else
                                    {
                                        strRigthSide = fullOCR.Substring(poscStartWrd, (poscEndWrd - 1) - poscStartWrd);
                                    }

                                    fullOCR = string.Format("{0}{1}", strLeftSide, strRigthSide);
                                    fullOCRProcess = fullOCR;
                                    fullOCRProcess = ChangeFullOCR(fullOCRProcess);
                                }

                                poscStartWrd = poscEndWrd + 1;
                            }
                        }
                    }
                }


                bolIterateDocument = false;
                while (!bolIterateDocument)
                {
                    poscStartWrd = 0;
                    poscEndWrd = 0;

                    if (fullOCR.IndexOf("************ continua ************", poscStartWrd) > -1 || fullOCR.IndexOf("************continua..*.........", poscStartWrd) > -1 || fullOCR.IndexOf("************ continua************", poscStartWrd) > -1)
                    {
                        if (fullOCR.IndexOf("************ continua ************", poscStartWrd) > -1)
                        {
                            stringHeader = "************ continua ************";
                        }
                        else
                        {
                            if (fullOCR.IndexOf("************ continua************", poscStartWrd) > -1)
                            {
                                stringHeader = "************ continua************";
                            }
                            else
                            {
                                stringHeader = "************continua..*.........";
                            }
                        }

                        poscStartWrd = fullOCR.IndexOf(stringHeader, poscStartWrd);
                        poscEndWrd = poscStartWrd + stringHeader.Length + 1;

                        fullOCR = getFullOCR_Change_Footer(ref poscStartWrd, ref poscEndWrd, stringHeader, fullOCR, ref bolIterateDocument);
                    }
                    else
                    {
                        bolIterateDocument = true;
                    }
                }

                bolIterateDocument = false;
                while (!bolIterateDocument)
                {
                    poscStartWrd = 0;
                    poscEndWrd = 0;
                    stringHeader = "********** c o n t i n u a **********";
                    if (fullOCR.IndexOf(stringHeader, poscStartWrd) > -1)
                    {
                        poscStartWrd = fullOCR.IndexOf(stringHeader, poscStartWrd);
                        poscEndWrd = poscStartWrd + stringHeader.Length + 1;

                        fullOCR = getFullOCR_Change_Footer(ref poscStartWrd, ref poscEndWrd, stringHeader, fullOCR, ref bolIterateDocument);
                    }
                    else
                    {
                        bolIterateDocument = true;
                    }
                }

                bolIterateDocument = false;
                while (!bolIterateDocument)
                {
                    poscStartWrd = 0;
                    poscEndWrd = 0;
                    stringHeader = "*** continua ***";
                    if (fullOCR.IndexOf(stringHeader, poscStartWrd) > -1)
                    {
                        poscStartWrd = fullOCR.IndexOf(stringHeader, poscStartWrd);
                        poscEndWrd = poscStartWrd + stringHeader.Length + 1;

                        fullOCR = getFullOCR_Change_Footer(ref poscStartWrd, ref poscEndWrd, stringHeader, fullOCR, ref bolIterateDocument);
                    }
                    else
                    {
                        bolIterateDocument = true;
                    }
                }

                bolIterateDocument = false;
                while (!bolIterateDocument)
                {
                    poscStartWrd = 0;
                    poscEndWrd = 0;
                    stringHeader = "** en la camara estamos comprometidos con el medio ambiente. este documento ha sido impreso en papel ecologico **";
                    if (fullOCR.IndexOf(stringHeader, poscStartWrd) > -1)
                    {
                        poscStartWrd = fullOCR.IndexOf(stringHeader, poscStartWrd);
                        poscEndWrd = poscStartWrd + stringHeader.Length + 1;

                        fullOCR = getFullOCR_Change_Footer(ref poscStartWrd, ref poscEndWrd, stringHeader, fullOCR, ref bolIterateDocument);
                    }
                    else
                    {
                        bolIterateDocument = true;
                    }
                }

                bolIterateDocument = false;
                while (!bolIterateDocument)
                {
                    poscStartWrd = 0;
                    poscEndWrd = 0;
                    stringHeader = "caldas - envigado - itagui - la estrella - sabaneta";
                    if (fullOCR.IndexOf(stringHeader, poscStartWrd) > -1)
                    {
                        poscStartWrd = fullOCR.IndexOf(stringHeader, poscStartWrd);
                        poscEndWrd = poscStartWrd + stringHeader.Length + 1;

                        fullOCR = getFullOCR_Change_Footer(ref poscStartWrd, ref poscEndWrd, stringHeader, fullOCR, ref bolIterateDocument);
                    }
                    else
                    {
                        bolIterateDocument = true;
                    }
                }

                bolIterateDocument = false;
                while (!bolIterateDocument)
                {
                    poscStartWrd = 0;
                    poscEndWrd = 0;
                    stringHeader = "este documento solo tiene validez legal en formato electronico";
                    if (fullOCR.IndexOf(stringHeader, poscStartWrd) > -1)
                    {
                        poscStartWrd = fullOCR.IndexOf(stringHeader, poscStartWrd);
                        poscEndWrd = poscStartWrd + stringHeader.Length + 1;

                        fullOCR = getFullOCR_Change_Footer(ref poscStartWrd, ref poscEndWrd, stringHeader, fullOCR, ref bolIterateDocument);
                    }
                    else
                    {
                        bolIterateDocument = true;
                    }
                }

                #endregion Delete footer for each page of document

            }
            catch (Exception ex)
            {
                string error = "";
            }
            finally
            {
            }

            #endregion

            bool MarkCaptureFailed = false;

            try
            {

                #region Extracción campo Facultad de Avalar

                deliveryList = GetDeliveryList(fullOCR, wordList, /*listWordFacultadAvalar*/ listWordSearchGral[0], "FacultadAvalar", deliveryList, listFieldsContextuals);

                #endregion


                #region Extracción campo Monto

                deliveryList = GetDeliveryList(fullOCR, wordList, /*listWordMonto*/ listWordSearchGral[1], "Monto", deliveryList, listFieldsContextuals);

                #endregion


                #region Extracción campo Actividad

                deliveryList = GetDeliveryList(fullOCR, wordList, /*listWordActividad*/ listWordSearchGral[2], "Actividad", deliveryList, listFieldsContextuals);

                #endregion

            }
            catch (Exception ex)
            {
                MarkCaptureFailed = true;
            }


            /*#region Variables cálculo porcentaje mínimo de confianza
            double porcMinConfFacultadAvalar = 0;
            double porcMinConfMonto = 0;
            double porcMinConfActividad = 0;
            int totalWordOK = 0;
            #endregion

            #region Porcentaje Mínimo de Confianza de Extracción => Facultad de Avalar.
            porcMinConfFacultadAvalar = getMinPorcentConfidence(listWordFacultadAvalar, deliveryList, MarkCaptureFailed, 0, ref totalWordOK);

            //if (porcMinConfFacultadAvalar > 0)
            //{
            //    porcMinConfFacultadAvalar = (porcMinConfFacultadAvalar / totalWordOK);
            //}
            #endregion Porcentaje Mínimo de Confianza de Extracción => Facultad de Avalar.


            #region Porcentaje Mínimo de Confianza de Extracción => Monto.
            MarkCaptureFailed = false;
            totalWordOK = 0;

            porcMinConfMonto = getMinPorcentConfidence(listWordMonto, deliveryList, MarkCaptureFailed, 1, ref totalWordOK);

            //if (porcMinConfMonto > 0)
            //{
            //    porcMinConfMonto = (porcMinConfMonto / totalWordOK);
            //}
            #endregion Porcentaje Mínimo de Confianza de Extracción => Monto.


            #region Porcentaje Mínimo de Confianza de Extracción => Actividad.
            MarkCaptureFailed = false;
            totalWordOK = 0;

            porcMinConfActividad = getMinPorcentConfidence(listWordActividad, deliveryList, MarkCaptureFailed, 2, ref totalWordOK);

            //if (porcMinConfActividad > 0)
            //{
            //    porcMinConfActividad = (porcMinConfActividad / totalWordOK);
            //}
            #endregion Porcentaje Mínimo de Confianza de Extracción => Actividad.*/


            var queryFacultadAvalar = deliveryList[0]
                .OrderBy(x => x.PositionStart)
                .Select(x => x.Word);

            List<List<string>> research = new List<List<string>>();

            research.Add(new List<string>());
            research.Add(new List<string>());
            research.Add(new List<string>());

            StringBuilder sbFacultadAvalar = new StringBuilder();
            foreach (var itemFA in queryFacultadAvalar)
            {
                string strItemFA = GetFieldContextualFormat(itemFA.ToString(), "Facultad de Avalar", listWordSearchGral[0], listFieldsContextuals);
                sbFacultadAvalar.Append(strItemFA);
                sbFacultadAvalar.AppendLine();
                sbFacultadAvalar.AppendLine();
            }

            research[0].Add(sbFacultadAvalar.ToString());

            var queryMonto = deliveryList[1]
               .OrderBy(x => x.PositionStart)
               .Select(x => x.Word);

            StringBuilder sbMonto = new StringBuilder();
            foreach (var itemM in queryMonto)
            {
                string strItemM = GetFieldContextualFormat(itemM.ToString(), "Monto", listWordSearchGral[1], listFieldsContextuals);
                sbMonto.Append(strItemM);
                sbMonto.AppendLine();
                sbMonto.AppendLine();
            }

            research[1].Add(sbMonto.ToString());


            var queryActividad = deliveryList[2]
               .OrderBy(x => x.PositionStart)
               .Select(x => x.Word);

            StringBuilder sbActividad = new StringBuilder();
            foreach (var itemA in queryActividad)
            {
                string strItemA = GetFieldContextualFormat(itemA.ToString(), "Actividad", listWordSearchGral[2], listFieldsContextuals);
                sbActividad.Append(strItemA);
                sbActividad.AppendLine();
                sbActividad.AppendLine();
            }

            research[2].Add(sbActividad.ToString());
           
            ExtractResult result = new ExtractResult();
            result.Facultad = research[0].ToArray();
            result.Monto = research[1].ToArray();
            result.Actividad = research[2].ToArray();
            
            result.FullOCR = "";

            return result;
        }


        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }


        public string[] setArrayWords(string[] arrayWords, string fullOCR, int typeFilterEval, int numField, string wordFindHead, ref int poscStartWrd, ref int poscAuxiliar)
        {
            numField--;            
            poscAuxiliar = fullOCR.IndexOf(wordFindHead, poscStartWrd) + wordFindHead.Length + 1;
            arrayWords[numField] = fullOCR.Substring(poscAuxiliar, fullOCR.IndexOf(' ', poscAuxiliar + 1) - poscAuxiliar);
            poscAuxiliar = fullOCR.IndexOf(' ', poscAuxiliar + 1);
            return (arrayWords);
        }

        public double getFuzzyMatch(string fullOCR, string stringHeader, string totalPaginas, string pageActive)
        {
            double distHeader = 0;
            double fuzzyMatch = 0;
            double[] fuzzyMatchResult = new double[1];
            double length = 0;

            distHeader = LevenshteinDistance(stringHeader, fullOCR);
            fuzzyMatch = 0;
            length = 0;

            length = fullOCR.Length - distHeader;

            fuzzyMatch = (stringHeader.Length / length);
            fuzzyMatch = fuzzyMatch * 100;

            return (fuzzyMatch);
        }

        public double getMinPorcentConfidence(List<WordSearch> listWordFacultadAvalar, List<List<WordSearch>> deliveryList, bool MarkCaptureFailed, int indixeField, ref int totalWordOK)
        {

            #region Variables cálculo porcentaje mínimo de confianza
            double porcMinConfFacultadAvalar = 0;
            double porcMinConfFaculAvalarSingle = 0;
            double porcMinConfLeft = 0;
            double porcMinConfRigth = 0;
            int totalWordLeft = 0;
            int totalWordRigth = 0;
            int totalItemsFA = 0;
            int totalDays = 0;
            int poscKeyWord = 0;

            string valueFieldLeft = "";
            string valueFieldLeftAux = "";
            string valueFieldRigth = "";
            #endregion

            #region Porcentaje Mínimo de Confianza de Extracción => Facultad de Avalar.

            foreach (WordSearch wordSearch in deliveryList[indixeField])
            {
                var filterKeyWordFind = deliveryList[indixeField]
                .Where(x => x.KeyWordEval == wordSearch.KeyWordEval)
                .Select(x => x);

                if (filterKeyWordFind.Count() > 0)
                {
                    foreach (var itemParagrapher in filterKeyWordFind)
                    {
                        totalWordLeft = 0;
                        string[] arrayParagrapher = itemParagrapher.Word.Split(' ');

                        foreach (string itemSelected in arrayParagrapher)
                        {
                            if (totalWordLeft == itemParagrapher.totalWordLeft)
                            {
                                poscKeyWord = valueFieldLeftAux.Length;
                            }

                            valueFieldLeftAux = string.Format("{0} {1}", valueFieldLeftAux, itemSelected);
                            totalWordLeft++;
                        }

                        totalWordLeft = 0;

                        if (poscKeyWord > 0)
                        {
                            valueFieldLeft = itemParagrapher.Word.Substring(0, poscKeyWord).Trim();
                            valueFieldRigth = itemParagrapher.Word.Substring(poscKeyWord + wordSearch.KeyWordEval.Length + 1, itemParagrapher.Word.Length - (poscKeyWord + wordSearch.KeyWordEval.Length + 1));

                            string[] arrayWordL = valueFieldLeft.Split(' ');
                            string[] arrayWordR = valueFieldRigth.Split(' ');

                            string[] arrayWordResult = new string[arrayWordL.Count() + arrayWordR.Count() + 1];

                            for (int i = 0; i < arrayWordL.Count(); i++)
                            {
                                arrayWordResult[i] = arrayWordL[i];
                            }

                            arrayWordResult[arrayWordL.Count()] = wordSearch.KeyWordEval;

                            int j = arrayWordL.Count() + 1;
                            for (int i = 0; i < arrayWordR.Count(); i++)
                            {
                                arrayWordResult[j] = arrayWordR[i];
                                j++;
                            }

                            int IndexWR = 0;

                            for (int i = 0; i < arrayWordResult.Count(); i++)
                            {
                                if (arrayWordResult[i].Contains(wordSearch.KeyWordEval))
                                {
                                    IndexWR = i;
                                    i = arrayWordResult.Count();
                                }
                            }

                            string[] arrayWordResultAux;
                            totalWordLeft = 0;
                            totalWordRigth = 0;

                            foreach (string item in arrayWordResult)
                            {
                                if (string.IsNullOrEmpty(item.Trim()))
                                {
                                    totalWordLeft++;
                                }
                            }

                            arrayWordResultAux = new string[arrayWordResult.Count() - totalWordLeft];

                            foreach (string item in arrayWordResult)
                            {
                                if (!string.IsNullOrEmpty(item.Trim()))
                                {
                                    arrayWordResultAux[totalWordRigth] = item;
                                    totalWordRigth++;
                                }
                            }

                            arrayWordResult = arrayWordResultAux;

                            totalWordLeft = 0;
                            totalWordRigth = 0;

                            if (!MarkCaptureFailed)
                            {
                                if (IndexWR > 0)
                                {
                                    totalWordLeft = IndexWR - 1;

                                    if (IndexWR < arrayWordResult.Count())
                                    {
                                        totalWordRigth = (arrayWordResult.Count() - (totalWordLeft + 2));
                                    }

                                    porcMinConfLeft = 0;
                                    if (totalWordLeft > 0)
                                    {
                                        if ((totalWordLeft == 10 && indixeField == 0) || (totalWordLeft == 35 && indixeField == 1) || (totalWordLeft == 15 && indixeField == 2))
                                        {
                                            porcMinConfLeft = 1.0;
                                        }
                                        else
                                        {
                                            switch (indixeField)
                                            {
                                                case 0:
                                                    totalDays = 10;
                                                    break;
                                                case 1:
                                                    totalDays = 35;
                                                    break;
                                                case 2:
                                                    totalDays = 15;
                                                    break;
                                            }

                                            porcMinConfLeft = ((double)totalWordLeft / (double)totalDays);
                                        }
                                    }

                                    porcMinConfRigth = 0;
                                    if (totalWordRigth > 0)
                                    {
                                        if ((totalWordRigth == 40 && indixeField == 0) || (totalWordRigth == 15 && indixeField == 1) || (totalWordRigth == 15 && indixeField == 2))
                                        {
                                            porcMinConfRigth = 1.0;
                                        }
                                        else
                                        {
                                            switch (indixeField)
                                            {
                                                case 0:
                                                    totalDays = 40;
                                                    break;
                                                case 1:
                                                    totalDays = 15;
                                                    break;
                                                case 2:
                                                    totalDays = 15;
                                                    break;
                                            }

                                            porcMinConfRigth = ((double)totalWordRigth / (double)totalDays);
                                        }
                                    }

                                    porcMinConfFaculAvalarSingle = porcMinConfFaculAvalarSingle + ((porcMinConfLeft + porcMinConfRigth) / 2);
                                }
                                else
                                {
                                    porcMinConfFaculAvalarSingle += 0;
                                }
                            }
                            else
                            {
                                porcMinConfFaculAvalarSingle += 0;
                            }

                            totalItemsFA++;
                        }
                    }

                    if (porcMinConfFaculAvalarSingle > 0)
                    {
                        if (totalItemsFA > 0)
                        {
                            porcMinConfFaculAvalarSingle = (porcMinConfFaculAvalarSingle / totalItemsFA);
                            totalItemsFA = 0;
                        }
                    }
                    else
                    {
                        porcMinConfFaculAvalarSingle = 0;
                    }

                    porcMinConfFacultadAvalar += porcMinConfFaculAvalarSingle;
                    porcMinConfFaculAvalarSingle = 0;

                    totalWordOK++;
                }
            }

            if (porcMinConfFacultadAvalar > 0)
            {
                porcMinConfFacultadAvalar = (porcMinConfFacultadAvalar / totalWordOK);
            }

            #endregion Porcentaje Mínimo de Confianza de Extracción => Facultad de Avalar.

            return (porcMinConfFacultadAvalar);
        }


        public string ChangeFullOCR(string fullOCR)
        {
            fullOCR = fullOCR.Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("trav^s", "traves")
                .Replace("N^mero", "numero")
                .Replace("c6digo de verificaci6n", "codigo de verificacion").Replace("codigo de verificaci6n", "codigo de verificacion").Replace("namero", "numero")
                .Replace("afiliaoos", "afiliados")
                .Replace("cert[ficado", "certificado")
                .Replace("codigo de verifica ( non", "codigo de verificacion").Replace("verif[cac[6n", "verificacion").Replace("verificac[on", "verificacion")
                .Replace("de· ", "de ")
                .Replace("lacamara", "la camara")
                .Replace("ref'ublica", "republica")
                .Replace("s,m.lm.v.", "s.m.l.m.v.")
                .Replace("la samara de comercio", "la camara de comercio")
                .Replace("proporc1ona", "proporciona")
                .Replace("mercant^", "mercantil")
                .Replace("ex]stencia", "existencia")
                .Replace("secretar[o", "secretario")
                .Replace("verifocac^on", "verificacion")
                .Replace("verifucacion", "verificacion").Replace("cod1go", "codigo").Replace("sancoones", "sanciones").Replace("proporcdna", "proporciona")
                .Replace("cal!", "cali").Replace("representaci6n", "representacion").Replace("c^mara", "camara").Replace("certif[cado", "certificado")
                .Replace("certif icado", "certificado").Replace("c6digo", "codigo").Replace("expe[ido", "expedido").Replace("sll", "sii").Replace("sh", "sii").Replace("( su )", "(sii)")
                .Replace("matr|cula", "matricula").Replace("am8s", "a mas").Replace("'1.", "").Replace("ex[stencia", "existencia").Replace("certif[cado", "certificado")
                .Replace("rr", "").Replace("r m", "").Replace("f 1", "").Replace("'..", "").Replace("f{% -", "").Replace("/ .", "").Replace("f»2", "")
                .Replace("f^.", "").Replace("ti 2", "").Replace("- cert^ificado", "certificado").Replace("bamara", "camara").Replace("s!1", "sii")
                .Replace("serv[cios", "servicios").Replace("monter^a", "monteria").Replace("comerc[o", "comercio").Replace("v[rtuales", "virtuales")
                .Replace("caiviara", "camara").Replace("servic]os", "servicios").Replace("expe]ido", "expedido").Replace("s]l", "sii").Replace("ex]stencia", "existencia")
                .Replace("vlrtuales", "virtuales").Replace("serv[c[os", "servicios").Replace("sil", "sii").Replace("( sid", "sii").Replace("s.m.l,m,\\ /", "s.m.l.m.v")
                .Replace("coiviercio", "comercio").Replace("s.m.l,m.v", "s.m.l.m.v").Replace("cddigo de verificaci6n", "codigo de verificacion").Replace("com€rcio de cartag€na", "comercio de cartagena")
                .Replace("trav6s", "traves").Replace("comerclo", "comercio").Replace("expe[idoa taraves", "expedido a traves").Replace("virt[ja^ - - - - - ··;", "virtuales ( sii )")
                .Replace("camera de comercio de casanam", "camara de comercio de casanare").Replace("del.portal", "del portal")
                .Replace("31 de marzo,y evite", "31 de marzo y evite").Replace("matr|cula", "matricula").Replace("y'confianza", "y confianza")
                .Replace("  ", " ").Replace("(sii)", "( sii )").Replace("sli", "sii").Replace("certif]cado", "certificado").Replace("exped[do", "expedido")
                .Replace("deexistencm", "de existencia").Replace("e ) ( istencia", "de existencia").Replace("comercic}", "comercio").Replace("comerc!0", "comercio")
                .Replace("gamara", "camara").Replace("s[i", "sii").Replace("cert'if[cado", "certificado").Replace("s,m.l.m.v", "s.m.l.m.v")
                .Replace("negoc[os", "negocios").Replace("travis", "traves").Replace("matr^cula", "matricula").Replace("girardol", "girardot").Replace("oe", "de")
                .Replace("p / 5^·%'", "").Replace("( sii )", " sii ").Replace(" sii ", " ( sii ) ").Replace("camara de comercio de girardot,", "camara de comercio de girardot")
                .Replace("  ( sii )  ", " ( sii ) ").Replace("atraves", "a traves").Replace("certficado", "certificado")
                .Replace("exbtencia", "existencia").Replace("representac[on", "representacion").Replace("ver f cac on", "verificacion").Replace("verifica ( non", "verificacion")
                .Replace("expeoido", "expedido").Replace("certihcado", "certificado").Replace("matrfcula", "matricula").Replace("certif'cado", "certificado")
                .Replace("matrjcula", "matricula").Replace("cod[go", "codigo").Replace("f,0", "").Replace("serv[cios", "servicios")
                .Replace("comerc!0", "comercio").Replace("representac[on", "representacion").Replace("s!1", "sii").Replace("cert]ficado", "certificado")
                .Replace("ver[ficaci6n", "verificacion").Replace("comerc[o", "comercio").Replace("v[rtuales", "virtuales").Replace("certif[cado", "certificado")
                .Replace("s][", "sii").Replace("v[rtuales", "virtuales").Replace("exped]do", "expedido").Replace("servic]os", "servicios")
                .Replace("( sii ) eomercia", "( sii )").Replace("exped[doa", "expedida").Replace("certif[cado", "certificado")
                .Replace("servic[os", "servicios").Replace("cert]ficado", "certificado").Replace("serv]c[os", "servicios").Replace("( si]}", "( sii )")
                .Replace("serv]cios", "servicios").Replace("vlrtuales", "virtuales").Replace("representac]on", "representacion")
                .Replace("serv1cios", "servicios").Replace("a^", "").Replace("expedidoa", "expedido a").Replace("certnficado", "certificado")
                .Replace("( si[ )", "( sii )").Replace("neiva ds", "neiva").Replace("( si! )", "( sii )").Replace("verificac!6n", "verificacion")
                .Replace("codigodeverificacion", "codigo de verificacion").Replace("coiviercio", "comercio").Replace("decoiviercio", "de comercio")
                .Replace("af]liados", "afiliados").Replace("des]no", "destino").Replace("existenc]a", "existencia").Replace("( sit )", "( sii )")
                .Replace("tari ) ar", "tardar").Replace("cod go de ver f cac on", "codigo de verificacion").Replace("taaves", "traves")
                .Replace("certi]ficado", "certificado").Replace("afjliados", "afiliados").Replace("v ^ rtuales", "virtuales").Replace("verifucacion", "verificacion")
                .Replace("codigo de ver caci(*", "codigo de verificacion").Replace("ver ^ fj cachon", "verificacion").Replace("codigc", "codigo")
                .Replace("codigo de ver f cac 6n", "codigo de verificacion").Replace("certif ^ cado", "certificado").Replace("existen", "existencia").Replace("existencm", "existencia")
                .Replace("(s!1)", "( sii )").Replace("expemdo", "expedido").Replace("cert ^ ficado", "certificado").Replace("verifcacjoxj", "verificacion")
                .Replace("s!1}", "sii").Replace("afilbado ^ ',", "afiliado").Replace("exbtencm", "existencia").Replace("afil ^ ados", "afiliados")
                .Replace("afilbados", "afiliados").Replace("ver[ficacion", "verificacion").Replace("uraea", "uraba").Replace("bunvenyura", "buenaventura");
            
            return (fullOCR);
        }

        public string ChangeFullOCRFirst(string fullOCR)
        {
            fullOCR = fullOCR.Replace("trav^s", "traves")
                .Replace("N^mero", "numero")
                .Replace("c6digo de verificaci6n", "codigo de verificacion").Replace("codigo de verificaci6n", "codigo de verificacion").Replace("namero", "numero")
                .Replace("afiliaoos", "afiliados")
                .Replace("cert[ficado", "certificado")
                .Replace("codigo de verifica ( non", "codigo de verificacion").Replace("verif[cac[6n", "verificacion").Replace("verificac[on", "verificacion")
                .Replace("de· ", "de ")
                .Replace("lacamara", "la camara")
                .Replace("ref'ublica", "republica")
                .Replace("s,m.lm.v.", "s.m.l.m.v.")
                .Replace("la samara de comercio", "la camara de comercio")
                .Replace("proporc1ona", "proporciona")
                .Replace("mercant^", "mercantil")
                .Replace("ex]stencia", "existencia")
                .Replace("secretar[o", "secretario")
                .Replace("verifocac^on", "verificacion")
                .Replace("verifucacion", "verificacion").Replace("cod1go", "codigo").Replace("sancoones", "sanciones").Replace("proporcdna", "proporciona")
                .Replace("cal!", "cali").Replace("representaci6n", "representacion").Replace("c^mara", "camara").Replace("certif[cado", "certificado")
                .Replace("certif icado", "certificado").Replace("c6digo", "codigo").Replace("expe[ido", "expedido").Replace("sll", "sii").Replace("sh", "sii").Replace("( su )", "(sii)")
                .Replace("matr|cula", "matricula").Replace("am8s", "a mas").Replace("'1.", "").Replace("ex[stencia", "existencia").Replace("certif[cado", "certificado")
                .Replace("rr", "").Replace("r m", "").Replace("f 1", "").Replace("'..", "").Replace("f{% -", "").Replace("/ .", "").Replace("f»2", "")
                .Replace("f^.", "").Replace("ti 2", "").Replace("- cert^ificado", "certificado").Replace("bamara", "camara").Replace("s!1", "sii")
                .Replace("serv[cios", "servicios").Replace("monter^a", "monteria").Replace("comerc[o", "comercio").Replace("v[rtuales", "virtuales")
                .Replace("caiviara", "camara").Replace("servic]os", "servicios").Replace("expe]ido", "expedido").Replace("s]l", "sii").Replace("ex]stencia", "existencia")
                .Replace("vlrtuales", "virtuales").Replace("serv[c[os", "servicios").Replace("sil", "sii").Replace("( sid", "sii").Replace("s.m.l,m,\\ /", "s.m.l.m.v")
                .Replace("coiviercio", "comercio").Replace("s.m.l,m.v", "s.m.l.m.v").Replace("cddigo de verificaci6n", "codigo de verificacion").Replace("com€rcio de cartag€na", "comercio de cartagena")
                .Replace("trav6s", "traves").Replace("comerclo", "comercio").Replace("expe[idoa taraves", "expedido a traves").Replace("virt[ja^ - - - - - ··;", "virtuales ( sii )")
                .Replace("camera de comercio de casanam", "camara de comercio de casanare").Replace("del.portal", "del portal")
                .Replace("31 de marzo,y evite", "31 de marzo y evite").Replace("matr|cula", "matricula").Replace("y'confianza", "y confianza")
                .Replace("  ", " ").Replace("(sii)", "( sii )").Replace("sli", "sii").Replace("certif]cado", "certificado").Replace("exped[do", "expedido")
                .Replace("deexistencm", "de existencia").Replace("e ) ( istencia", "de existencia").Replace("comercic}", "comercio").Replace("comerc!0", "comercio")
                .Replace("gamara", "camara").Replace("s[i", "sii").Replace("cert'if[cado", "certificado").Replace("s,m.l.m.v", "s.m.l.m.v")
                .Replace("negoc[os", "negocios").Replace("travis", "traves").Replace("matr^cula", "matricula").Replace("girardol", "girardot").Replace("oe", "de")
                .Replace("p / 5^·%'", "").Replace("( sii )", " sii ").Replace(" sii ", " ( sii ) ").Replace("camara de comercio de girardot,", "camara de comercio de girardot")
                .Replace("  ( sii )  ", " ( sii ) ").Replace("atraves", "a traves").Replace("certficado", "certificado")
                .Replace("exbtencia", "existencia").Replace("representac[on", "representacion").Replace("ver f cac on", "verificacion").Replace("verifica ( non", "verificacion")
                .Replace("expeoido", "expedido").Replace("certihcado", "certificado").Replace("matrfcula", "matricula").Replace("certif'cado", "certificado")
                .Replace("matrjcula", "matricula").Replace("cod[go", "codigo").Replace("f,0", "").Replace("serv[cios", "servicios")
                .Replace("comerc!0", "comercio").Replace("representac[on", "representacion").Replace("s!1", "sii").Replace("cert]ficado", "certificado")
                .Replace("ver[ficaci6n", "verificacion").Replace("comerc[o", "comercio").Replace("v[rtuales", "virtuales").Replace("certif[cado", "certificado")
                .Replace("s][", "sii").Replace("v[rtuales", "virtuales").Replace("exped]do", "expedido").Replace("servic]os", "servicios")
                .Replace("( sii ) eomercia", "( sii )").Replace("exped[doa", "expedida").Replace("certif[cado", "certificado")
                .Replace("servic[os", "servicios").Replace("cert]ficado", "certificado").Replace("serv]c[os", "servicios").Replace("( si]}", "( sii )")
                .Replace("serv]cios", "servicios").Replace("vlrtuales", "virtuales").Replace("representac]on", "representacion")
                .Replace("serv1cios", "servicios").Replace("a^", "").Replace("expedidoa", "expedido a").Replace("certnficado", "certificado")
                .Replace("( si[ )", "( sii )").Replace("neiva ds", "neiva").Replace("( si! )", "( sii )").Replace("verificac!6n", "verificacion")
                .Replace("codigodeverificacion", "codigo de verificacion").Replace("coiviercio", "comercio").Replace("decoiviercio", "de comercio")
                .Replace("af]liados", "afiliados").Replace("des]no", "destino").Replace("existenc]a", "existencia").Replace("( sit )", "( sii )")
                .Replace("tari ) ar", "tardar").Replace("cod go de ver f cac on", "codigo de verificacion").Replace("taaves", "traves")
                .Replace("certi]ficado", "certificado").Replace("afjliados", "afiliados").Replace("v ^ rtuales", "virtuales").Replace("verifucacion", "verificacion")
                .Replace("codigo de ver caci(*", "codigo de verificacion").Replace("ver ^ fj cachon", "verificacion").Replace("codigc", "codigo")
                .Replace("codigo de ver f cac 6n", "codigo de verificacion").Replace("certif ^ cado", "certificado").Replace("existen", "existencia").Replace("existencm", "existencia")
                .Replace("(s!1)", "( sii )").Replace("expemdo", "expedido").Replace("cert ^ ficado", "certificado").Replace("verifcacjoxj", "verificacion")
                .Replace("s!1}", "sii").Replace("afilbado ^ ',", "afiliado").Replace("exbtencm", "existencia").Replace("afil ^ ados", "afiliados")
                .Replace("afilbados", "afiliados").Replace("ver[ficacion", "verificacion").Replace("uraea", "uraba").Replace("bunvenyura", "buenaventura");

            return (fullOCR);
        }

        public string getFullOCR_Change_Footer(ref int poscStartWrd, ref int poscEndWrd, string stringHeader, string fullOCR, ref bool bolIterateDocument)
        {
            string strLeftSide = "";
            string strRigthSide = "";

            if (poscStartWrd > 0 && poscEndWrd > 0)
            {
                if (!string.IsNullOrEmpty(stringHeader))
                {
                    if (poscStartWrd > 0)
                    {
                        strLeftSide = fullOCR.Substring(0, poscStartWrd);
                        if (fullOCR.Length > poscEndWrd)
                        {
                            strRigthSide = fullOCR.Substring(poscEndWrd, fullOCR.Length - poscEndWrd);
                        }
                        else
                        {
                            strRigthSide = fullOCR.Substring(poscStartWrd, (poscEndWrd - 1) - poscStartWrd);
                        }
                        fullOCR = string.Format("{0}{1}", strLeftSide, strRigthSide);
                    }
                    poscStartWrd = poscEndWrd + 1;
                }
            }
            else
            {
                bolIterateDocument = true;
            }
            return (fullOCR);
        }

        protected bool EsNumerico(string val)
        {
            return val.All(char.IsDigit);
            //Regex _isNumber = new Regex(@"^d+$");
            //Match m = _isNumber.Match(val);
            //return m.Success;
        }

        public List<List<CC_Parser.WordSearch>> GetDeliveryList(string fullOCR, List<List<string[]>> wordList, List<CC_Parser.WordSearch> listWordEval, string strKeyWordFind, List<List<WordSearch>> deliveryList, List<FieldsContextual> listFieldsContextuals)
        {
            int poscFirstKeyWord = 0, poscFirstKeyWord2 = 0, poscFirstKeyWordOK = 0;
            int poscStartWrd = 0;
            int poscEndWrd = 0;
            int poscPoint = 0;
            int posComma = 0;
            int poscPointComma = 0;
            int poscTwoPoint = 0;
            int poscSpace = 0;
            int poscEndParag = 0;
            int poscDelivery = 0;
            int indexFullOCR = 0;
            int sizeFullOCR = 0;
            int totalMatchOK = 0;
            int counterMatch = 0;
            int counterKeyWordsFinds = 0;
            int totalWordsLeft = 0;
            int totalWordsRigth = 0;
            int[] arrayMatch;

            string strLeftSide = "";
            string strRigthSide = "";
            string strReverseFullOCR = "";
            string strLeftSideFull = "";
            string strRigthSideFull = "";
            string firstKeyWord = "";
            string[] charSep = new string[] { ". ", ",", ";", ":", " ", "\r\n", "certifica" };

            bool bolExitExtraction = false;
            bool bolEnableContinueExtraction = false;
            bool bolContinueFindKeyWord = false;
            bool bolLoadFirstKeyWord = false;

            List<WordSearch> listWordRights = new List<WordSearch>();
            List<WordSearch> listWordLefts = new List<WordSearch>();
            List<WordSearch> listKeyWordsReturn = new List<WordSearch>();

            try
            {
                switch (strKeyWordFind)
                {
                    case "FacultadAvalar":
                        poscDelivery = 0;
                        break;

                    case "Monto":
                        poscDelivery = 1;
                        break;

                    case "Actividad":
                        poscDelivery = 2;
                        break;
                }

                #region Extracción campos: Facultad de Avalar/Monto/Actividad

                //Se carga la primer pablabra clave del array de facultad de avalar:
                //Se busca la primer coincidencia

                int index = -1;
                sizeFullOCR = fullOCR.Length;
                string strKeyWordFindNew = "";

                if (strKeyWordFind == "FacultadAvalar")
                {
                    strKeyWordFindNew = "Facultad de Avalar";
                }
                else {
                    strKeyWordFindNew = strKeyWordFind;
                }


                FieldsContextual fieldsContextual = (from fc in listFieldsContextuals
                                                    where fc.Field == strKeyWordFindNew
                                                    select fc).SingleOrDefault();

                if (fieldsContextual != null)
                {
                    totalWordsLeft = fieldsContextual.TotalWordLeft;
                    totalWordsRigth = fieldsContextual.TotalWordRight;
                }

                //Find the key word that more nearly to start the string or FullOCR:
                //ubicateKeyWord(string fullOCR, List<List<string[]>> wordList, int poscDelivery, int poscStartFindKW, ref int poscFirstKeyWord, ref string firstKeyWord)

                ubicateKeyWord(fullOCR, wordList, poscDelivery, 0, ref poscFirstKeyWord, ref firstKeyWord);

                if (totalWordsLeft > 0 && totalWordsRigth > 0)
                {
                    for (int idxFullOCR = 1; idxFullOCR <= sizeFullOCR - 1; idxFullOCR++)
                    {

                        if (poscFirstKeyWord >= 0)
                        {

                            string strLineOCRLeftEval = fullOCR.Substring(0, poscFirstKeyWord);
                            string[] diferenceListWordsLeft = strLineOCRLeftEval.Split(' ');

                            if (diferenceListWordsLeft.Count() >= totalWordsLeft)
                            {


                                if (counterKeyWordsFinds == 0)
                                {
                                    if (bolLoadFirstKeyWord)
                                    {
                                        string strLineOCREval = fullOCR.Substring(poscFirstKeyWordOK, poscFirstKeyWord - poscFirstKeyWordOK);
                                        string[] diferenceListWords = strLineOCREval.Split(' ');

                                        if (diferenceListWords.Count() > totalWordsRigth)
                                        {
                                            poscFirstKeyWordOK = poscFirstKeyWord;
                                            bolLoadFirstKeyWord = true;
                                            counterKeyWordsFinds = 0;
                                        }
                                    }
                                    else
                                    {
                                        poscFirstKeyWordOK = poscFirstKeyWord;
                                        bolLoadFirstKeyWord = true;
                                    }
                                }

                                listWordLefts = new List<WordSearch>();
                                listWordRights = new List<WordSearch>();

                                bolEnableContinueExtraction = false;

                                strReverseFullOCR = fullOCR.Substring(0, poscFirstKeyWord);
                                if (!string.IsNullOrEmpty(strReverseFullOCR))
                                {
                                    strReverseFullOCR = Reverse(strReverseFullOCR);

                                    poscStartWrd = 1;
                                    bolExitExtraction = false;
                                    indexFullOCR = poscFirstKeyWord;

                                    while (!bolExitExtraction)
                                    {
                                        poscPoint = strReverseFullOCR.IndexOf(charSep[0], poscStartWrd);
                                        posComma = strReverseFullOCR.IndexOf(charSep[1], poscStartWrd);
                                        poscPointComma = strReverseFullOCR.IndexOf(charSep[2], poscStartWrd);
                                        poscTwoPoint = strReverseFullOCR.IndexOf(charSep[3], poscStartWrd);
                                        poscSpace = strReverseFullOCR.IndexOf(charSep[4], poscStartWrd);
                                        poscEndParag = strReverseFullOCR.IndexOf(charSep[5], poscStartWrd);

                                        totalMatchOK = 0;
                                        if (poscPoint > 0)
                                        {
                                            if (poscPoint > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (posComma > 0)
                                        {
                                            if (posComma > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscPointComma > 0)
                                        {
                                            if (poscPointComma > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscTwoPoint > 0)
                                        {
                                            if (poscTwoPoint > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscSpace > 0)
                                        {
                                            if (poscSpace > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscEndParag > 0)
                                        {
                                            if (poscEndParag > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        arrayMatch = new int[] { };
                                        arrayMatch = new int[totalMatchOK];
                                        counterMatch = 0;

                                        if (poscPoint > 0)
                                        {
                                            arrayMatch[counterMatch] = poscPoint;
                                            counterMatch++;
                                        }

                                        if (posComma > 0)
                                        {
                                            arrayMatch[counterMatch] = posComma;
                                            counterMatch++;
                                        }

                                        if (poscPointComma > 0)
                                        {
                                            arrayMatch[counterMatch] = poscPointComma;
                                            counterMatch++;
                                        }

                                        if (poscTwoPoint > 0)
                                        {
                                            arrayMatch[counterMatch] = poscTwoPoint;
                                            counterMatch++;
                                        }

                                        if (poscSpace > 0)
                                        {
                                            arrayMatch[counterMatch] = poscSpace;
                                            counterMatch++;
                                        }

                                        if (poscEndParag > 0)
                                        {
                                            arrayMatch[counterMatch] = poscEndParag;
                                            counterMatch++;
                                        }

                                        poscEndWrd = arrayMatch[0];
                                        for (int idxM = 0; idxM < counterMatch; idxM++)
                                        {
                                            if (poscEndWrd > arrayMatch[idxM])
                                            {
                                                poscEndWrd = arrayMatch[idxM];
                                            }
                                        }

                                        if (Reverse(strReverseFullOCR.Substring(poscStartWrd, poscEndWrd - poscStartWrd)).ToLower() != "certifica")
                                        {
                                            if (!string.IsNullOrEmpty(Reverse(strReverseFullOCR.Substring(poscStartWrd, poscEndWrd - poscStartWrd)).ToLower()))
                                            {
                                                listWordLefts.Add(new WordSearch
                                                {
                                                    Word = Reverse(strReverseFullOCR.Substring(poscStartWrd, poscEndWrd - poscStartWrd)),
                                                    PositionStart = poscStartWrd
                                                });
                                            }
                                        }
                                        else
                                        {
                                            bolExitExtraction = true;
                                        }

                                        //if ((listWordLefts.Count == 10 && strKeyWordFind == "FacultadAvalar") || (listWordLefts.Count == 35 && strKeyWordFind == "Monto") || (listWordLefts.Count == 15 && strKeyWordFind == "Actividad"))
                                        if (listWordLefts.Count == totalWordsLeft)
                                        {
                                            bolExitExtraction = true;
                                        }

                                        poscStartWrd = poscEndWrd + 1;
                                    }

                                    if (listWordLefts.Count() > 0)
                                    {
                                        poscEndWrd = listWordLefts[listWordLefts.Count() - 1].PositionStart + listWordLefts[listWordLefts.Count() - 1].Word.Length;
                                        strLeftSideFull = fullOCR.Substring(indexFullOCR - poscEndWrd, poscEndWrd);
                                    }

                                    //Extraigo las siguientes 40 palabras, luego de la palabra clave ubicada:
                                    poscStartWrd = poscFirstKeyWord;
                                    bolExitExtraction = false;
                                    indexFullOCR = poscFirstKeyWord;

                                    while (!bolExitExtraction)
                                    {
                                        poscPoint = fullOCR.IndexOf(charSep[0], poscStartWrd);
                                        posComma = fullOCR.IndexOf(charSep[1], poscStartWrd);
                                        poscPointComma = fullOCR.IndexOf(charSep[2], poscStartWrd);
                                        poscTwoPoint = fullOCR.IndexOf(charSep[3], poscStartWrd);
                                        poscSpace = fullOCR.IndexOf(charSep[4], poscStartWrd);
                                        poscEndParag = fullOCR.IndexOf(charSep[5], poscStartWrd);

                                        totalMatchOK = 0;
                                        if (poscPoint > 0)
                                        {
                                            if (poscPoint > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (posComma > 0)
                                        {
                                            if (posComma > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscPointComma > 0)
                                        {
                                            if (poscPointComma > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscTwoPoint > 0)
                                        {
                                            if (poscTwoPoint > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscSpace > 0)
                                        {
                                            if (poscSpace > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        if (poscEndParag > 0)
                                        {
                                            if (poscEndParag > 0)
                                            {
                                                totalMatchOK++;
                                            }
                                        }

                                        arrayMatch = new int[] { };
                                        arrayMatch = new int[totalMatchOK];
                                        counterMatch = 0;

                                        if (poscPoint > 0)
                                        {
                                            arrayMatch[counterMatch] = poscPoint;
                                            counterMatch++;
                                        }

                                        if (posComma > 0)
                                        {
                                            arrayMatch[counterMatch] = posComma;
                                            counterMatch++;
                                        }

                                        if (poscPointComma > 0)
                                        {
                                            arrayMatch[counterMatch] = poscPointComma;
                                            counterMatch++;
                                        }

                                        if (poscTwoPoint > 0)
                                        {
                                            arrayMatch[counterMatch] = poscTwoPoint;
                                            counterMatch++;
                                        }

                                        if (poscSpace > 0)
                                        {
                                            arrayMatch[counterMatch] = poscSpace;
                                            counterMatch++;
                                        }

                                        if (poscEndParag > 0)
                                        {
                                            arrayMatch[counterMatch] = poscEndParag;
                                            counterMatch++;
                                        }

                                        poscEndWrd = arrayMatch[0];
                                        for (int idxM = 0; idxM < counterMatch; idxM++)
                                        {
                                            if (poscEndWrd > arrayMatch[idxM])
                                            {
                                                poscEndWrd = arrayMatch[idxM];
                                            }
                                        }

                                        if (fullOCR.Substring(poscStartWrd, poscEndWrd - poscStartWrd).ToLower() != "certifica")
                                        {
                                            if (!string.IsNullOrEmpty(fullOCR.Substring(poscStartWrd, poscEndWrd - poscStartWrd).ToLower()))
                                            {
                                                listWordRights.Add(new WordSearch
                                                {
                                                    Word = fullOCR.Substring(poscStartWrd, poscEndWrd - poscStartWrd),
                                                    PositionStart = poscStartWrd
                                                });
                                            }
                                        }
                                        else
                                        {
                                            bolExitExtraction = true;
                                        }

                                        //if ((listWordRights.Count == 40 && strKeyWordFind == "FacultadAvalar") || (listWordRights.Count == 15 && strKeyWordFind == "Monto") || (listWordRights.Count == 15 && strKeyWordFind == "Actividad"))
                                        if (listWordRights.Count == totalWordsRigth)
                                        {
                                            bolExitExtraction = true;
                                        }

                                        poscStartWrd = poscEndWrd + 1;
                                    }

                                    if (listWordRights.Count() > 0)
                                    {
                                        poscEndWrd = listWordRights[listWordRights.Count() - 1].PositionStart + listWordRights[listWordRights.Count() - 1].Word.Length;
                                        strRigthSideFull = fullOCR.Substring(indexFullOCR + firstKeyWord.Length, (poscEndWrd - (indexFullOCR + firstKeyWord.Length)));
                                    }

                                    setHomologateKeyWord(ref listWordLefts, wordList, poscDelivery);
                                    setHomologateKeyWord(ref listWordRights, wordList, poscDelivery);

                                    //Busco si se cumple la condición de que en el parrafo a procesarse si hayan por lo menos 2 palabras claves a la izquierda o a la derecha de la primer palabra encontrada, al inicio:                           
                                    var ExistsWordsLeftValid = listWordLefts.Join(listWordEval, a => a.Word, b => b.Word, (a, b) => new { a.Word, b.Word2 }).ToList();
                                    var ExistsWordsRigthValid = listWordRights.Join(listWordEval, a => a.Word, b => b.Word, (a, b) => new { a.Word, b.Word2 }).ToList();

                                    strReverseFullOCR = "";

                                    foreach (WordSearch wordSearch in listWordLefts)
                                    {
                                        if (!string.IsNullOrEmpty(strReverseFullOCR))
                                        {
                                            strReverseFullOCR = string.Format("{0} {1}", strReverseFullOCR, wordSearch.Word);
                                        }
                                        else
                                        {
                                            strReverseFullOCR = wordSearch.Word;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(strReverseFullOCR))
                                    {
                                        strReverseFullOCR = string.Format("{0} {1}", strReverseFullOCR, firstKeyWord);
                                    }

                                    foreach (WordSearch wordSearch in listWordRights)
                                    {
                                        if (!string.IsNullOrEmpty(strReverseFullOCR))
                                        {
                                            strReverseFullOCR = string.Format("{0} {1}", strReverseFullOCR, wordSearch.Word);
                                        }
                                        else
                                        {
                                            strReverseFullOCR = wordSearch.Word;
                                        }
                                    }

                                    //strReverseFullOCR = fullOCR.Substring(poscFirstKeyWord - strLeftSideFull.Length, (((poscFirstKeyWord + strRigthSideFull.Length) - poscFirstKeyWord) + strLeftSideFull.Length + firstKeyWord.Length));

                                    evaluateTotalKeyWords(strReverseFullOCR, wordList, poscDelivery, ref listKeyWordsReturn);


                                    /***********************************/

                                    var listGroupByWordsEnd = (from i in (from lv in listKeyWordsReturn
                                                                          select new { lv.Word })

                                                               group i by new { i.Word } into g

                                                               select new { g.Key.Word }).ToList();

                                    /***************************************/


                                    //El campo keyworldeval lo uso para calcular el porcentaje de confianza mínimo:

                                    if (listGroupByWordsEnd.Count() > 1)
                                    {
                                        if (ExistsWordsLeftValid.Count() + ExistsWordsRigthValid.Count() > 1)
                                        {
                                            //Habilito la extracción del campo Facultad de Avalar:
                                            if (!string.IsNullOrEmpty(strLeftSideFull) && !string.IsNullOrEmpty(strRigthSideFull))
                                            {
                                                string valueKeyWordExists = (from l in deliveryList[poscDelivery]
                                                                             where l.Word == fullOCR.Substring(poscFirstKeyWord - strLeftSideFull.Length, (((poscFirstKeyWord + strRigthSideFull.Length) - poscFirstKeyWord) + strLeftSideFull.Length + firstKeyWord.Length))
                                                                             select l.Word).SingleOrDefault();

                                                if (string.IsNullOrEmpty(valueKeyWordExists))
                                                {
                                                    deliveryList[poscDelivery].Add(new WordSearch
                                                    {
                                                        PositionStart = poscFirstKeyWord,
                                                        Word = fullOCR.Substring(poscFirstKeyWord - strLeftSideFull.Length, (((poscFirstKeyWord + strRigthSideFull.Length) - poscFirstKeyWord) + strLeftSideFull.Length + firstKeyWord.Length)),
                                                        //Word= strReverseFullOCR,
                                                        KeyWordEval = firstKeyWord,
                                                        totalWordLeft = listWordLefts.Count(),
                                                        totalWordRigth = listWordRights.Count()
                                                    }
                                                    );
                                                }

                                                counterKeyWordsFinds = counterKeyWordsFinds + 1;
                                                //\r\n
                                                bolEnableContinueExtraction = true;
                                            }
                                        }
                                    }
                                }

                                //Bloqueo este fragmento porque cambiare el avance de la busqueda de palabras claves:
                             
                                if (counterKeyWordsFinds == 2)
                                {
                                    counterKeyWordsFinds = 0;
                                    bolLoadFirstKeyWord = false;

                                    //if ((poscFirstKeyWordOK + (((poscFirstKeyWordOK + strRigthSideFull.Length) - poscFirstKeyWordOK) + strLeftSideFull.Length + firstKeyWord.Length)) > 0)
                                    if ((poscFirstKeyWordOK + strRigthSideFull.Length) > 0)
                                    {
                                        poscEndWrd = poscFirstKeyWordOK + strRigthSideFull.Length;
                                        //(poscFirstKeyWordOK + (((poscFirstKeyWordOK + strRigthSideFull.Length) - poscFirstKeyWordOK) + strLeftSideFull.Length + firstKeyWord.Length)) + 1;
                                    }
                                    else
                                    {
                                        listKeyWordsReturn = new List<WordSearch>();
                                        listKeyWordsReturn = deliveryList[poscDelivery];
                                        WordSearch wordSearch = new WordSearch();

                                        if (listKeyWordsReturn.Count() > 0)
                                        {
                                            wordSearch = new WordSearch();
                                            wordSearch = listKeyWordsReturn[listKeyWordsReturn.Count() - 1];
                                        }

                                        if (wordSearch != null)
                                        {
                                            poscEndWrd = wordSearch.PositionStart + wordSearch.Word.Length + 1;
                                        }
                                    }
                                }
                                else
                                {
                                    //if (bolEnableContinueExtraction)

                                    //if ((poscFirstKeyWord + (((poscFirstKeyWord + strRigthSideFull.Length) - poscFirstKeyWord) + strLeftSideFull.Length + firstKeyWord.Length)) > 0)
                                    //{
                                    if ((poscFirstKeyWord + strRigthSideFull.Length) > 0)
                                    {
                                        poscEndWrd = poscFirstKeyWord + strRigthSideFull.Length;
                                        //poscEndWrd = (poscFirstKeyWord + (((poscFirstKeyWord + strRigthSideFull.Length) - poscFirstKeyWord) + strLeftSideFull.Length + firstKeyWord.Length)) + 1;
                                    }
                                    else
                                    {
                                        listKeyWordsReturn = new List<WordSearch>();
                                        listKeyWordsReturn = deliveryList[poscDelivery];
                                        WordSearch wordSearch = new WordSearch();

                                        if (listKeyWordsReturn.Count() > 0)
                                        {
                                            wordSearch = new WordSearch();
                                            wordSearch = listKeyWordsReturn[listKeyWordsReturn.Count() - 1];
                                        }

                                        if (wordSearch != null)
                                        {
                                            poscEndWrd = wordSearch.PositionStart + wordSearch.Word.Length + 1;
                                        }
                                    }
                                    /*else
                                    {
                                        poscEndWrd = poscFirstKeyWord + firstKeyWord.Length + 1;
                                    }*/
                                }

                                if (poscEndWrd < fullOCR.Length)
                                {
                                    poscFirstKeyWord = 0;
                                    firstKeyWord = "";
                                    idxFullOCR = poscEndWrd;
                                    ubicateKeyWord(fullOCR, wordList, poscDelivery, poscEndWrd, ref poscFirstKeyWord, ref firstKeyWord);
                                }
                            }
                            else {
                                //The app into it with the firs key word is mor early to start the document:
                                poscEndWrd = poscFirstKeyWord + 1;
                                poscFirstKeyWord = 0;
                                firstKeyWord = "";
                                idxFullOCR = poscEndWrd;
                                ubicateKeyWord(fullOCR, wordList, poscDelivery, poscEndWrd, ref poscFirstKeyWord, ref firstKeyWord);
                            }
                        }
                        else
                        {
                            idxFullOCR = sizeFullOCR;
                        }
                    }

                }//End validation of total key words, left and rigth.

                #endregion

            }
            catch (Exception ex)
            {
                string errorr = ex.ToString();
            }

            return (deliveryList);
        }

        /// <summary>
        /// In this procedure its formmatter each contextual field, initially the block record th array con each word separate with space blank and nextly substring the fisrt part of field based the top of key words.
        /// After susbtring lade rigth with the same logic the lade left.
        /// </summary>
        /// <param name="fieldValue"></param>
        /// <param name="listWordEval"></param>
        /// <param name="listFieldsContextuals"></param>
        /// <returns></returns>
        public string GetFieldContextualFormat(string fieldValue, string typeFieldEval, List<CC_Parser.WordSearch> listWordEval, List<FieldsContextual> listFieldsContextuals)
        {
            //string strKeyWordFind, List<List<WordSearch>> deliveryList

            string result = "";
            string keyWordValue = "";
            string keyWordValueRev = "";
            string newKeyWordValue = "";
            string contentFieldLeft = "";
            string contentFieldRigth = "";
            int counterKeyWordLeft = 0;
            int counterKeyWordRigth = 0;
            int counterKeyWordMatch = 0;
            int poscFind = 0;
            int totalWordsLeft = 0;
            int totalWordsRigth = 0;
            int minPoscKeyWord = 0;
            bool IsKeyWordCorrect = false;
            FieldsContextual fieldsContextual = new FieldsContextual();
            string[] arrayKeyWordSplit = fieldValue.Split(' ');
            string[] arrayKeyWordEnd = new string[1];

            int[] arrayPositionKW;
            int poscKeyWordMatch = 0;

            foreach (FieldsContextual itemFieldsContextual in listFieldsContextuals)
            {
                newKeyWordValue = "";

                if (itemFieldsContextual.Field == "FacultadAvalar" || itemFieldsContextual.Field == "Facultad de Avalar" || itemFieldsContextual.Field == "Monto" || itemFieldsContextual.Field == "Actividad")
                {
                    if (typeFieldEval == itemFieldsContextual.Field)
                    {
                        FieldsContextual fieldsContextual2 = (from fc in listFieldsContextuals
                                                              where fc.Field == itemFieldsContextual.Field
                                                              select fc).SingleOrDefault();

                        if (fieldsContextual2 != null)
                        {
                            totalWordsLeft = fieldsContextual2.TotalWordLeft - 1;
                            totalWordsRigth = fieldsContextual2.TotalWordRight - 1;
                        }

                        arrayKeyWordSplit = fieldValue.Split(' ');

                        arrayPositionKW = new int[arrayKeyWordSplit.Count()];

                        if (arrayKeyWordSplit.Count() > (totalWordsLeft + totalWordsRigth + 1))
                        {
                            for (int indiceKW = 0; indiceKW < arrayKeyWordSplit.Count(); indiceKW++)
                            {
                                counterKeyWordLeft++;

                                //if (!IsKeyWordCorrect)
                                //{
                                foreach (CC_Parser.WordSearch itemWordSearch in listWordEval)
                                {
                                    keyWordValue = arrayKeyWordSplit[indiceKW];

                                    poscFind = 0;
                                    poscFind = keyWordValue.IndexOf("" + itemWordSearch.Word + "", 0);

                                    if (poscFind == -1)
                                    {
                                        poscFind = keyWordValue.IndexOf("" + itemWordSearch.Word + ",", 0);
                                    }

                                    if (poscFind == -1)
                                    {
                                        poscFind = keyWordValue.IndexOf("" + itemWordSearch.Word + ".", 0);
                                    }

                                    if (poscFind == -1)
                                    {
                                        poscFind = keyWordValue.IndexOf(itemWordSearch.Word + "", 0);
                                    }

                                    if (poscFind >= 0)
                                    {
                                        IsKeyWordCorrect = true;

                                        int[] queryFilteredKW = arrayPositionKW.Where(i => i == indiceKW).ToArray();

                                        if (queryFilteredKW.Count() == 0)
                                        {
                                            arrayPositionKW[counterKeyWordMatch] = indiceKW;
                                            counterKeyWordMatch++;
                                        }
                                    }
                                }                     
                            }

                            if (arrayPositionKW.Count() > 0)
                            {
                                counterKeyWordLeft = 0;
                                counterKeyWordRigth = 0;

                                minPoscKeyWord = arrayPositionKW[0];
                                if (arrayPositionKW.Count() > 1)
                                {
                                    for (int i = 1; i < arrayPositionKW.Count(); i++)
                                    {
                                        if (arrayPositionKW[i] > 0)
                                        {
                                            if (minPoscKeyWord > arrayPositionKW[i])
                                            {
                                                minPoscKeyWord = arrayPositionKW[i];
                                            }
                                        }
                                    }
                                }

                                if (minPoscKeyWord > 0)
                                {
                                    poscKeyWordMatch = minPoscKeyWord;
                                }

                                arrayKeyWordEnd = new string[poscKeyWordMatch];
                                int indiceArrayKWE = 0;

                                for (int indiceKWE = poscKeyWordMatch - 1; indiceKWE >= 0; indiceKWE--)
                                {
                                    if (counterKeyWordLeft <= totalWordsLeft)
                                    {
                                        arrayKeyWordEnd[indiceArrayKWE] = arrayKeyWordSplit[indiceKWE];
                                        counterKeyWordLeft++;
                                        indiceArrayKWE++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (arrayKeyWordEnd.Count() > 0)
                                {
                                    for (int indiceKWE = counterKeyWordLeft - 1; indiceKWE > 0; indiceKWE--)
                                    {
                                        contentFieldLeft = string.Format("{0} {1}", contentFieldLeft, arrayKeyWordEnd[indiceKWE]);
                                    }
                                }

                                counterKeyWordRigth = 0;

                                for (int indiceKWE = poscKeyWordMatch + 1; indiceKWE < arrayKeyWordSplit.Count(); indiceKWE++)
                                {
                                    if (counterKeyWordRigth <= totalWordsRigth)
                                    {
                                        contentFieldRigth = string.Format("{0} {1}", contentFieldRigth, arrayKeyWordSplit[indiceKWE]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (!string.IsNullOrEmpty(contentFieldLeft) && !string.IsNullOrEmpty(contentFieldRigth))
                                {
                                    newKeyWordValue = string.Format("{0} {1} {2}", contentFieldLeft.Trim(), arrayKeyWordSplit[poscKeyWordMatch], contentFieldRigth.Trim());
                                }
                            }
                        }
                        else
                        {
                            result = fieldValue;
                            break;
                        }

                        if (!string.IsNullOrEmpty(newKeyWordValue))
                        {
                            result = newKeyWordValue;
                        }
                    }
                }//end validation type group
            }

            if (!string.IsNullOrEmpty(newKeyWordValue))
            {
                result = newKeyWordValue;
            }

            return (result);
        }


        static int LevenshteinDistance(string original, string modified)
        {
            if (original == modified)
                return 0;

            int len_orig = original.Length;
            int len_diff = modified.Length;
            if (len_orig == 0 || len_diff == 0)

                return len_orig == 0 ? len_diff : len_orig;

            var matrix = new int[len_orig + 1, len_diff + 1];

            for (int i = 1; i <= len_orig; i++)
            {
                matrix[i, 0] = i;
                for (int j = 1; j <= len_diff; j++)
                {
                    int cost = modified[j - 1] == original[i - 1] ? 0 : 1;
                    if (i == 1)
                        matrix[0, j] = j;

                    var vals = new int[] {
                    matrix[i - 1, j] + 1,
                    matrix[i, j - 1] + 1,
                    matrix[i - 1, j - 1] + cost
                };
                    matrix[i, j] = vals.Min();
                    if (i > 1 && j > 1 && original[i - 1] == modified[j - 2] && original[i - 2] == modified[j - 1])
                        matrix[i, j] = Math.Min(matrix[i, j], matrix[i - 2, j - 2] + cost);

                }
            }
            return matrix[len_orig, len_diff];
        }

        public DataTable ConvertToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection properties =
               TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        public void setReplaceHeaderFullOCR(ref string fullOCR, ref string fullOCRProcess, ref bool enableDropHeader, HeaderCompany itemHeader, string[] arrayFullOCR, ref string[] arrayHeader, int modulo)
        {
            int contadorFuzzyM = 0;
            int contadorVirgulilla = 0;
            string lineEvalExec = "";
            string[] arrayHeaderAux = null;
            string[] arrayVirgulilla = null;
            string[] arrayVirgulillaAux = null;
            string[] arrayVirgulillaEnd = null;
            bool enableContinue = false;

            string stringHeader = "";
            double distHeader = 0;
            double fuzzyMatch = 0;
            double[] fuzzyMatchResult = new double[1];
            double lengthFM = 0;
            int length = 0;
            string strCamaraComercioActive = "";
            int positionHeader = 0;

            enableContinue = false;

            if (modulo == 1)
            {
                itemHeader.DescriptionHeader = string.Format("{0}|{1}", itemHeader.DescriptionHeader, itemHeader.City.ToLower());
            }

            arrayHeaderAux = itemHeader.DescriptionHeader.Split('|');

            if (arrayHeaderAux.Count() > 0)
            {
                for (int ind = 0; ind < arrayHeaderAux.Count(); ind++)
                {
                    arrayHeaderAux[ind] = arrayHeaderAux[ind].Replace("\r\n", "");
                    arrayHeaderAux[ind] = arrayHeaderAux[ind].Replace("\n", "");
                }

                fuzzyMatchResult = new double[arrayHeaderAux.Count()];

                if (itemHeader.DescriptionHeader.IndexOf("~", 0) > -1)
                {
                    foreach (string lineEval in arrayHeaderAux)
                    {
                        lineEvalExec = lineEval;
                        lineEvalExec = lineEvalExec.Replace("[", "");
                        lineEvalExec = lineEvalExec.Replace("]", "");

                        arrayVirgulilla = null;

                        if (lineEvalExec.IndexOf("~", 0) >= 0)
                        {
                            arrayVirgulilla = lineEvalExec.Split('~');
                        }

                        try
                        {
                            if (arrayVirgulilla.Count() == 0)
                            {
                                arrayVirgulillaAux = new string[1];
                                arrayVirgulillaAux[0] = lineEval;
                            }
                            else
                            {
                                contadorVirgulilla = 0;
                                arrayVirgulillaAux = new string[arrayVirgulilla.Count() + 1];
                                lineEvalExec = arrayHeaderAux[contadorFuzzyM - 1];
                                arrayVirgulillaAux[0] = lineEvalExec;

                                foreach (string lineExtract in arrayVirgulilla)
                                {
                                    arrayVirgulillaAux[contadorVirgulilla] = lineExtract;

                                    contadorVirgulilla++;
                                }
                            }

                            if (contadorVirgulilla > 0)
                            {
                                enableContinue = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            //arrayVirgulillaAux = new string[1];
                            //arrayVirgulillaAux[0] = lineEval;
                        }
                    }
                }

                if (!enableContinue)
                {
                    arrayVirgulillaAux = arrayHeaderAux;
                }

                foreach (string lineExtract in arrayVirgulillaAux)
                {
                    if (!string.IsNullOrEmpty(lineExtract))
                    {
                        if (lineExtract.Contains(' '))
                        {
                            arrayVirgulillaEnd = lineExtract.Split(' ');
                        }
                        else
                        {
                            arrayVirgulillaEnd = new string[1];
                            arrayVirgulillaEnd[0] = lineExtract;
                        }

                        foreach (string lineExtract2 in arrayVirgulillaEnd)
                        {
                            foreach (string lineExtract3 in arrayFullOCR)
                            {
                                if (lineExtract2.Length == lineExtract3.Length)
                                {
                                    stringHeader = lineExtract2;
                                    distHeader = LevenshteinDistance(stringHeader.ToLower(), lineExtract3);
                                    fuzzyMatch = 0;
                                    lengthFM = 0;

                                    if (stringHeader.Length > lineExtract3.Length)
                                    {
                                        lengthFM = stringHeader.Length;
                                    }
                                    else
                                    {
                                        lengthFM = lineExtract3.Length;
                                    }

                                    fuzzyMatch = (distHeader / lengthFM);
                                    fuzzyMatch = fuzzyMatch * 100;

                                    if (fuzzyMatch > 0 && fuzzyMatch <= 17)
                                    {
                                        if (!lineExtract2.Contains(",") && !lineExtract2.Contains(":") && !lineExtract2.Contains(";") && !lineExtract2.Contains("."))
                                        {
                                            if (!lineExtract3.Contains(",") && !lineExtract3.Contains(":") && !lineExtract3.Contains(";") && !lineExtract3.Contains("."))
                                            {
                                                fullOCR = fullOCR.Replace(lineExtract3, lineExtract2);
                                                fullOCRProcess = fullOCR;
                                            }
                                        }
                                    }
                                }
                            }
                            contadorFuzzyM++;
                        }
                    }
                }//Fin bloque proceso de busqueda difusa

                enableContinue = false;
            }
            else
            {
                enableDropHeader = true;
            }

            arrayHeader = new string[arrayHeaderAux.Count() - 1];

            for (int ind = 0; ind < arrayHeaderAux.Count() - 1; ind++)
            {
                arrayHeader[ind] = arrayHeaderAux[ind];
            }

        }// end get header...


        public void ubicateKeyWord(string fullOCR, List<List<string[]>> wordList, int poscDelivery, int poscStartFindKW, ref int poscFirstKeyWord, ref string firstKeyWord)
        {
            bool bolContinueFindKeyWord = true;
            int counterMatch = 0;
            int poscFindKeyWord = 0;
            int minPoscKeyWord = 0;
            int incFindKeyWord = 0;
            int[] arrayPositionKW;
            string[] arrayKeyWords;

            foreach (string keyWord in wordList[poscDelivery][0])
            {
                counterMatch++;
            }

            arrayPositionKW = new int[counterMatch];
            arrayKeyWords = new string[counterMatch];

            foreach (string firstKeyWordAux in wordList[poscDelivery][0])
            {
                poscFirstKeyWord = 0;
                poscFirstKeyWord = fullOCR.IndexOf(" " + firstKeyWordAux + " ", poscStartFindKW);

                if (poscFirstKeyWord == -1)
                {
                    poscFirstKeyWord = fullOCR.IndexOf(" " + firstKeyWordAux + ", ", poscStartFindKW);
                }

                if (poscFirstKeyWord == -1)
                {
                    poscFirstKeyWord = fullOCR.IndexOf(" " + firstKeyWordAux + ". ", poscStartFindKW);
                }

                if (poscFirstKeyWord == -1)
                {
                    poscFirstKeyWord = fullOCR.IndexOf(firstKeyWordAux + " ", poscStartFindKW);
                }

                if (poscFirstKeyWord == -1)
                {
                    poscFirstKeyWord = fullOCR.IndexOf(firstKeyWordAux + ", ", poscStartFindKW);
                }

                if (poscFirstKeyWord >= 0)
                {
                    arrayPositionKW[incFindKeyWord] = poscFirstKeyWord;
                    arrayKeyWords[incFindKeyWord] = firstKeyWordAux;
                    incFindKeyWord++;
                }
            }

            if (arrayPositionKW.Count() > 0)
            {
                minPoscKeyWord = arrayPositionKW[0];

                if (arrayPositionKW.Count() > 1)
                {
                    for (int i = 1; i < arrayPositionKW.Count(); i++)
                    {
                        if (arrayPositionKW[i] > 0) {
                            if (minPoscKeyWord > arrayPositionKW[i])
                            {
                                minPoscKeyWord = arrayPositionKW[i];
                            }
                        }
                    }
                }

                if (minPoscKeyWord > 0)
                {
                    poscFindKeyWord = 0;
                    for (int i = 0; i < arrayPositionKW.Count(); i++)
                    {
                        if (arrayPositionKW[i] > 0)
                        {
                            if (minPoscKeyWord == arrayPositionKW[i])
                            {
                                poscFindKeyWord = i;
                                i = arrayPositionKW.Count();
                            }
                        }
                    }
                    firstKeyWord = arrayKeyWords[poscFindKeyWord];
                    poscFirstKeyWord = arrayPositionKW[poscFindKeyWord];
                }
            }

        }

        public void evaluateTotalKeyWords(string strBlockParagraphe, List<List<string[]>> wordList, int poscDelivery, ref List<WordSearch> listKeyWordsReturn)
        {
            int counterMatch = 0;
            int poscStartFindKW = 0;
            int poscFirstKeyWord = 0;
            int poscEndFinder = 0;
            string firstKeyWord = "";
            string strKeyWordsExists = "";
            
            List<WordSearch> listKeyWordsProcess = new List<WordSearch>();
            
            foreach (string firstKeyWordAux in wordList[poscDelivery][0])
            {
                poscFirstKeyWord = 0;
                poscStartFindKW = 0;
                poscEndFinder = 0;

                while (poscEndFinder < strBlockParagraphe.Length)
                {
                    poscFirstKeyWord = strBlockParagraphe.IndexOf(" " + firstKeyWordAux + " ", poscStartFindKW);

                    if (poscFirstKeyWord == -1)
                    {
                        poscFirstKeyWord = strBlockParagraphe.IndexOf(" " + firstKeyWordAux + ", ", poscStartFindKW);
                    }

                    if (poscFirstKeyWord == -1)
                    {
                        poscFirstKeyWord = strBlockParagraphe.IndexOf(" " + firstKeyWordAux + ". ", poscStartFindKW);
                    }

                    if (poscFirstKeyWord == -1)
                    {
                        poscFirstKeyWord = strBlockParagraphe.IndexOf(firstKeyWordAux + " ", poscStartFindKW);
                    }

                    if (poscFirstKeyWord == -1)
                    {
                        poscFirstKeyWord = strBlockParagraphe.IndexOf(firstKeyWordAux + ", ", poscStartFindKW);
                    }

                    if (poscFirstKeyWord >= 0)
                    {
                        poscStartFindKW = poscFirstKeyWord;
                        listKeyWordsProcess.Add(new WordSearch() { Word = firstKeyWordAux });
                        poscEndFinder = poscStartFindKW + firstKeyWordAux.Length + 1;
                        poscStartFindKW = poscEndFinder;
                    }
                    else
                    {
                        poscEndFinder = strBlockParagraphe.Length;
                    }
                }
            }
            listKeyWordsReturn = listKeyWordsProcess;
        }

        public void setReplaceParagrapher(ref string fullOCR, string[] arrayFullOCR/*ref List<WordSearch> listWordSearch*/, List<List<string[]>> wordList, int poscDelivery)
        {
            string stringHeader = "";
            double distHeader = 0;
            double fuzzyMatch = 0;
            double[] fuzzyMatchResult = new double[1];
            double lengthFM = 0;
            string stringWord = "";
            string stringWordEnd = "";
            int countListWS = 0;

            List<KeyWordsPorcentages> listAnalizeKW = new List<KeyWordsPorcentages>();
            
            if (arrayFullOCR.Count() > 0)
            {
                foreach (string itemFullOCR in arrayFullOCR)
                {
                    foreach (string firstKeyWord in wordList[poscDelivery][0])
                    {
                        stringWord = itemFullOCR;
                        stringWord = stringWord.Trim();

                        if (stringWord.ToLower().Trim().IndexOf("valares", 0) >= 0 && firstKeyWord=="valores")
                        {
                            countListWS++;
                        }

                        if ((stringWord.Length == firstKeyWord.Length && (stringWord.IndexOf(",") == -1 && stringWord.IndexOf(".") == -1))
                            || stringWord.Length > firstKeyWord.Length
                            )
                        {
                            if (stringWord.Length == firstKeyWord.Length
                            || ((stringWord.Length > firstKeyWord.Length) && stringWord.Substring(0, firstKeyWord.Length + 1).Substring(firstKeyWord.Length, 1) == ",")
                            || ((stringWord.Length > firstKeyWord.Length) && stringWord.Substring(0, firstKeyWord.Length + 1).Substring(firstKeyWord.Length, 1) == ".")
                            )
                            {
                                if (stringWord.Length > firstKeyWord.Length)
                                {
                                    stringWord = stringWord.Substring(0, firstKeyWord.Length);
                                }

                                stringHeader = firstKeyWord;
                                distHeader = LevenshteinDistance(stringHeader.ToLower(), stringWord);
                                fuzzyMatch = 0;
                                lengthFM = 0;

                                if (stringHeader.Length > stringWord.Length)
                                {
                                    lengthFM = stringHeader.Length;
                                }
                                else
                                {
                                    lengthFM = stringWord.Length;
                                }

                                fuzzyMatch = (distHeader / lengthFM);
                                fuzzyMatch = fuzzyMatch * 100;


                                if (stringWord.IndexOf("licitos") >= 0 && firstKeyWord == "licitos")
                                {
                                    int contadorllll = 0;
                                }


                                if (fuzzyMatch >= 0 && fuzzyMatch <= 15) //Se reemplaza las palabras claves exsitentes con un porcentaje de confianza de por lo menos un 85%:
                                {
                                    //fullOCR = fullOCR.Replace(stringWord, firstKeyWord);

                                    listAnalizeKW.Add(new KeyWordsPorcentages() {
                                                     KeyWordEval = stringWord,
                                                     KeyWordSource = firstKeyWord,
                                                     Porcentage = fuzzyMatch
                                                     });
                                }
                            }
                        }
                    }
                }

                //Update the fullOCR with the value correct of key word, after find it:

                countListWS = 0;
                foreach (KeyWordsPorcentages item in listAnalizeKW)
                {
                    
                    countListWS = 0;
                    countListWS = (from kw in listAnalizeKW
                                   where kw.KeyWordEval == item.KeyWordEval
                                   select kw).ToList().Count();
                    

                    KeyWordsPorcentages queryKeyWordSingle = listAnalizeKW.OrderBy(w => w.Porcentage)
                                              .Where(w => w.KeyWordEval == item.KeyWordEval).FirstOrDefault();


                    if (queryKeyWordSingle != null)
                    {
                        if (countListWS > 0)
                        {
                            if (!(queryKeyWordSingle.Porcentage == 0 && countListWS == 1))
                            {
                                if (queryKeyWordSingle.Porcentage > 0)
                                {
                                    fullOCR = fullOCR.Replace(queryKeyWordSingle.KeyWordEval, queryKeyWordSingle.KeyWordSource);
                                }
                            }
                        }
                    }

                }
            }
        }// end get header...


        public string setReplaceCharacterSpecials(string stringInto)
        {
            return (stringInto.Replace(",","").Replace(".","").Replace("/","").Replace(":","").Replace(";",""));
        }

        public void setHomologateKeyWord(ref List<WordSearch> listWordSearch, List<List<string[]>> wordList, int poscDelivery)
        {
            int countListWS = 0;
            
            foreach (string firstKeyWord in wordList[poscDelivery][0])
            {
                countListWS = 0;
                foreach (WordSearch wordSearch in listWordSearch)
                {
                    if (wordSearch.Word.IndexOf(firstKeyWord, 0) >= 0)
                    {
                        listWordSearch[countListWS].Word = firstKeyWord;
                        listWordSearch[countListWS].Word2 = firstKeyWord;
                    }
                    countListWS++;
                }
            }

        }

        public static string ExecutionDirectoryPathName
        {
            get
            {
                var dirPath = Assembly.GetExecutingAssembly().Location;
                dirPath = Path.GetDirectoryName(dirPath);
                return dirPath + @"\";
            }
        }
        
    }


    class ConnectionStringFactory
    {
        internal static string BuildModelConnectionString(string connectionString)
        {
            var builder = new EntityConnectionStringBuilder
            {
                Provider = "System.Data.SqlClient",
                Metadata = @"res://*/BDCamaraComercio.csdl|res://*/BDCamaraComercio.ssdl|res://*/BDCamaraComercio.msl", //@"your metadata string",
                ProviderConnectionString = connectionString
            };
            return builder.ConnectionString;
        }
    }
}
