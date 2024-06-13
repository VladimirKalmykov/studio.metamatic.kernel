using System;

namespace Metamatic
{
    public class RepositoryException: Exception
    {
        public readonly Exception innerException;
        public RepositoryException(string message = "Repository error", Exception innerException = null): base(message)
        {
        }
    }
    
    public class IORepositoryException: RepositoryException
    {
        public readonly string uri;
        public IORepositoryException(string uri, string message = "IO Repository error", Exception innerException = null): base($"IO Exception (uri: {uri}): {message}, original exception: {innerException?.Message} {innerException?.StackTrace}")
        {
            this.uri = uri;
        }
    }
    
    public class WriteRepositoryException: IORepositoryException
    {
        public WriteRepositoryException(string uri, string message = "Repository write error", Exception innerException = null): base(message, uri)
        {
        }
    }
    
    public class ReadRepositoryException: IORepositoryException
    {
        public ReadRepositoryException(string uri, string message = "Repository read error", Exception innerException = null): base( uri, message)
        {
        }
    }
}