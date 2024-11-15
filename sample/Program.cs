using System;
using System.IO;
using System.Threading.Tasks;

namespace WindowsScreenRecorder
{
    public class Program
    {
        static async Task Main()
        {
            var fileName = Path.Combine( Environment.CurrentDirectory, $"recording.mp4" );

            {
                using var stream = File.Create( fileName );
                await using var recorder = await ScreenRecorder.CreateAsync( stream );

                Console.WriteLine( $"Recording in {fileName}." );
                Console.Write( "Press any key to stop." );
                Console.Read();
            }
            Console.Write( "Recording finished." );
        }
    }
}
