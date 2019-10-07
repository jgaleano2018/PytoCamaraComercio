using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC_Parser
{
    public class ExtractResult
    {
        private string[] facultad;
        private string[] monto;
        private string[] actividad;
        private string fullOCR;

        public string[] Facultad
        {
            get
            {
                return facultad;
            }

            set
            {
                facultad = value;
            }
        }

        public string[] Monto
        {
            get
            {
                return monto;
            }

            set
            {
                monto = value;
            }
        }

        public string[] Actividad
        {
            get
            {
                return actividad;
            }

            set
            {
                actividad = value;
            }
        }

        public string FullOCR
        {
            get {
                return fullOCR;
            }

            set {
                fullOCR = value;
            }
        }    
    }
}
