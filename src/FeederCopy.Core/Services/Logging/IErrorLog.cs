namespace FeederCopy.Core.Services.Logging
{
    public interface IErrorLog
    {
        void Error(string context, Exception exception);
    }
}
