using System;
using System.Runtime.CompilerServices;
namespace OpenGLMinecraftClone
{
    public class Program
    {
        // Noah Lee 6/17/25 & 6/18/25. Coding during the summer break and class???
        public static void Main(string[] args)
        {
            using (Game game = new Game(1920, 1080))
            {
                try 
                {
                    game.Run(); 
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine($"Error during game initialization : {ex.Message}");
                    return; 
                }
            }
        }
    }
}