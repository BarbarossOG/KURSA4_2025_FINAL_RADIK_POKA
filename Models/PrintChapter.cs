using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class PrintChapter
    {
        public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public int ObjectId { get; set; }
    public List<PrintSubchapter> Subchapters { get; set; } = new();
    }
}