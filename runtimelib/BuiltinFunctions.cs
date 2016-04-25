using System;
using System.Linq;
using System.Text;

public static class BuiltinFunctions
{
    public static void Echo(params JamList[] values)
    {
        foreach (var value in values)
        {
            Console.Write(value.ToString());
            Console.Write(" ");
        }
        Console.WriteLine();
    }

    public static JamList MD5(JamList input)
    {
        return new JamList(input.Elements.Select(CalculateMD5Hash).ToArray());
    }

    static string CalculateMD5Hash(string input)
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);

        byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(inputBytes);
        
        var sb = new StringBuilder();

        foreach (byte t in hash)
            sb.Append(t.ToString("x2"));

        return sb.ToString();
    }

}