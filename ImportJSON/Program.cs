using cdn_api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static ExportJSON.Item;
using static ExportJSON.Korekta;

namespace ImportJSON
{
    class Program
    {
        private static int XLWersjaAPI = 20232;
        private static int XLTrybWsadowy = 1;
        private static int XLWinieta = -1;
        private static int XLUtworzWlasnaSesje = 1;
        private static string XLSQLServer = "192.168.4.24";
        private static string XLSQLDatabase = "Termedia";//"ERPXL_PoloMarket"
        private static string XLUser = "ADMIN"; //
        private static string XLPass = "NE!w0rk12";//"Dupa2003!";
        private static string connectionString = @"Data Source=192.168.4.24;Initial Catalog=ERPXL_Termedia; User ID=onesupport;Password=a956!2A@";
        static async Task Main()
        {

            TextWriter tw2 = new StreamWriter(@"C:\OneSupport\FSK_JSON\log.txt", append: true);
            tw2.WriteLine();
            tw2.WriteLine();
            tw2.WriteLine();
            tw2.WriteLine();
            tw2.WriteLine("Hello " + DateTime.Now.ToString("f"));
            //tw2.WriteLine();
            tw2.WriteLine();
            try
            {
                int sesja = 0;

                string korektyQuery = "SELECT  Oko_ID,OKo_GidTyp , OKo_GidNumer, OKo_IDTermediaFS,  OKo_DataWystawienia, OKo_DataSprzedazy,  OKo_NumerFSK FROM [CDN].[ONE_Korekty] where OKo_IDTermediaFSK is NULL or OKo_IDTermediaFSK = 0";
                List<ListaKorekt> ListKorekty = SelectListKorekty(korektyQuery, "cdn.ONE_Korekty", connectionString, tw2);

                // logowanie(ref sesja, tw2);

                foreach (ListaKorekt korekta in ListKorekty)
                {
                    tw2.WriteLine("Przesyłanie korekty " + korekta.NumerFSK);

                    string korektaItemsQuery = "SELECT fsk_elem.TrE_TwrNumer, fsk_elem.Tre_Ilosc, fsk_elem.TrE_StawkaPod, fsk_elem.TrE_Cena, fsk_elem.TrE_CenaPoRabacie, fs_elem.TrE_Ilosc "
                                                + " FROM cdn.TraElem as fsk_elem join cdn.TraNag as fsk on fsk_elem.TrE_GIDTYP = fsk.TrN_GIDTyp and fsk_elem.TrE_GIDNumer = fsk.TRN_GIDNumer "
                                                + " join cdn.TraNag as fs on fs.TrN_GIDNumer = fsk.TrN_ZwrNumer and fs.TrN_GIDTyp = fsk.TrN_ZwrTyp "
                                                + " join cdn.TraElem as fs_elem on fs_elem.Tre_TwrNumer = fsk_elem.TrE_TwrNumer and fs.TrN_GIDNumer = fs_elem.TrE_GIDNumer " 
                                                + " and fs.TrN_GIDTyp = fs_elem.TRe_GIDTyp where (fsk_elem.TrE_GIDTyp=" +korekta.GIDTyp+" AND fsk_elem.TrE_GIDNumer="+korekta.GIDNumer+")";
                    //tw2.WriteLine(korektaItemsQuery);
                    List<ListaItems> ListItems = SelectListItems(korektaItemsQuery, "cdn.TraElem", connectionString, tw2);

                    Elem[] elemy = new Elem[ListItems.Count];
                    int i = 0;
                    foreach (ListaItems item in ListItems)
                    {

                        tw2.WriteLine("Dodawanie pozycji: " + item.TwrNumer);
                        //tw2.WriteLine("Ilość :" + item.Ilosc);
                        Elem elem = new Elem();
                        elem.Gid_numer = item.TwrNumer;
                        elem.Ilosc = item.Ilosc.Trim() == "0,0000" ? item.FsIlosc.Replace(",", "."): item.Ilosc.Replace(",", ".");
                        elem.Podatek = item.Podatek.Replace(",", ".");
                        elem.Cena = item.CenaRabat.Replace(",", ".");
                        elem.Cena_rabt = item.CenaRabat.Replace(",", ".");
                        elemy[i] = elem;
                        i++;
                        //tw2.WriteLine("Ilość Replace(",", ".") + (Convert.ToInt32(item.Ilosc) == 0 ? item.FsIlosc.Replace(",", ".") : item.Ilosc.Replace(",", ".")));


                    }

                    Faktura faktura = new Faktura();
                    Nag nag = new Nag();
                    nag.NumerFSK = korekta.NumerFSK;
                    nag.Data_wys = Convert.ToDateTime(korekta.DataWys).ToString("yyyy-MM-dd");
                    nag.Data_sprzedazy = Convert.ToDateTime(korekta.DataSprze).ToString("yyyy-MM-dd");
                    faktura.Nag = nag;
                    faktura.Elemy = elemy;

                    Dane dane = new Dane();
                    dane.InvoiceId = Convert.ToInt32(korekta.IDTermediaFS);
                    dane.Status = 2;
                    dane.Message = "";
                    dane.Faktura = faktura;

                    



                    string json = await Http_JSON_Response(true, dane, tw2);
                    string idTermedia = json.Substring(json.IndexOf("id") + 5, json.IndexOf("}") - (json.IndexOf("id") + 6));
                    tw2.WriteLine("Info Zwrotne ID : " + idTermedia);


                    string updateQuery = "UPDATE [CDN].[ONE_Korekty] SET [OKo_IDTermediaFSK] = '" + idTermedia + "' WHERE Oko_ID = " + korekta.ID;
                    Update(updateQuery, tw2);



                    if (json.Replace(" ", "").Contains("\"code\":200"))
                    {
                        tw2.WriteLine("Dodawanie atrybutu");
                        logowanie(ref sesja, tw2);

                        XLAtrybutInfo_20232 atr2 = new XLAtrybutInfo_20232();
                        atr2.Wersja = XLWersjaAPI;
                        atr2.GIDFirma = 1433857;
                        atr2.GIDLp = 0;
                        atr2.GIDNumer = Convert.ToInt32(korekta.GIDNumer);
                        atr2.GIDTyp = Convert.ToInt32(korekta.GIDTyp);
                        atr2.GIDSubLp = 0;
                        atr2.Klasa = "ID_Termedia";
                        atr2.Wartosc = idTermedia;
                        int blad = cdn_api.cdn_api.XLDodajAtrybut(sesja, atr2);
                        if (blad != 0)
                        {
                            tw2.WriteLine("Błąd dodawania atrybutu ID_Termedia " + idTermedia + ": " + blad);

                        }
                        wylogowanie(sesja);

                        
                    }
                    /*if (gidNumer != "")
                    {
                        tw2.WriteLine("Dodano indeks o gid numerze: " + gidNumer);
                        string updateQuery = "UPDATE [CDN].[ONE_Indeksy] SET [OIn_GidNumer] = '" + gidNumer + "' WHERE OIn_ID = " + indeks.ID;
                        Update(updateQuery, tw2);


                    }*/
                }

                //wylogowanie(sesja);

                tw2.Close();
            }
            catch (Exception ex)
            {
                tw2.WriteLine("Błąd funkcji: " + ex.Message);
                tw2.Close();
            }

        }


