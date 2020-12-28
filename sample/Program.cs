using System;
using System.IO;
using System.Threading.Tasks;

namespace WindowsScreenRecorder
{
    public class Program
    {
        static void Main()
        {
            var fileName = Path.Combine( Environment.CurrentDirectory, "recording.mp4" );

            {
                using var stream = File.Create( fileName );
                using var recorder = ScreenRecorder.Create( stream );

                Console.WriteLine( $"Recording in {fileName}." );
                Console.Write( "Press any key to stop." );
                Console.Read();
            }

            Console.Write( "Recording finished." );
        }
    }
}
