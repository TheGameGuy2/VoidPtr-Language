namespace Errors;

public static class ErrorHandler
{
    public static void Throw(string msg)
    {
        throw new Exception("Error: "+ msg);
    }

    public static void Throw(string msg, int line)
    {
        throw new Exception("Error: "+ msg + $" at: line {line}");
    }
}