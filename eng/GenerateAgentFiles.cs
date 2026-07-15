// Regenerates the per-coding-agent guidance files (Cursor rule, GitHub Copilot instructions,
// AGENTS.md block) from a single source of truth: the Markdown body of skill/SKILL.md (the
// Claude Code skill file), stripped of its own YAML frontmatter and re-wrapped in each other
// tool's frontmatter shape. This keeps the guidance text itself in exactly one place.
//
// Run from the repo root: dotnet run eng/GenerateAgentFiles.cs

var projectDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "RandomSkunk.StructuredLogging");
var skillPath = Path.Combine(projectDir, "skill", "SKILL.md");
var agentFilesDir = Path.Combine(projectDir, "agentfiles");
var outputDir = Path.Combine(projectDir, "skillfiles-src");
Directory.CreateDirectory(outputDir);

var body = StripFrontmatter(File.ReadAllText(skillPath));

var cursorFrontmatter = File.ReadAllText(Path.Combine(agentFilesDir, "cursor-rule.mdc"));
File.WriteAllText(
    Path.Combine(outputDir, "randomskunk-structuredlogging.cursor.mdc"),
    cursorFrontmatter + "\n" + body);

var copilotFrontmatter = File.ReadAllText(Path.Combine(agentFilesDir, "copilot-instructions.md"));
File.WriteAllText(
    Path.Combine(outputDir, "randomskunk-structuredlogging.copilot.instructions.md"),
    copilotFrontmatter + "\n" + body);

File.WriteAllText(
    Path.Combine(outputDir, "randomskunk-structuredlogging.agentsmd.block"),
    body);

Console.WriteLine($"Generated agent files written to {outputDir}");

static string StripFrontmatter(string content)
{
    if (!content.StartsWith("---", StringComparison.Ordinal))
        return content;

    var endIndex = content.IndexOf("\n---", 3, StringComparison.Ordinal);
    if (endIndex < 0)
        return content;

    var afterMarker = content.IndexOf('\n', endIndex + 1);
    return afterMarker < 0 ? string.Empty : content[(afterMarker + 1)..].TrimStart('\n');
}
