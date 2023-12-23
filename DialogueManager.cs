using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class HistoryEntry
{
    public int nodeIndex = -1;
    public int choiceIndex = -1;
}

public class Output
{
    
}

public class Line : Output
{
    public string id;
    public string text;
}

public class DialogueLine : Line
{
    public string speaker;
}

public class ActionLine : Line
{
}

public class ChoicesOutput : Output
{
    public List<ChoiceLine> choices;
}

public class ChoiceLine
{
    public string id;
    public int choiceIndex;
    public string text;
}

public class DialogueManager
{
    public readonly Dictionary<string, string> variables;

    private readonly List<NodeData> _story;
    private readonly Dictionary<string, Dictionary<int, int>> _storyPath = new Dictionary<string, Dictionary<int, int>>();
    private readonly Dictionary<int, int> _storyPathByIndex = new Dictionary<int, int>();

    private readonly List<Line> _nextLines = new List<Line>();
    private readonly List<HistoryEntry> _history = new List<HistoryEntry>();
    
    private readonly Dictionary<string, string> _localization;

    private int _currentNode = 0;
    private bool _waitingChoice;
    public int _waitingJump = -1;

    public DialogueManager(List<NodeData> story, IDictionary<string, string> variables, Dictionary<string, string> localization = null)
    {
        if (story.Count < 1) { throw new Exception("The story data doesn't have at least one node."); }
        _story = story;
        _localization = localization ?? new Dictionary<string, string>();
        this.variables = new Dictionary<string, string>(variables);
        HistoryEntry historyEntry = new HistoryEntry { nodeIndex = 0 };
        _history.Add(historyEntry);
        SaveNextLines();
    }

    public void RestartDialogue(bool removeHistory = true)
    {
        _currentNode = 0;
        _waitingChoice = false;
        HistoryEntry historyEntry = new HistoryEntry { nodeIndex = 0 };
        _history.Add(historyEntry);
    }

    public Output GetNextLine()
    {
        if (_waitingChoice)
        {
            throw new Exception("The dialogue is waiting for a choice.");
        }

        Output output;
        if (_nextLines.Count > 0)
        {
            output = ExtractFirstTextLine(_nextLines);
        }
        else if (_waitingJump != -1)
        {
            JumpToNode(_waitingJump, _currentNode);
            output = GetNextLine();
        }
        else
        {
            output = GetChoicesLine(_history[^1].nodeIndex);
        }

        return output;
    }

    public Line PeekNextLine ()
    {
        return _nextLines.Count == 0 ? null : _nextLines[0];
    }

    private void SaveNextLines()
    {
        foreach (LineData lineData in _story[_currentNode].lines)
        {
            SaveLine(lineData);
            if(_waitingJump != -1) return;
        }
    }

    private void SaveLine(LineData lineData)
    {
        switch (lineData)
        {
            case DialogueLineData dialogueLineData:
                string id = _story[_currentNode].Id + "." + dialogueLineData.index;
                _nextLines.Add(new DialogueLine
                {
                    id = id,
                    speaker = dialogueLineData.speaker,
                    text = GetDialogueText(id, dialogueLineData.text, _currentNode)
                });
                break;
            case ActionLineData actionLineData:
                _nextLines.Add(new ActionLine
                {
                    text = actionLineData.text
                });
                break;
            case GoToLineData goToLineData:
                _waitingJump = goToLineData.targetIndex;
                break;
            case ConditionalLineData conditionalLineData:
                List<LineData> linesData = GetConditionLines(conditionalLineData);
                foreach (LineData data in linesData)
                {
                    SaveLine(data);
                }
                break;
        }
    }

    private ChoicesOutput GetChoicesLine(int index)
    {
        ChoicesOutput line = new ChoicesOutput();
        if (index < 0 || (line.choices = GetChoices(index)).Count == 0)
        {
            return null;
        }
        
        _waitingChoice = true;

        return line;
    }

    public void SetChoice(int choiceIndex)
    {
        if (!_waitingChoice)
        {
            throw new Exception("The dialogue is not waiting for a choice.");
        }

        TryGetChoiceTarget(_story[_currentNode].choices, choiceIndex, out int targetIndex);
        
        JumpToNode(targetIndex, _currentNode, choiceIndex);
        _waitingChoice = false;
    }

