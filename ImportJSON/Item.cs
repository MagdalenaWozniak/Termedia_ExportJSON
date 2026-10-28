using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportJSON
{
    class Item
    {
        public struct ListaItems
        {
            public string TwrNumer { set; get; }
            public string Ilosc { set; get; }
            public string Podatek { set; get; }
            public string Cena { set; get; }
            public string CenaRabat { set; get; }
            public string FsIlosc { set; get; }
        }
    }
}
