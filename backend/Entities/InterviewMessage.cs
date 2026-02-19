namespace IkOtomasyon.Api.Entities;

public class InterviewMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InterviewSession? Session { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? QuestionBankId { get; set; }
    public InterviewQuestionBank? QuestionBank { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
