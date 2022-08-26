using System.Data;
using System.Data.OleDb;
using InstagramParserBot.Instagram;
using Spire.Doc;
using Spire.Doc.Documents;

namespace InstagramParserBot.Tools;

// the worst translate realisation
public static class MicrosoftOfficeService
{
    private static readonly string RussianCitiesPath;

    static MicrosoftOfficeService()
    {
        RussianCitiesPath =
            Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.FullName + "/russian_cities.xlsx";
    }

    public static string TranslateFromExcelFile(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";
        
        var split = input.Split(',');

        var connectionString =
            "Provider=Microsoft.ACE.OLEDB.12.0;" +
            $"Data Source={RussianCitiesPath};" +
            "Extended Properties=\"Excel 12.0 Xml;" +
            "HDR=No;IMEX=1\";";

        var dataTable = new DataTable();
        
        using (var sqlConnection = new OleDbConnection(connectionString))
        {
            var query = $"SELECT * FROM [cities$] WHERE [F1] = '{split[0]}'";
            
            var sqlAdapter = new OleDbDataAdapter(new OleDbCommand(query, sqlConnection));
            
            sqlAdapter.Fill(dataTable);
        }

        string result;
        
        try
        {
            result = dataTable.Rows[0][1].ToString()!;
        }
        catch (Exception e)
        {
            return string.Join(", " , split);
        }

        return result;
    }

    public static Document SendWordDocument(List<InstagramUserData> followers)
    {
        var document = new Document();

        var paragraph = document.AddSection().AddParagraph();

        foreach (var follower in followers)
        {
            paragraph.AppendText(follower.ToString());
            paragraph.AppendBreak(BreakType.LineBreak);
        }
        
        document.SaveToFile($"accounts{DateTime.Today:ddMM}.docx", FileFormat.Docx);

        return document;
    }
}