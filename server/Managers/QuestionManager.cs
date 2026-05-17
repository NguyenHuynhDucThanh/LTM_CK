using System.Text.Json;
using server.Models;

namespace server.Managers;

public class QuestionManager
{
    private readonly Dictionary<string, List<Question>> _sets = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _questionDir;

    public QuestionManager(IWebHostEnvironment env)
    {
        _questionDir = Path.Combine(env.ContentRootPath, "Questions");
        Directory.CreateDirectory(_questionDir);
        LoadAll();
    }

    public void LoadAll()
    {
        _sets.Clear();

        foreach (var file in Directory.GetFiles(_questionDir, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var json = File.ReadAllText(file);
            var questions = JsonSerializer.Deserialize<List<Question>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];

            _sets[name] = questions;
        }
    }

    public List<string> GetSetNames() => _sets.Keys.OrderBy(x => x).ToList();

    public List<Question> GetQuestions(string setName)
    {
        return _sets.TryGetValue(setName, out var questions) ? questions : [];
    }
}
