using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class Money
{
    private static readonly Dictionary<string, CultureInfo> CurrencyCultureMap = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = CultureInfo.GetCultureInfo("en-US"),
        ["EUR"] = CultureInfo.GetCultureInfo("de-DE"),
        ["GBP"] = CultureInfo.GetCultureInfo("en-GB"),
        ["JPY"] = CultureInfo.GetCultureInfo("ja-JP"),
        ["PHP"] = CultureInfo.GetCultureInfo("en-PH"),
        ["CAD"] = CultureInfo.GetCultureInfo("en-CA"),
        ["AUD"] = CultureInfo.GetCultureInfo("en-AU")
    };

    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public Money(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode.ToUpper();

        if (!CurrencyCultureMap.ContainsKey(CurrencyCode))
        {
            throw new ArgumentException($"Unsupported currency: {CurrencyCode}");
        }
    }

    public static Money Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be empty");
        }

        var (currencyCode, numberStr) = ExtractCurrencyCode(input);

        var parsingResults = AttemptParsing(numberStr);

        if (parsingResults.Count == 0)
        {
            throw new FormatException($"Could not parse amount from: {input}");
        }

        if (parsingResults.Count > 1)
        {
            var formatOptions = string.Join(", ", parsingResults.Keys);
            throw new FormatException($"Ambiguous format - multiple valid interpretations: {formatOptions}");
        }

        var parsedValue = parsingResults.First();
        return new Money(parsedValue.Value, currencyCode ?? parsedValue.Key); // Use detected currency or default
    }

    private static (string CurrencyCode, string NumberStr) ExtractCurrencyCode(string input)
    {
        var currencyMatch = Regex.Match(input, @"^([A-Z]{2,3})\s*");
        if (currencyMatch.Success)
        {
            var code = currencyMatch.Groups[1].Value;
            if (CurrencyCultureMap.ContainsKey(code))
            {
                return (code, input.Substring(currencyMatch.Length));
            }
        }

        if (input.Contains('$'))
        {
            return ("USD", input.Replace("$", "").Trim());
        }
        if (input.Contains('€'))
        {
            return ("EUR", input.Replace("€", "").Trim());
        }
        if (input.Contains('£'))
        {
            return ("GBP", input.Replace("£", "").Trim());
        }

        return (null, input);
    }

    private static Dictionary<string, decimal> AttemptParsing(string numberStr)
    {
        var results = new Dictionary<string, decimal>();

        if (decimal.TryParse(numberStr, NumberStyles.Currency, CultureInfo.CurrentCulture, out var currentResult))
        {
            results["CurrentCulture"] = currentResult;
        }

        foreach (var culture in CurrencyCultureMap.Values)
        {
            if (decimal.TryParse(numberStr, NumberStyles.Currency, culture, out var result))
            {
                string formatName = $"Format: {culture.Name} ({culture.NumberFormat.CurrencySymbol})";
                if (!results.ContainsValue(result))
                {
                    results[formatName] = result;
                }
            }
        }

        if (decimal.TryParse(numberStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var simpleResult))
        {
            results["InvariantCulture"] = simpleResult;
        }

        return results;
    }

    public override string ToString()
    {
        return $"{CurrencyCode} {Amount:N2}";
    }
}

public class PriceNormalizer
{
    public static List<Money> NormalizeAndSort(List<string> priceInputs)
    {
        var parsedPrices = new List<Money>();
        var errors = new List<string>();

        foreach (var input in priceInputs)
        {
            try
            {
                parsedPrices.Add(Money.Parse(input));
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to parse '{input}': {ex.Message}");
            }
        }

        if (errors.Any())
        {
            Console.WriteLine("Encountered errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"- {error}");
            }
        }

        return parsedPrices.OrderBy(m => m.Amount).ToList();
    }
}

namespace Currency_Formatter___Parser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Price Normalization System");
            Console.WriteLine("Enter prices (one per line, empty line to finish):");

            var inputs = new List<string>();
            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) break;
                inputs.Add(line);
            }

            var normalizedPrices = PriceNormalizer.NormalizeAndSort(inputs);

            Console.WriteLine("\nNormalized and Sorted Prices:");
            Console.WriteLine(new string('-', 30));
            foreach (var price in normalizedPrices)
            {
                Console.WriteLine(price);
            }
        }
    }
}
