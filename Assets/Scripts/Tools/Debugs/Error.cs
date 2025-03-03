using Assets;
using Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine.UIElements.Experimental;

namespace Tools
{
    public enum EError { Log, Warning, Error, FatalError, Count }

    public class Error : System.Exception
    {
        string m_Message;
        List<string> m_Trace;

        public new string Message => m_Message;
        public List<string> Trace => m_Trace;

        public Error(string message, EError type = EError.Error, int frame = 0, ELogTag logTag = ELogTag.None, bool display = true) : base(message)
        {
            frame += 1;

            m_Message = message;
            m_Trace = GetTrace(frame);

            // check if configuration contains this type of logs to display
            display &= ErrorHandler.ShouldDisplayLogTag(logTag);
                              
            if (display)
            {
                message = FormatErrorMessage(message, type, frame);
                switch (type)
                {
                    case EError.Log:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        UnityEngine.Debug.Log(message);
                        break;
                    case EError.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        UnityEngine.Debug.LogWarning(message);
                        break;
                    case EError.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        UnityEngine.Debug.LogError(message);
                        break;
                    case EError.FatalError:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;

                    default:
                        break;
                }

                Console.ResetColor();

                Console.WriteLine(GetStackTrace(frame).ToString());
            }
           
            if (type == EError.FatalError)
                throw this;
        }

        public string GetTraceString()
        {
            string output = "";
            foreach (string traceLine in m_Trace)
            {
                output += traceLine + "\n";
            }
            return output;
        }

        private List<string> GetTrace(int frame)
        {
            StackTrace stackTrace = new StackTrace();
            List<string> trace = new List<string>();

            for (int i = 0; i < 15; i++)
            {
                StackFrame callerFrame = stackTrace.GetFrame(frame + 1 + i); // get the calling frame 
                if (callerFrame == null)
                    break;

                string callerMethod = callerFrame.GetMethod().Name;
                string callerType = callerFrame.GetMethod().DeclaringType.FullName;
                int callerLine = callerFrame.GetFileLineNumber();

                trace.Add($"{callerType}.{callerMethod}()");
            }

            return trace;
        }

        private string FormatErrorMessage(string message, EError errorType, int frame)
        {
            string pattern = @"(?:\w+\.)*(\w+)(?:`[\d\[\], ]+)?\.(\w+)\(\)";
            Match match = Regex.Match(m_Trace[0], pattern);
            if (match.Success)
            {
                return $"{match.Groups[1].Value}.{match.Groups[2].Value}(): {message}";
            } 
           
            return message;
        }

        StackTrace GetStackTrace(int frame)
        {
            string environmentStackTrace = Environment.StackTrace;

            // Split the stack trace into individual lines
            string[] lines = environmentStackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // Create a StackTrace object
            StackTrace customStackTrace = new StackTrace();
            List<StackFrame> customFrames = new ();
        
            // Iterate over each line of the stack trace and add StackFrame objects
            for (int i = frame; i < lines.Length; i++)
            {
                string line = lines[i];

                // Assuming the format of each line is "   at Namespace.Class.Method()"
                int startIndex = line.IndexOf("   at ") + 6;    // Start index of the method name
               
                if (startIndex >= 0)
                {
                    string call = line.Substring(startIndex, line.Length - startIndex - 1);
                    string[] parts = call.Split(':');
                    if (parts.Length < 2)
                    {
                        UnityEngine.Debug.LogWarning("Unable so split call in at least 2 parts : " + call);
                        continue;
                    }

                    string fileName = parts[parts.Length - 2];
                    if (! int.TryParse(parts[parts.Length - 1].Trim(), out int lineNum))
                    {
                        UnityEngine.Debug.LogWarning("Unable to parse line number : " + parts[parts.Length - 1]);
                        continue;
                    }

                    customFrames.Add(new StackFrame(fileName, lineNum));
                }
            }

            return customStackTrace;
        }
    }
}