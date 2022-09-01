using System.Data;
using System.Data.OleDb;
using InstagramParserBot.Instagram;
using Spire.Doc;
using Spire.Doc.Documents;
using Excel = Microsoft.Office.Interop.Excel;

namespace InstagramParserBot.Tools;

public static class MicrosoftOfficeService
{
    private static OleDbConnection _connection = null!;
    private static readonly string DatabasePath;

    static MicrosoftOfficeService()
    {
        DatabasePath = Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.FullName + "/database.xlsx";

        var connectionString =
            "Provider=Microsoft.ACE.OLEDB.12.0;" +
            $"Data Source={DatabasePath};" +
            "Extended Properties=\"Excel 12.0 Xml;" +
            "HDR=No\";";

        _connection = new OleDbConnection(connectionString);
    }

    // the worst translate realisation
    public static string TranslateFromExcelFile(string input)
    {
        ConnectionOn();
        
        if (string.IsNullOrEmpty(input))
            return "";

        var split = input.Split(',');

        var dataTable = new DataTable();

        var query = @$"SELECT * FROM [russian_cities$] WHERE [F1] = '{split[0]}'";
        var sqlAdapter = new OleDbDataAdapter(new OleDbCommand(query, _connection));
        sqlAdapter.Fill(dataTable);

        string result;

        try
        {
            result = dataTable.Rows[0][1].ToString()!;
        }
        catch (Exception e)
        {
            return string.Join(", ", split);
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

    public static bool DatabaseHasUser(string username, string number)
    {
        ConnectionOn();
        
        if (string.IsNullOrEmpty(number))
            number = "-";

        var dataTable = new DataTable();

        var query = @$"SELECT * FROM [user_data$] WHERE [F1] = '{username}' OR [F2] = '{number}'";
        var sqlAdapter = new OleDbDataAdapter(new OleDbCommand(query, _connection));
        sqlAdapter.Fill(dataTable);

        return dataTable.Rows.Count > 0;
    }

    public static void AddUsersToDatabase(List<InstagramUserData> users)
    {
        ConnectionOn();
        
        foreach (var data in users)
        {
            var query = @$"INSERT INTO [user_data$] ([F1], [F2]) VALUES('{data.UserName}','{data.ContactNumber}')";
            var command = new OleDbCommand
            {
                Connection = _connection,
                CommandText = query,
                CommandType = CommandType.Text
            };
            command.ExecuteNonQuery();
        }

        _connection.Close();

        Console.WriteLine("[DATABASE STATUS] Base updated!");
    }

    private static void ConnectionOn()
    {
        if ((_connection.State & ConnectionState.Open) != 0)
            return;
        
        _connection.Open();
    }
}