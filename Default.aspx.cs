using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CC_Parser;

namespace AppCamaraComercio
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            ParagraphExtracter ParagraphExtracter = new ParagraphExtracter();
            ExtractResult extractResult = new ExtractResult();

            extractResult = ParagraphExtracter.parse_function(txtFullOCR.Text, "Medellin", "6", @"Data Source=DESKTOP-59SP7SJ\SQLEXPRESS;Initial Catalog=BDTransformacionInformacion;Integrated Security=True");

        }
    }
}