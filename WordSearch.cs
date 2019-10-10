using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC_Parser
{
    public class WordSearch
    {
        public int Id { get; set; }
        public string Word { get; set; }
        public string Word2 { get; set; }
        public string KeyWordEval { get; set; }
        public int totalWordLeft { get; set; }
        public int totalWordRigth { get; set; }
        public int PositionStart { get; set; }
        public int PositionEnd { get; set; }
    }
}
