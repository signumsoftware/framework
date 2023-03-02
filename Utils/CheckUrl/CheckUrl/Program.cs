using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CheckUrl;

class Program
{
    static async Task Main(string[] args)
    {
        if(args.Length < 2)
        {
            Console.WriteLine("Usage: CheckUrl alive/dead \"http://www.google.com\" 10");
        }

        var alive = 
            args[0]?.ToLower() == "alive" ? true :
            args[0]?.ToLower() == "dead" ? false : 
            throw new InvalidOperationException("Unexcpected " + args[0]);

        var url = args[1];
        var retry = args.Length == 2 ? 15 : int.Parse(args[2]);

        var client = new HttpClient();
        try
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("GET " + url + "...");
                try
                {
                    var response = await client.GetAsync(url);

                    Console.ForegroundColor = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.Write((int)response.StatusCode + " (" + response.StatusCode + ")");

                    if (response.IsSuccessStatusCode == alive)
                    {
                        Console.WriteLine();
                        return;
                    }



                }
                catch(HttpRequestException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(e.Message);
                }


                retry--;

                if (retry == 0)
                    throw new ApplicationException("Timeoout");

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(" retrying in 10 sec (" + retry + " left)");
                await Task.Delay(1000 * 10);
            }
        }
        finally
        {
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