    private bool TryGetChoiceTarget(List<LineData> choices, int choiceIndex, out int targetIndex)
    {
        foreach (LineData line in choices)
        {
            if (line is ChoiceData data)
            {
                if (data.index != choiceIndex) continue;
                
                targetIndex = data.targetIndex;
                return true;
            }

            if (!(line is ConditionalLineData conditionalLineData)) continue;
            
            if (TryGetChoiceTarget(conditionalLineData.lines, choiceIndex, out int returnIndex))
            {
                targetIndex = returnIndex;
                return true;
            }

            foreach (ConditionalLineData nestedConditional in conditionalLineData.nestedConditionals)
            {
                if (!TryGetChoiceTarget(nestedConditional.lines, choiceIndex, out int returnNestedIndex)) continue;
                targetIndex = returnNestedIndex;
                return true;
            }
        }

        targetIndex = -1;
        return false;
    }

    private void JumpToNode(int targetIndex, int originIndex, int choiceIndex = -1)
    {
        HistoryEntry actualEntry = _history[^1];
        actualEntry.choiceIndex = choiceIndex;
        HistoryEntry newEntry = new HistoryEntry
        {
            nodeIndex = targetIndex
        };
        if (_storyPathByIndex.ContainsKey(originIndex))
        {
            _storyPathByIndex[originIndex]++;
        }
        else { _storyPathByIndex.Add(originIndex, 1); }

        string nodeName = _story[originIndex].name;
        if (!string.IsNullOrEmpty(nodeName))
        {
            if (!_storyPath.ContainsKey(nodeName))
            {
                _storyPath.Add(nodeName, new Dictionary<int, int>());
            }

            if (_storyPath[nodeName].ContainsKey(choiceIndex))
            {
                _storyPath[nodeName][choiceIndex]++;
            }
            else
            {
                _storyPath[nodeName].Add(choiceIndex, 1);
            }
        }

        _history.Add(newEntry);
        _currentNode = targetIndex;
        _waitingJump = -1;
        
        if (targetIndex != -1)
        {
            SaveNextLines();
        }
    }

    private Line ExtractFirstTextLine(IList<Line> lines)
    {
        Line firstLine = lines[0];
        lines.RemoveAt(0);

        if (firstLine is ActionLine)
        {
            ParseActionLine(firstLine.text);
        }

        return firstLine;
    }

    private void ParseActionLine(string line)
    {
        Match match = Regex.Match(line, "^!(?<bool>\\w*)$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string var = match.Groups["bool"].Value.ToLower();
            string val = variables[var];
            switch (val.ToLower())
            {
                case "true":
                    variables[var] = "false";
                    break;
                case "false":
                    variables[var] = "true";
                    break;
            }
            return;
        }

        match = Regex.Match(line, "^(?<event>\\w*) = (?<result>true|false)$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            variables[match.Groups["event"].Value.ToLower()] = match.Groups["result"].Value.ToLower();
            return;
        }
        
        match = Regex.Match(line, "^(?<var>\\w*) = (?<value>.*)$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            variables[match.Groups["var"].Value.ToLower()] = EvaluatePredicate(match.Groups["value"].Value.Trim());
        }
    }

    private string EvaluatePredicate(string predicate)
    {
        if (variables.ContainsKey(predicate))
            return variables[predicate];

        if (TryIntOperation(predicate, out int result))
        {
            return result.ToString();
        }

        return predicate;
    }

    private bool TryIntOperation(string predicate, out int result)
    {
        result = 0;
        
        Match match = Regex.Match(predicate, "^(?<var>\\w*\\d*) (?<operator>[+\\-\\/*]) (?<value>\\w*\\d*)$", RegexOptions.IgnoreCase);
        if (!match.Success) return false;
        
        string var = match.Groups["var"].Value.ToLower();
        if (variables.ContainsKey(var)) var = variables[var];
        if (!int.TryParse(var, out int varResult)) return false;
            
        string op = match.Groups["operator"].Value;
        string value = match.Groups["value"].Value;
        if (variables.ContainsKey(value)) value = variables[value];
        if (!int.TryParse(value, out int valueResult)) return false;
        
        result = OperateIntVar(varResult, op, valueResult);
        return true;
    }

