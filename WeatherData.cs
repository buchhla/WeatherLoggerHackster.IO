using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherLogger
{
    public class WeatherData
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public float TemperatureinF { get; set; }
        public float Pressureinmb { get; set; }
    }
}
