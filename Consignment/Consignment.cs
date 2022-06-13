using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Containers
{
    public sealed class Consignment
    {
        public List<string> _firstMoves = new List<string>();
        public List<string> _secondMoves = new List<string>();

        public void AddMoveFirst(string m)
        {
            _firstMoves.Add(m);
        }

        public void AddMoveSecond(string m)
        {
            _secondMoves.Add(m);
        }

        public void RemoveFirstMove(string m)
        {
            _firstMoves.Remove(m);
        }
        public void RemoveSecondMove(string m)
        {
            _secondMoves.Remove(m);
        }
    }
}
