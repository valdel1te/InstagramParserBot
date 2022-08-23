namespace InstagramParserBot.Tools;

public static class NumberBase
{
    private static List<string> _phoneNumbersBase;
    private static readonly string FilePath;

    static NumberBase()
    {
        FilePath = 
            Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.FullName + "/phone_numbers.txt";
        _phoneNumbersBase = ReadTxtBase();
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

    public static List<string> GetBaseList() => _phoneNumbersBase;

    public static void OverrideBase(List<string> newBase)
    {
        _phoneNumbersBase = newBase;
        File.WriteAllLines(FilePath, _phoneNumbersBase);
        
        Console.WriteLine("[NUMBER BASE STATUS] Base updated!");
    }
}