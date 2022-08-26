namespace InstagramParserBot.Tools;

public static class NumberBase
{
    private static readonly List<string> PhoneNumbersBase;
    private static readonly string FilePath;

    static NumberBase()
    {
        FilePath = 
            Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.FullName + "/phone_numbers.txt";
        PhoneNumbersBase = ReadTxtBase();
    }
    
    private static List<string> ReadTxtBase()
    {
        Console.WriteLine("[NUMBER BASE STATUS] Start parsing phone_numbers.txt..");

        var numbers = new List<string>();
        
        if (!File.Exists(FilePath))
        {
            Console.WriteLine("[NUMBER BASE ERROR] File doesn't exists");
            return new List<string>();
        }
        
        numbers.AddRange(File.ReadAllLines(FilePath));
        
        Console.WriteLine("[NUMBER BASE STATUS] Parsed successfully!");
        
        return numbers;
    }

    public static List<string> GetBaseList() => PhoneNumbersBase;

    public static void AppendBase(List<string> newBase)
    {
        for (var index = 0; index < newBase.Count; index++)
        {
            var number = newBase[index];
            if (string.IsNullOrEmpty(number))
                newBase.Remove(number);
        }

        PhoneNumbersBase.AddRange(newBase);
        File.WriteAllLines(FilePath, PhoneNumbersBase);
        
        Console.WriteLine("[NUMBER BASE STATUS] Base updated!");
    }
}