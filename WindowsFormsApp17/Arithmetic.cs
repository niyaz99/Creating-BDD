using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UCLouvain.BDDSharp;

static class Arithmetic
{
    
    public static List<Token> Calculate(string expression)
    {
        MatchCollection collection = Regex.Matches(expression, @"\(|\)|[A-Z]|[a-z]|!|\||&|>|\^|=");
        if (collection.Count == 0)
            return null;
        Regex variables = new Regex(@"[A-Z]|[a-z]");
        Regex operations = new Regex(@"!|\||&|>|\^|="); 
        Regex brackets = new Regex(@"\(|\)");
        string[] priority = { "!", "&", "|", ">", "^","=" };

        Stack<string> stack = new Stack<string>();
        List<Token> list = new List<Token>();
        foreach (Match match in collection)
        {
            Match temp = variables.Match(match.Value);
            if (temp.Success) { list.Add(new Token(temp.Value, TokenType.Variable)); continue; }
            temp = brackets.Match(match.Value);
            if (temp.Success)
            {
                if (temp.Value == "(") { stack.Push(temp.Value); continue; }
                string operation = stack.Pop();
                while (operation != "(")
                {
                    list.Add(new Token(operation, TokenType.Operation));
                    operation = stack.Pop();
                }
                continue;
            }
            temp = operations.Match(match.Value);
            if (temp.Success)
            {
                    while (stack.Count != 0 && Array.IndexOf(priority, temp.Value) > Array.IndexOf(priority, stack.Peek()))
                    {
                        if (stack.Peek() == "(") break;
                        list.Add(new Token(stack.Pop(), TokenType.Operation));
                    }
                stack.Push(temp.Value);
            }
        }
        while (stack.Count != 0)
            list.Add(new Token(stack.Pop(), TokenType.Operation));
        return list;
    }
    public static bool Calculate(List<Token> rpn, Dictionary<string, bool> variables)
    {
        Stack<bool> result = new Stack<bool>();
        
        foreach (Token token in rpn)
        {
            if (token.Type == TokenType.Variable) result.Push(variables[token.Value]);
            if (token.Type == TokenType.Operation)
                try
                {
                    switch (token.Value)
                    {
                        case "!": result.Push(!result.Pop()); break;  //Not
                        case "&": result.Push(result.Pop() & result.Pop()); break; // And
                        case "|": result.Push(result.Pop() | result.Pop()); break; //Or
                        case ">": result.Push(result.Pop() | !result.Pop()); break; // Impl
                        case "^": result.Push(!(result.Pop() ^ result.Pop())); break; // Xor
                        case "=":                                                     //Equiv
                            {
                                bool a = result.Pop();
                                bool b = result.Pop();
                                result.Push((!a | b) & (a | !b));
                                break;
                            }
                    }
                }
                catch
                {
                    ;
                }
        }
        return result.Pop();
    }
  
    public static Dictionary<string, bool> GetVariables(List<Token> rpn)
    {
        string[] variables = rpn.Where(x => x.Type == TokenType.Variable).Distinct().Select(x => x.Value).Cast<string>().ToArray();
        Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
        foreach (string variable in variables)
            dictionary[variable] = false;
        return dictionary;
    }
    public static void GetVariables(int value, Dictionary<string, bool> variables)
    {
        string binary = Convert.ToString(value, 2);
        for (int i = 1; i < binary.Length; i++)
            variables[variables.ElementAt(i - 1).Key] = binary[i] == '0' ? false : true;
    }
    public static BDDNode CreateTree(List<Token> rpn, Dictionary<string, bool> variables,BDDManager manager)//возвращает бинарное дерево
    {
        int CountOfVariables = variables.Count;
        int count = Convert.ToInt32(Math.Pow(2, CountOfVariables - 1));//количесвто переменных в последнем ряду
        BDDNode[] arr = new BDDNode[count];
        for (int i = 0; i < count; i++)
        {
            bool left;
            bool right;
            GetVariables(i * 2 + count * 2, variables);
            try {left = Calculate(rpn, variables); }
            catch { return null; }
            GetVariables(i * 2 + 1 + count * 2, variables);
            try { right = Calculate(rpn, variables); }
            catch { return null; }
            BDDNode lft;//Low
            BDDNode rght;//High
            if (left)
                lft = manager.One;
            else
                lft = manager.Zero;
            if (right)
                rght = manager.One;
            else
                rght = manager.Zero;
            arr[i] = manager.Create(CountOfVariables - 1, rght, lft);
        }
        if (CountOfVariables == 1)
        {
            return arr[0];
        }
        if(CountOfVariables==2)
        {
            return manager.Create(CountOfVariables - 2, arr[1], arr[0]);
        }
        int newcount = 0;
        List<BDDNode> list = new List<BDDNode>();
        int c = 0;
        for (int i = 0; i < Convert.ToInt32(Math.Pow(2, CountOfVariables - 2)); i++)//предпоследняя линия переменных
        {
            list.Add(manager.Create(CountOfVariables - 2, arr[i*2+1], arr[i*2]));
        }
        if(CountOfVariables==3)
        {
            return manager.Create(0, list[1], list[0]);
        }
        c = list.Count();
        int k = 0;
        for (int i = 0; i < CountOfVariables - 1; i++)
        {
            newcount = newcount + Convert.ToInt32(Math.Pow(2, i));
        }
        int lvl = CountOfVariables - 3; 
        while (lvl != 0)
        {
            for (int i = list.Count; i < newcount; i++)
            {
                if (i == Convert.ToInt32(Math.Pow(2, lvl))+c)
                {
                    k = c;
                    lvl--;
                    c = i;// first c=16, when variables.count=6
                }
                list.Add(manager.Create(lvl, list[i - c + i % Convert.ToInt32(Math.Pow(2, lvl)) + 1+k], list[i - c + i % Convert.ToInt32(Math.Pow(2, lvl))+k]));
            }
        }
        return list.Last();
    }
    public static void PrintTable(List<Token> rpn, Dictionary<string, bool> variables, TextBox tb)
    {
        tb.Clear();
        int count = (int)Math.Pow(2, variables.Count);
        foreach (var value in variables)
            tb.AppendText(value.Key+" ");
        tb.AppendText("f\n");
        for (int i = 0; i < count; i++)
        {
            GetVariables(i + count, variables);
            foreach (var value in variables)
                tb.AppendText(value.Value ? "1 " : "0 ");
            tb.AppendText(Calculate(rpn, variables) ? "1 \n" : "0 \n"); //значения функций
        }
        foreach (var value in variables)
            tb.AppendText(" ");
        tb.AppendText(" \n");
    }
}