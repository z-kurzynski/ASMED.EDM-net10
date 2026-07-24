using System.Text;
using System.Text.RegularExpressions;

namespace ASMED.EDM.UI.Services;

public class CSharpMethodStubGenerator
{
    public string GenerateStubsFromSource(string sourceCode)
    {
        var result = new StringBuilder();
        var lines = sourceCode.Split('\n');

        var inMethod = false;
        var braceCount = 0;
        var methodSignature = new StringBuilder();
        var beforeClass = new StringBuilder();
        var inClass = false;
        var classIndent = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.TrimStart();

            // Zachowaj wszystko przed klasą (using, namespace, itp.)
            if (!inClass && !trimmedLine.StartsWith("public class") && 
                !trimmedLine.StartsWith("internal class") && 
                !trimmedLine.StartsWith("public partial class") &&
                !trimmedLine.StartsWith("internal partial class"))
            {
                beforeClass.AppendLine(line);
                continue;
            }

            // Rozpoczęcie klasy
            if (!inClass && (trimmedLine.StartsWith("public class") || 
                trimmedLine.StartsWith("internal class") ||
                trimmedLine.StartsWith("public partial class") ||
                trimmedLine.StartsWith("internal partial class")))
            {
                inClass = true;
                classIndent = line.Length - line.TrimStart().Length;
                result.Append(beforeClass);
                result.AppendLine(line);
                continue;
            }

            if (!inClass)
                continue;

            // Jeśli jesteśmy w metodzie
            if (inMethod)
            {
                // Zliczaj nawiasy klamrowe
                foreach (char c in line)
                {
                    if (c == '{') braceCount++;
                    if (c == '}') braceCount--;
                }

                // Koniec metody
                if (braceCount == 0)
                {
                    inMethod = false;
                    methodSignature.Clear();
                }
                continue;
            }

            // Sprawdź czy to sygnatura metody
            if (IsMethodSignature(trimmedLine) && !trimmedLine.Contains(";"))
            {
                // Zachowaj atrybuty przed metodą
                var attributeLines = new List<string>();
                for (int j = i - 1; j >= 0; j--)
                {
                    var prevLine = lines[j].TrimStart();
                    if (prevLine.StartsWith("[") && prevLine.EndsWith("]"))
                    {
                        attributeLines.Insert(0, lines[j]);
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var attr in attributeLines)
                {
                    result.AppendLine(attr);
                }

                // Czy sygnatura metody jest w jednej linii?
                if (line.Contains("(") && line.Contains(")"))
                {
                    result.AppendLine(line);

                    // Znajdź następną linię z {
                    var nextLineIdx = i + 1;
                    while (nextLineIdx < lines.Length && !lines[nextLineIdx].Contains("{"))
                    {
                        nextLineIdx++;
                    }

                    if (nextLineIdx < lines.Length)
                    {
                        var openBraceLine = lines[nextLineIdx];
                        var indent = new string(' ', openBraceLine.Length - openBraceLine.TrimStart().Length);
                        result.AppendLine(indent + "{");
                        result.AppendLine(indent + "    throw new NotImplementedException();");
                        result.AppendLine(indent + "}");
                        result.AppendLine();

                        inMethod = true;
                        braceCount = 1;
                    }
                }
                else
                {
                    // Sygnatura metody rozproszona po wielu liniach
                    methodSignature.AppendLine(line);
                    var nextIdx = i + 1;
                    while (nextIdx < lines.Length && !lines[nextIdx].Contains(")"))
                    {
                        methodSignature.AppendLine(lines[nextIdx]);
                        nextIdx++;
                    }

                    if (nextIdx < lines.Length)
                    {
                        methodSignature.AppendLine(lines[nextIdx]); // Linia z )
                        result.Append(methodSignature);

                        // Znajdź {
                        var bracketIdx = nextIdx + 1;
                        while (bracketIdx < lines.Length && !lines[bracketIdx].Contains("{"))
                        {
                            bracketIdx++;
                        }

                        if (bracketIdx < lines.Length)
                        {
                            var openBraceLine = lines[bracketIdx];
                            var indent = new string(' ', openBraceLine.Length - openBraceLine.TrimStart().Length);
                            result.AppendLine(indent + "{");
                            result.AppendLine(indent + "    throw new NotImplementedException();");
                            result.AppendLine(indent + "}");
                            result.AppendLine();

                            inMethod = true;
                            braceCount = 1;
                        }

                        methodSignature.Clear();
                    }
                }
                continue;
            }

            // Właściwości (properties), pola, konstruktory itp.
            if (trimmedLine.Contains("{ get;") || 
                trimmedLine.Contains("{ set;") ||
                IsPropertyOrField(trimmedLine) ||
                IsConstructor(trimmedLine, lines, i))
            {
                result.AppendLine(line);

                // Jeśli to konstruktor z ciałem
                if (IsConstructor(trimmedLine, lines, i) && !trimmedLine.Contains(";"))
                {
                    // Znajdź otwarcie nawiasu klamrowego
                    if (line.Contains("{"))
                    {
                        inMethod = true;
                        braceCount = 1;
                        foreach (char c in line)
                        {
                            if (c == '{') braceCount++;
                            if (c == '}') braceCount--;
                        }
                        if (braceCount == 0)
                            inMethod = false;
                    }
                }
                continue;
            }

            // Inne linie (zamknięcie klasy, komentarze, itp.)
            result.AppendLine(line);
        }

        return result.ToString();
    }

