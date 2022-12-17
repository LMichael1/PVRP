using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVRPSolver
{
    public class Vehicle
    {
        public int ID { get; set; }

        public Vehicle(int id)
        {
            ID = id;
        }

        public Vehicle Clone()
        {
            return new Vehicle(ID);
        }
    }
}
