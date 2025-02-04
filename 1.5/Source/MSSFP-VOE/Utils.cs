using System;
using System.Collections.Generic;
using Verse;

namespace MSSFP.VOE;

public static class Utils
{
    public static List<int> GenerateRandomParts(int total, int parts)
    {
        if (parts <= 0)
        {
            throw new ArgumentException("Parts must be greater than 0.");
        }

        List<int> randomNumbers = [];

        // Generate parts - 1 random numbers between 1 and (total - 1), inclusive
        for (int i = 0; i < parts - 1; i++)
        {
            randomNumbers.Add(Rand.Range(1, total));
        }

        // Add 0 and total to the list
        randomNumbers.Add(0);
        randomNumbers.Add(total);

        // Sort the list in ascending order
        randomNumbers.Sort();

        // Calculate differences between consecutive elements
        List<int> partsList = [];
        for (int i = 1; i < randomNumbers.Count; i++)
        {
            partsList.Add(randomNumbers[i] - randomNumbers[i - 1]);
        }

        return partsList;
    }
}
