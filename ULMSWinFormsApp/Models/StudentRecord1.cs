namespace ULMSWinFormsApp.Models
{
    public interface StudentRecord1
    {
        Dictionary<string, double?> Marks { get; set; }
        string Programme { get; set; }
        string Status { get; set; }
        string StudentId { get; set; }
        string StudentName { get; set; }
        int Year { get; set; }
    }
}