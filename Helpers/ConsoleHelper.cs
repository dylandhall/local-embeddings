using System.Text;
using System.Text.RegularExpressions;
using ConsoleMarkdownRenderer;
using LocalEmbeddings.Managers;

namespace LocalEmbeddings.Helpers;

public static class ConsoleHelper {
    public static void WriteMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return;

        markdown = markdown.Replace("\r", "");
        markdown = Regex.Replace(markdown, "(?<!\n)\n(?!\n)", "\n\n\n");

        Displayer.DisplayMarkdown(markdown, new Uri(AppContext.BaseDirectory, UriKind.Absolute),
            allowFollowingLinks: false);
    }
    
    public static ActionEnum GetActionFromKey(Dictionary<ConsoleKey, (ActionEnum action, string description)> actions,
        bool isDefaultAllowed, string? defaultText = "")
    {
        var sb = new StringBuilder();

        foreach (var action in actions)
        {
            sb.AppendLine($"* **Press {action.Key}** to {action.Value.description}");
        }

        if (isDefaultAllowed)
        {
            sb.AppendLine($"Any other key to {defaultText}");
        }

        sb.AppendLine();
        ConsoleHelper.WriteMarkdown(sb.ToString());

        if (isDefaultAllowed)
        {
            var key = Console.ReadKey(intercept:true).Key;

            return actions.TryGetValue(key, out var action)
                ? action.action
                : ActionEnum.Default;
        }

        {
            ConsoleKey? key = null;
            while (key == null || !actions.ContainsKey(key.Value))
            {
                key = Console.ReadKey(intercept: true).Key;
            }

            return actions.TryGetValue(key.Value, out var action)
                ? action.action
                : ActionEnum.Default;
        }
    }
    
    
    public static (int? numberSelected, ActionEnum? action) GetActionOrNumberFromKey(
        Dictionary<ConsoleKey, (ActionEnum action, string description)> actions, int maxNum, bool isDefaultAllowed,
        string? defaultText = "")
    {
        var sb = new StringBuilder();

        foreach (var action in actions)
        {
            sb.AppendLine($"* **Press {action.Key}** to {action.Value.description}");
        }

        if (isDefaultAllowed)
        {
            sb.AppendLine($"Any other key to {defaultText}");
        }

        sb.AppendLine();
        ConsoleHelper.WriteMarkdown(sb.ToString());

        if (isDefaultAllowed)
        {
            var key = Console.ReadKey(intercept: true);

            return GetReturn(actions, key, maxNum);
        }

        {
            ConsoleKeyInfo? key = null;
            while (key == null || (!actions.ContainsKey(key.Value.Key) &&
                                   !(key.Value.KeyChar >= '1' && key.Value.KeyChar < maxNum + '1')))
            {
                key = Console.ReadKey(intercept: true);
            }

            return GetReturn(actions, key.Value, maxNum);
        }

        static (int? numberSelected, ActionEnum? action) GetReturn(
            Dictionary<ConsoleKey, (ActionEnum action, string description)> actions, ConsoleKeyInfo key, int maxNum)
        {
            return actions.TryGetValue(key.Key, out var action)
                ? (null, action.action)
                : key.KeyChar >= '1' && key.KeyChar < maxNum + '1'
                    ? (key.KeyChar - '1', null)
                    : (null, ActionEnum.Default);
        }
    }
}