    private bool IsMethodSignature(string line)
    {
        line = line.Trim();

        // Ignoruj właściwości auto-implemented
        if (line.Contains("{ get;") || line.Contains("{ set;"))
            return false;

        // Podstawowe modyfikatory dostępu
        var modifiers = new[] { "public", "private", "protected", "internal", "static", "virtual", "override", "async" };
        var hasModifier = modifiers.Any(m => line.StartsWith(m + " "));

        // Czy linia zawiera nazwę metody i otwierający nawias
        var hasParenthesis = line.Contains("(");

        // Ignoruj konstruktory (sprawdzane oddzielnie)
        var words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            var potentialReturnType = words[words.Length - 2];
            var potentialMethodName = words[words.Length - 1].Split('(')[0];

            // Czy to metoda (ma typ zwracany)
            var hasReturnType = !string.IsNullOrWhiteSpace(potentialReturnType) && 
                               potentialReturnType != "public" && 
                               potentialReturnType != "private" && 
                               potentialReturnType != "protected" &&
                               potentialReturnType != "internal" &&
                               potentialReturnType != "static" &&
                               potentialReturnType != "virtual" &&
                               potentialReturnType != "override" &&
                               potentialReturnType != "async";

            return hasModifier && hasParenthesis && hasReturnType;
        }

        return hasModifier && hasParenthesis;
    }

    private bool IsPropertyOrField(string line)
    {
        line = line.Trim();

        // Auto-implemented property
        if ((line.Contains("{ get;") || line.Contains("{ set;")) && line.Contains("}"))
            return true;

        // Pole (field)
        if (line.Contains(";") && !line.Contains("("))
        {
            var modifiers = new[] { "public", "private", "protected", "internal", "static", "readonly", "const" };
            return modifiers.Any(m => line.StartsWith(m + " "));
        }

        return false;
    }

    private bool IsConstructor(string line, string[] allLines, int currentIndex)
    {
        line = line.Trim();

        // Konstruktor rozpoczyna się modyfikatorem dostępu i nazwą klasy
        if (!line.Contains("("))
            return false;

        // Szukaj nazwy klasy w poprzednich liniach
        for (int i = currentIndex; i >= 0; i--)
        {
            var prevLine = allLines[i].Trim();
            if (prevLine.StartsWith("public class") || 
                prevLine.StartsWith("internal class") ||
                prevLine.StartsWith("public partial class") ||
                prevLine.StartsWith("internal partial class"))
            {
                var className = prevLine.Split(' ').FirstOrDefault(w => 
                    !w.Equals("public") && 
                    !w.Equals("class") && 
                    !w.Equals("internal") && 
                    !w.Equals("partial"));

                if (!string.IsNullOrWhiteSpace(className))
                {
                    className = className.TrimEnd(':', '{');
                    return line.Contains(className + "(");
                }
            }
        }

        return false;
    }
}