    private int OperateIntVar(int var, string op, int value)
    {
        return op switch
        {
            "+" => var + value,
            "-" => var - value,
            "*" => var * value,
            "/" => var / value,
            _ => throw new Exception("Operation sign not valid: " + op)
        };
    }

    private string GetDialogueText(string id, string text, int nodeIndex)
    {
        if (_localization.ContainsKey(id)) text = _localization[id];
        
        MatchCollection m = Regex.Matches(text, "{%([^\\s=\\/]{2,}?)}");
        foreach (Match match in m)
        {
            if (variables.ContainsKey(match.Groups[1].Value.ToLower()))
            {
                text = Regex.Replace(text,
                    "{%" + match.Groups[1].Value + "}",
                    variables[match.Groups[1].Value.ToLower()]);
            }
        }
            
        if (Regex.IsMatch(text, "\\|", RegexOptions.IgnoreCase))
        {
            string[] textLines = text.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            text = textLines[GetPathCount(nodeIndex) % textLines.Length].Trim();
        }

        return text;
    }

    private List<ChoiceLine> GetChoices(int nodeIndex)
    {
        List<ChoiceLine> choices = new List<ChoiceLine>();
        NodeData nodeData = _story[nodeIndex];

        for (int i = 0; i < nodeData.choices.Count; i++)
        {
            LineData lineData = nodeData.choices[i];
            switch (lineData)
            {
                case ConditionalLineData data:
                {
                    List<LineData> linesData = GetConditionLines(data);
                    foreach (LineData conditionalLineData in linesData)
                    {
                        if (conditionalLineData is ChoiceData choiceData)
                        {
                            string id = nodeData.Id + "$" + choiceData.index;
                            ChoiceLine choice = new ChoiceLine
                            {
                                id = id,
                                text = GetDialogueText(id, choiceData.text, nodeIndex),
                                choiceIndex = choiceData.index
                            };
                        
                            choices.Add(choice);
                        }
                    }

                    break;
                }
                case ChoiceData choiceData:
                {
                    string id = nodeData.Id + "$" + choiceData.index;
                    ChoiceLine choice = new ChoiceLine
                    {
                        id = id,
                        text = GetDialogueText(id, choiceData.text, nodeIndex),
                        choiceIndex = choiceData.index
                    };
                    
                    choices.Add(choice);
                    break;
                }
            }
        }

        return choices;
    }

    public List<LineData> GetConditionLines(ConditionalLineData data)
    {
        List<LineData> list = new List<LineData>();

        if (AreConditionsValid(data.comparison))
        {
            return data.lines;
        }

        foreach (ConditionalLineData conditional in data.nestedConditionals)
        {
            if (AreConditionsValid(conditional.comparison))
            {
                return conditional.lines;
            }
        }

        return list;
    }

