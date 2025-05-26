namespace Mashd.Backend.BuiltInMethods;

public static class FuzzyMatchMethod
{
    public static bool FuzzyMatch(string str1, string str2, double threshold)
    {
        if (threshold is < 0 or > 1)
        {
            return false;
        }
    
        // Normalize the strings by removing spaces and converting to lowercase
        str1 = str1.Replace(" ", "").ToLower();
        str2 = str2.Replace(" ", "").ToLower();
    
        // Calculate the Levenshtein distance
        int distance = CalculateLevenshteinDistance(str1, str2);
    
        // Calculate the maximum possible length for normalization
        int maxLength = Math.Max(str1.Length, str2.Length);
    
        // Return true if the normalized distance is within the threshold
        return (double)distance / maxLength <= threshold;
    }
    
    private static int CalculateLevenshteinDistance(string s, string t)
    {
        int[,] dp = new int[s.Length + 1, t.Length + 1];
    
        for (int i = 0; i <= s.Length; i++)
            dp[i, 0] = i;
        for (int j = 0; j <= t.Length; j++)
            dp[0, j] = j;
    
        for (int i = 1; i <= s.Length; i++)
        {
            for (int j = 1; j <= t.Length; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }
    
        return dp[s.Length, t.Length];
    }
}