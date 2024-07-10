using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CardGameCore;

abstract class Core(int port)
{
	public TcpListener listener = new(IPAddress.Any, port);
	public abstract Task HandleNetworking();
	public abstract Task Init(PipeStream? pipeStream);
}
