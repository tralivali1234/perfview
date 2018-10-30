﻿using System;
using System.Text;
using System.Text.RegularExpressions;

public delegate int MainBody();

internal class CommandLineUtilities
{
    public static bool Debug = false;
    public static int RunConsoleMainWithExceptionProcessing(MainBody body)
    {
        if (Debug)
        {
            return body();
        }

        try
        {
            return body();
        }
        catch (ApplicationException e)
        {
            // Errors that think are really for user consumption I used ApplicationException for.  
            // Just print the message and fail.  
            Console.WriteLine("Error: " + e.Message);
            Console.WriteLine("Use /? for help.");
            return 1;
        }
        catch (Exception e)
        {
            Console.WriteLine("An exceptional condition occured.");
            Console.WriteLine("Exception: " + e);
            // Console.WriteLine("Use /debug for additional debugging information.");
            return 1;
        }
    }

    /// <summary>
    /// Given a list of arguments, from 'startAt' to the end of 'arguments' quote any 
    /// arguments that have spaces in them so that the resulting command lineNumber has 
    /// been turned back into a string that could be passed to a subprocess.
    /// </summary>
    /// <param variable="arguments">The command lineNumber arguments parsed as space sparated token.</param>
    /// <param variable="startAt">The index in 'arguments' of the sub-array of interest (typically 0).</param>
    /// <returns>
    /// A string that represents the original commannd lineNumber string before being parsed 
    /// into array of space separated arguments 
    /// </returns>
    public static string FormCommandLineFromArguments(string[] arguments, int startAt)
    {
        // TODO not quite right for strings with combinations of \ and "s 
        StringBuilder ret = new StringBuilder();
        bool first = true;
        while (startAt < arguments.Length)
        {
            string arg = arguments[startAt++];
            // WriteLine("Got Arg " + arg);
            if (!first)
            {
                ret.Append(' ');
            }

            first = false;
            if (Regex.IsMatch(arg, @"\s"))
            {
                ret.Append('"');
                string rest = arg;
                while (true)
                {
                    Match m = Regex.Match(rest, "^(.*?)(\\\\*)\"(.*)");  //  search for \"  \\" \\\"
                    if (!m.Success)
                    {
                        break;
                    }

                    ret.Append(m.Groups[1].Value);
                    ret.Append('\\', m.Groups[2].Value.Length * 2);
                    ret.Append("\\\"");
                    rest = m.Groups[3].Value;
                }
                Match mLast = Regex.Match(rest, "^(.*)(\\\\+)$");
                if (mLast.Success)
                {
                    ret.Append(mLast.Groups[1].Value);
                    ret.Append('\\', mLast.Groups[2].Value.Length * 2);
                }
                else
                {
                    ret.Append(rest);
                }

                ret.Append('"');
            }
            else
            {
                ret.Append(arg);
            }
        }
        return ret.ToString();
    }
}