        static public async Task<string> Http_JSON_Response(bool SaveToFile, Dane data, TextWriter tw2)
        {
            try
            {

                Dane[] tabDane = new Dane[1];
                tabDane[0] = data;
                string json = "";
                InfoZwrotne dane = new InfoZwrotne();
                ServicePointManager.ServerCertificateValidationCallback +=
    (sender, cert, chain, sslPolicyErrors) => { return true; };

                System.Net.ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12 |
            SecurityProtocolType.Tls11 |
            SecurityProtocolType.Tls;

                tw2.WriteLine("Przesyłanie korekty " + data.Faktura.Nag.NumerFSK);


                using (var client = new HttpClient())
                {
                    //client.BaseAddress = new Uri("https://www.termedia.pl");
                    client.DefaultRequestHeaders.Clear();
                    //client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("pl-PL"));

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Add("apikey", "f14dd151-b503-41de-a7a0-879d7aa12d44");
                    //StringContent queryString = new StringContent(data)
                    //response.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    var jsonFile = JsonConvert.SerializeObject(tabDane);
                    TextWriter test = new StreamWriter(@"C:\OneSupport\FSK_JSON\testJSON.txt");
                    test.WriteLine(jsonFile);
                    test.Close();



                    var content = new StringContent(jsonFile, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(
                "https://www.termedia.pl/api/invoices",
                content);
                    //HttpContent responseContent = response.Content;
                    //var response = await client.PostAsync();//("/api/invoices", );

                    // 
                    HttpContent responseContent = response.Content;
                    tw2.WriteLine("Response: " + response.ToString());
                    //var baselinker_response = new Baselinker_response();
                    using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                    {

                        json = await reader.ReadToEndAsync();
                        dane = InfoZwrotne.FromJson(json);
                        // baselinker_response = Baselinker_response.FromJson(json);
                        if (SaveToFile == true)
                        {
                            TextWriter tw = new StreamWriter(@"C:\OneSupport\FSK_JSON\Input.txt");
                            try
                            {

                                tw.WriteLine(json);
                                tw.Close();
                            }
                            catch (Exception e)
                            {
                                tw.WriteLine("Błąd zapisu: " + e.Message);
                                tw.Close();
                            }
                        }


                    }
                    client.Dispose();



                    /*if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        return Request.CreateResponse(HttpStatusCode.OK, result);
                    }

                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "fail");*/
                }
                //return dane;
                return json;
            }
            catch (Exception e)
            {//MessageBox.Show("Błąd wywołania funkcji POST: " + e.Message);
                tw2.WriteLine("Błąd wywołania funkcji POST: " + e.Message);
                // return new InfoZwrotne();
                return "";
            }
        }




        public static List<ListaKorekt> SelectListKorekty(string query, string srcTableName, string connection_string, TextWriter tw2)
        {
            List<ListaKorekt> str = new List<ListaKorekt>();
            string sttr = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, srcTableName);
                    foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                    {

                        str.Add(new ListaKorekt()
                        {
                            ID = row[0].ToString(),
                            GIDTyp = row[1].ToString(),
                            GIDNumer = row[2].ToString(),
                            IDTermediaFS = row[3].ToString(),
                            DataWys = row[4].ToString(),
                            DataSprze = row[5].ToString(),
                            NumerFSK = row[6].ToString()
                        });


                    }
                }


            }
            catch (Exception ex)
            {
                tw2.WriteLine(sttr = sttr + "Bład SQL:" + ex.ToString() + "\n");
            }
            return str;
        }


        public static List<ListaItems> SelectListItems(string query, string srcTableName, string connection_string, TextWriter tw2)
        {
            List<ListaItems> str = new List<ListaItems>();
            string sttr = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, srcTableName);
                    foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                    {

                        str.Add(new ListaItems()
                        {
                            TwrNumer = row[0].ToString(),
                            Ilosc = row[1].ToString(),
                            Podatek = row[2].ToString(),
                            Cena = row[3].ToString(),
                            CenaRabat = row[4].ToString(),
                            FsIlosc = row[5].ToString()
                        }) ;


                    }
                }


            }
            catch (Exception ex)
            {
                tw2.WriteLine(sttr = sttr + "Bład SQL:" + ex.ToString() + "\n");
            }
            return str;
        }


        public static string Update(string sql, TextWriter tw2)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    connection.Open();
                    adapter.UpdateCommand = new SqlCommand(sql, connection);
                    adapter.UpdateCommand.ExecuteNonQuery();
                    //MessageBox.Show("Row updated !! ");
                }
            }
            catch (Exception ex)
            {
                tw2.WriteLine(ex.ToString());
            }
            return sql;
        }

        
        static public void logowanie(ref int sesja, TextWriter tw2)
        {
            XLLoginInfo_20232 nowyUzytkownik = new XLLoginInfo_20232();
            nowyUzytkownik.Wersja = XLWersjaAPI;
            nowyUzytkownik.Winieta = XLWinieta;
            nowyUzytkownik.TrybWsadowy = XLTrybWsadowy;
            nowyUzytkownik.UtworzWlasnaSesje = XLUtworzWlasnaSesje;
            nowyUzytkownik.SerwerKlucza = XLSQLServer;
            nowyUzytkownik.Baza = XLSQLDatabase;
            nowyUzytkownik.OpeIdent = XLUser;
            nowyUzytkownik.OpeHaslo = XLPass;
            nowyUzytkownik.PortHTTPSerweraKlucza = 1;
            nowyUzytkownik.RodzajSerweraKlucza = 0;
            nowyUzytkownik.SerwerKlucza = "localhost::5000185870";
            nowyUzytkownik.ProgramID = "ONE-045 FSK -> JSON";

            int blad = cdn_api.cdn_api.XLLogin(nowyUzytkownik, ref sesja);

            if (blad != 0)
            {
                tw2.WriteLine("Błąd logowania: " + blad);
            }

        }

        static public void wylogowanie(int sesja)
        {
            cdn_api.cdn_api.XLLogout(sesja);
        }
    }

    
}
