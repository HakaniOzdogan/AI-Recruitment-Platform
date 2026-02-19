namespace IkOtomasyon.Api.Entities;

public class InterviewQuestionBank
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TopicKey { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string TagsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
