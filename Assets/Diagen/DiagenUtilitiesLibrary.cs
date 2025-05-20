using System.Text;

namespace Diagen
{
    public static class DiagenUtilities
    {
        public static string ReplaceCharWithEscapedChar(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                switch (c)
                {
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}