    private bool AreConditionsValid(string conditions)
    {
        string[] conditionOr = conditions.Split(" OR ", StringSplitOptions.RemoveEmptyEntries);
        
        if (conditionOr.Length == 0) { return true; }
        
        for (int i = 0; i < conditionOr.Length; i++)
        {
            string[] conditionAnd = conditionOr[i].Trim()
                .Split(" AND ", StringSplitOptions.RemoveEmptyEntries);
        
            if (conditionAnd.Length == 0) { return true; }

            bool ev = true;
            for (int j = 0; j < conditionAnd.Length; j++)
            {
                if (!IsConditionValid(conditionAnd[j].Trim()))
                {
                    ev = false;
                }
            }

            if (ev)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsConditionValid(string condition)
    {
        string[] conditionSplit = condition.Split(" ", StringSplitOptions.RemoveEmptyEntries);

        switch (conditionSplit.Length)
        {
            case 0:
                return true;
            case 1:
                Match match = Regex.Match(condition, "^!(.*)", RegexOptions.IgnoreCase);
                return match.Success ? IsBooleanComparisonValid(match.Groups[1].Value, "==", false) :
                    IsBooleanComparisonValid(conditionSplit[0].Trim(), "==", true);
            case 2:
                if (conditionSplit[0].Trim() != "!")
                    throw new Exception("Non valid expresion: " + condition);

                return IsBooleanComparisonValid(conditionSplit[0].Trim(), "==", false);
            case 3:
                string variable = conditionSplit[0].Trim();
                string op = conditionSplit[1].Trim();
                string value = conditionSplit[2].Trim();
                
                if (!Regex.IsMatch(op, "(==|>=|<=|!=|<|>)", RegexOptions.IgnoreCase))
                    throw new Exception("Non valid operator: " + condition);
                
                int val;
                if (variables.ContainsKey(variable))
                {
                    if (!int.TryParse(variables[variable], out int var))
                        return op == "==" ^ !string.Equals(variables[variable], value,
                            StringComparison.CurrentCultureIgnoreCase);
                    if (int.TryParse(value, out val))
                    {
                        return IsIntegerComparisonValid(var, val, op);
                    }
                    if (int.TryParse(variables[value.ToLower()], out val))
                    {
                        return IsIntegerComparisonValid(var, val, op);
                    }

                    return op == "==" ^ !string.Equals(variables[variable], value, StringComparison.CurrentCultureIgnoreCase);
                }
        
                if (Regex.IsMatch(value, "(True|False)", RegexOptions.IgnoreCase))
                {
                    return IsBooleanComparisonValid(variable, op, bool.Parse(value));
                }
        
                return int.TryParse(value, out val) && IsIntegerComparisonValid(GetPathCount(variable), val, op);
            default:
                throw new Exception("Non valid expresion: " + condition);
        }
    }

    private bool DoVariableExist(string variable)
    {
        if (_storyPath.ContainsKey(variable)) { return true; }
        if (!Regex.IsMatch(variable, "\\$", RegexOptions.IgnoreCase)) return false;
        
        string[] s = variable.Split(new[] { "$" }, StringSplitOptions.None);
        string name = s[0];
        if (!_storyPath.ContainsKey(name)) return false;

        return int.TryParse(s[s.Length - 1], out int choice) && _storyPath[name].ContainsKey(choice);
    }

    private int GetPathCount(string variable)
    {
        if (_storyPath.ContainsKey(variable))
        {
            return _storyPath[variable].Keys.Sum(c => _storyPath[variable][c]);
        }

        if (!Regex.IsMatch(variable, "\\$", RegexOptions.IgnoreCase)) return 0;
        string[] s = variable.Split(new[] { "$" }, StringSplitOptions.None);
        string name = s[0];
        if (!_storyPath.ContainsKey(name)) return 0;
        if (!int.TryParse(s[s.Length - 1], out int choice)) return 0;
            
        return _storyPath[name].ContainsKey(choice) ? _storyPath[name][choice] : 0;
    }

    private int GetPathCount(int nodeIndex)
    {
        return !_storyPathByIndex.ContainsKey(nodeIndex) ? 0 : _storyPathByIndex[nodeIndex];
    }

    private static bool IsIntegerComparisonValid(int variable, int value, string comparisonOperator)
    {
        return comparisonOperator switch
        {
            "==" => variable == value,
            ">" => variable > value,
            "<" => variable < value,
            "!=" => variable != value,
            "<=" => variable <= value,
            ">=" => variable >= value,
            _ => false
        };
    }

    private bool IsBooleanComparisonValid(string variable, string comparisonOperator, bool value)
    {
        if (!variables.ContainsKey(variable))
            return value switch
            {
                true => comparisonOperator != "==" ^ DoVariableExist(variable),
                _ => comparisonOperator == "==" ^ DoVariableExist(variable)
            };
        
        if (bool.TryParse(variables[variable], out bool varValue))
        {
            return comparisonOperator switch
            {
                "==" => varValue == value,
                "!=" => varValue != value,
                _ => throw new Exception(comparisonOperator + " is not a boolean operator. Incorrect comparison: " + variable + comparisonOperator + value)
            };
        }

        throw new Exception(variable + " is not a boolean. Incorrect comparison: " + variable + comparisonOperator + value);

    }

    public int GetActualNodeIndex()
    {
        return _history.Count == 0 ? 0 : _history[^1].nodeIndex;
    }

    public string GetActualNodeName()
    {
        int nodeIndex = GetActualNodeIndex();
        return nodeIndex < 0 ? null : _story[nodeIndex].name;
    }
}