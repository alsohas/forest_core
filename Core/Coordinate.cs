namespace forest_core
{
    public class Coordinate
    {
        public double Latitude;
        public double Longitude;

        public Coordinate(double longitude, double latitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return $"Longitude: {Longitude}\tLatitude: {Latitude}";
        }
    }
}