using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab88
{
    internal class Enemy
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Enemy(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
