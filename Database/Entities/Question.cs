﻿namespace CustomQotd.Database.Entities
{
    public enum QuestionType
    {
        Suggested, Accepted, Sent
    }

    public class Question(int id, QuestionType questionType, string text, ulong submittedByUserId, DateTime timestamp)
    {
        public int Id { get; set; } = id;
        public QuestionType QuestionType { get; set; } = questionType;
        public string Text { get; set; } = text;
        public ulong SubmittedByUserId { get; set; } = submittedByUserId;
        public DateTime Timestamp { get; set; } = timestamp;
    }
}