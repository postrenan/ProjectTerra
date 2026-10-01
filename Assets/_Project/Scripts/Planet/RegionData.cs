using System;
using System.Collections.Generic;

namespace ProjectTerra.Planet
{
    [Serializable]
    public class RegionData
    {
        public string name;
        public string country;
        public string type;
        public float centerLat;
        public float centerLon;
        public float minLat;
        public float maxLat;
        public float minLon;
        public float maxLon;
        public int forestPercent;
        public int mineralsPercent;
        public int arablePercent;
        public int waterPercent;

        public float BoundingArea => (maxLat - minLat) * (maxLon - minLon);

        public bool Contains(float lat, float lon)
        {
            return lat >= minLat && lat <= maxLat && lon >= minLon && lon <= maxLon;
        }

        public float DistanceSqr(float lat, float lon)
        {
            float dLat = lat - centerLat;
            float dLon = lon - centerLon;
            return dLat * dLat + dLon * dLon;
        }
    }

    [Serializable]
    public class RegionDatabase
    {
        public List<RegionData> regions = new List<RegionData>();
    }
}
