using System;
using System.Collections.Generic;

namespace TransportationsSystem.Services
{
    public class PaymentCalculator
    {
        // Base rates per kilometer (PHP)
        private const decimal BASE_RATE_PER_KM = 15.0m;
        private const decimal ROUND_TRIP_MULTIPLIER = 1.8m; // 10% discount vs 2x
        private const decimal DAY_TRIP_BASE = 3000.0m; // Base day trip rate
        private const decimal DAY_TRIP_PER_DAY = 2500.0m; // Additional per day

        // Distance matrix between Bohol locations (in kilometers)
        private static readonly Dictionary<string, Dictionary<string, decimal>> DistanceMatrix = new()
        {
            ["Tagbilaran City"] = new()
            {
                ["Panglao"] = 18, ["Dauis"] = 12, ["Baclayon"] = 10, ["Loboc"] = 24,
                ["Carmen"] = 55, ["Tubigon"] = 52, ["Ubay"] = 90, ["Talibon"] = 95,
                ["Jagna"] = 63, ["Anda"] = 90, ["Alburquerque"] = 15, ["Loay"] = 20,
                ["Loon"] = 45, ["Calape"] = 48, ["Clarin"] = 67, ["Inabanga"] = 75,
                ["Sagbayan"] = 70, ["Candijay"] = 75, ["Guindulman"] = 85
            },
            ["Panglao"] = new()
            {
                ["Tagbilaran City"] = 18, ["Dauis"] = 8, ["Baclayon"] = 22, ["Loboc"] = 35,
                ["Carmen"] = 65, ["Tubigon"] = 65, ["Ubay"] = 100, ["Talibon"] = 105,
                ["Jagna"] = 75, ["Anda"] = 100, ["Alburquerque"] = 25, ["Loay"] = 30,
                ["Loon"] = 55, ["Calape"] = 60, ["Clarin"] = 75, ["Inabanga"] = 85,
                ["Sagbayan"] = 80, ["Candijay"] = 85, ["Guindulman"] = 95
            },
            ["Dauis"] = new()
            {
                ["Tagbilaran City"] = 12, ["Panglao"] = 8, ["Baclayon"] = 15, ["Loboc"] = 28,
                ["Carmen"] = 60, ["Tubigon"] = 60, ["Ubay"] = 95, ["Talibon"] = 100,
                ["Jagna"] = 70, ["Anda"] = 95, ["Alburquerque"] = 20, ["Loay"] = 25,
                ["Loon"] = 50, ["Calape"] = 55, ["Clarin"] = 70, ["Inabanga"] = 80,
                ["Sagbayan"] = 75, ["Candijay"] = 80, ["Guindulman"] = 90
            },
            ["Carmen"] = new()
            {
                ["Tagbilaran City"] = 55, ["Panglao"] = 65, ["Dauis"] = 60, ["Baclayon"] = 50,
                ["Loboc"] = 35, ["Tubigon"] = 40, ["Ubay"] = 45, ["Talibon"] = 50,
                ["Jagna"] = 80, ["Anda"] = 95, ["Alburquerque"] = 60, ["Loay"] = 40,
                ["Loon"] = 25, ["Calape"] = 30, ["Clarin"] = 35, ["Inabanga"] = 45,
                ["Sagbayan"] = 20, ["Candijay"] = 85, ["Guindulman"] = 90
            },
            ["Jagna"] = new()
            {
                ["Tagbilaran City"] = 63, ["Panglao"] = 75, ["Dauis"] = 70, ["Baclayon"] = 58,
                ["Loboc"] = 45, ["Carmen"] = 80, ["Tubigon"] = 90, ["Ubay"] = 42,
                ["Talibon"] = 85, ["Anda"] = 28, ["Alburquerque"] = 68, ["Loay"] = 48,
                ["Loon"] = 75, ["Calape"] = 80, ["Clarin"] = 95, ["Inabanga"] = 100,
                ["Sagbayan"] = 85, ["Candijay"] = 25, ["Guindulman"] = 35
            },
            ["Anda"] = new()
            {
                ["Tagbilaran City"] = 90, ["Panglao"] = 100, ["Dauis"] = 95, ["Baclayon"] = 85,
                ["Loboc"] = 70, ["Carmen"] = 95, ["Tubigon"] = 110, ["Ubay"] = 50,
                ["Talibon"] = 95, ["Jagna"] = 28, ["Alburquerque"] = 95, ["Loay"] = 75,
                ["Loon"] = 100, ["Calape"] = 105, ["Clarin"] = 115, ["Inabanga"] = 120,
                ["Sagbayan"] = 105, ["Candijay"] = 18, ["Guindulman"] = 25
            }
        };

        public static decimal CalculatePayment(string origin, string destination, string tripType, int tripDays = 1)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination))
                return 0;

            if (origin == destination)
                return 100; // Minimum charge for same location

            decimal distance = GetDistance(origin, destination);

            return tripType.ToUpper() switch
            {
                "ONE_WAY" => CalculateOneWay(distance),
                "ROUND_TRIP" => CalculateRoundTrip(distance),
                "DAY_TRIP" => CalculateDayTrip(distance, tripDays),
                _ => CalculateOneWay(distance)
            };
        }

        private static decimal GetDistance(string origin, string destination)
        {
            // Try to get distance from matrix
            if (DistanceMatrix.ContainsKey(origin) && DistanceMatrix[origin].ContainsKey(destination))
            {
                return DistanceMatrix[origin][destination];
            }

            // Try reverse lookup
            if (DistanceMatrix.ContainsKey(destination) && DistanceMatrix[destination].ContainsKey(origin))
            {
                return DistanceMatrix[destination][origin];
            }

            // Default distance if not found (estimated)
            return 50;
        }

        private static decimal CalculateOneWay(decimal distance)
        {
            decimal baseAmount = distance * BASE_RATE_PER_KM;
            
            // Add minimum charge
            if (baseAmount < 150)
                baseAmount = 150;

            return Math.Round(baseAmount, 2);
        }

        private static decimal CalculateRoundTrip(decimal distance)
        {
            decimal oneWay = CalculateOneWay(distance);
            decimal roundTrip = oneWay * ROUND_TRIP_MULTIPLIER;
            
            return Math.Round(roundTrip, 2);
        }

        private static decimal CalculateDayTrip(decimal distance, int days)
        {
            if (days < 1) days = 1;
            if (days > 30) days = 30; // Maximum 30 days

            // Base day trip rate + additional per day
            decimal totalAmount = DAY_TRIP_BASE;
            
            if (days > 1)
            {
                totalAmount += (days - 1) * DAY_TRIP_PER_DAY;
            }

            // Add distance factor for longer trips
            if (distance > 50)
            {
                decimal distanceSurcharge = (distance - 50) * 10m * days;
                totalAmount += distanceSurcharge;
            }

            return Math.Round(totalAmount, 2);
        }

        public static string GetTripTypeDisplay(string tripType)
        {
            return tripType.ToUpper() switch
            {
                "ONE_WAY" => "One Way",
                "ROUND_TRIP" => "Round Trip",
                "DAY_TRIP" => "Day Trip",
                _ => "One Way"
            };
        }
    }
}
